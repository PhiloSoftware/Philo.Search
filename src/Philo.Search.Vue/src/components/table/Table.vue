<template>
  <div :class="rowClass">
    <div :class="colClass">
      <div :class="rowClass">
        <div :class="colClass" class="mb-1">
          <button variant="outline-primary" @click="toggleFilterShow">
            Filters
          </button>
        </div>
        <transition name="slide-down">
          <div v-if="showFilter" :class="colClass">
            <div :class="rowClass" class="tableFilters">
              <div
                :class="colClass"
                v-for="filter in columnFilters"
                v-bind:key="filter.id"
              >
                <template v-if="filter.type === 'text'">
                  <input
                    type="text"
                    v-model="filter.value"
                    @keyup="requestDataLoad"
                  />
                </template>
                <template v-else-if="filter.type === 'number'">
                  <b-input-group size="sm" :prepend="filter.label">
                    <b-form-input
                      type="number"
                      :min="filter.min"
                      :max="filter.max"
                      :step="filter.step"
                      v-model="filter.value"
                      @change="filterChanged"
                    />
                    <b-form-select
                      v-model="filter.action"
                      :options="comparators"
                      @change="filterChanged"
                    >
                    </b-form-select>
                  </b-input-group>
                </template>
                <template v-else-if="filter.type === 'bool'">
                  <b-input-group size="sm" :prepend="filter.label">
                    <b-form-select
                      v-model="filter.value"
                      :options="boolOptions"
                      @change="filterChanged"
                    >
                    </b-form-select>
                  </b-input-group>
                </template>
                <template
                  v-else-if="
                    filter.type === 'date' || filter.type === 'unixdate'
                  "
                >
                  <datetime-picker
                    v-model="filter.value"
                    :label="filter.label"
                    :timeDefault="
                      filter.label.startsWith('From ') ? '00:00:00' : '23:59:59'
                    "
                    @change="filterChanged"
                  />
                </template>
                <template v-else-if="filter.type === 'list'">
                  <b-input-group size="sm" :prepend="filter.label">
                    <multiselect
                      v-model="filter.value"
                      label="text"
                      track-by="value"
                      class="form-control"
                      :tagging="true"
                      :options="filter.options"
                      :multiple="true"
                      @input="filterChanged"
                    >
                    </multiselect>
                    <!-- <b-form-select v-model="filter.value" :options="filter.options" @change='filterChanged' >
                      <template slot='first'>
                        <option :value="null"></option>
                      </template>
                    </b-form-select> -->
                  </b-input-group>
                </template>
              </div>
            </div>
          </div>
        </transition>
        <div
          :class="colClass"
          class="mb-1 text-right order-3 order-md-4 d-flex"
        >
          <div v-if="$slots.add" class="add-button">
            <b-button
              variant="outline-success"
              v-b-modal="'modal_add' + tableId"
              ><i class="fa fa-upload" /> Add</b-button
            >
            <b-modal
              :id="'modal_add' + tableId"
              :title="addModalTitle"
              :size="addModalSize"
              @show="
                showAdd = true;
                $emit('addShown');
              "
              @ok="$emit('addSaved')"
              @hide="$emit('addHidden')"
              class="text-left"
            >
              <slot name="add" v-if="showAdd"></slot>
            </b-modal>
          </div>
          <font-awesome-icon
            v-if="dataLoadFailed"
            icon="exclamation"
            class="flipX text-success reloadGrid mt-1 text-warning"
            :class="{ rotating: fetchingData }"
            @click="reloadChanger++"
          >
          </font-awesome-icon>
          <i
            v-else
            class="flipX text-success reloadGrid mt-1 icon-refresh"
            :class="{ rotating: fetchingData }"
            @click="reloadChanger++"
          />
        </div>
      </div>
      <div :class="rowClass">
        <div :class="colClass">
          <datatable
            ref="dataTable"
            :columns="visibleColumns"
            :data="fetchData"
            :filter-by="mockFilterBy"
            :sortBy="sortModel"
            responsive
            class="filteredTable"
          >
            <template v-slot="row">
              <slot name="rowlayout" v-bind="row"></slot>
            </template>
          </datatable>
        </div>
      </div>
      <div :class="rowClass">
        <div :class="colClass">
          <nav>
            <datatable-pager
              v-model="pageModel"
              type="abbreviated"
              :per-page="pageSize"
            ></datatable-pager>
          </nav>
        </div>
        <div :class="colClass" class="text-right">
          <span class="p-2 text-primary"
            >[{{ pagingInfo.currentIdx }} - {{ pagingInfo.currentMaxIdx }}] of
            {{ pagingInfo.totalRows }}</span
          >
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Component, Prop, Vue } from "vue-property-decorator";
import { debounce } from "ts-debounce";
import DeepEqual from "deep-equal";
import { asEnumerable } from "linq-es2015";
import { Dictionary } from "vue-router/types/router";
import { VueDatatable, VuejsDatatableFactory } from "vuejs-datatable";
import {
  DataColumn,
  DataColumnFilterValue,
  Filter,
  FilterGroup,
  FilterSet,
  SortDirection,
} from "@/processor/datastructure";
import Processor from "@/processor/processor";
import PsVueDataTable from "./PsVueDataTable";

Vue.use(VuejsDatatableFactory);

@Component
export default class Table extends Vue {
  processor!: Processor;

  @Prop({
    type: String,
    required: false,
    default: "pstable",
  })
  tableId!: string;

  @Prop({
    type: Boolean,
    required: false,
    default: false,
  })
  bindToQueryString!: boolean;

  @Prop({
    type: String,
    required: false,
    default: "",
  })
  sort!: string;

  @Prop({
    type: String,
    required: false,
    default: "desc",
  })
  sortDir!: string;

  @Prop({
    type: Number,
    required: false,
    default: 1,
  })
  page!: number;

  @Prop({
    type: Number,
    required: false,
    default: 20,
  })
  pageSize!: number;

  @Prop({
    type: Array,
    required: true,
    default: [],
  })
  columns!: Array<DataColumn>;

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  @Prop({ default: () => {} })
  public fetchRows!: (
    filter: FilterSet
  ) => Promise<{ rows: Array<any>; totalRowCount: number }>;

  $refs!: {
    // eslint-disable-next-line @typescript-eslint/ban-types
    dataTable: PsVueDataTable<{}>;
  };

  dataLoadFailed = false;
  fetchingData = false;
  showFilter = false;
  pageModel = this.page;
  items: Record<string, any>[] = [];
  pagingInfo: {
    currentIdx: number;
    currentMaxIdx: number;
    totalRows: number;
  } = {
    currentIdx: 0,
    currentMaxIdx: 0,
    totalRows: 0,
  };

  get sortModel(): { sort: string; dir: string } {
    return {
      sort: this.sort,
      dir: this.sortDir,
    };
  }

  get rowClass(): string {
    return "";
  }

  get colClass(): string {
    return "";
  }

  get columnFilters(): Array<DataColumnFilterValue> {
    return this.processor.columnFilters;
  }

  get visibleColumns(): Array<DataColumn> {
    return asEnumerable(this.processor.columns)
      .Where((c) => c.visible !== false)
      .ToArray();
  }

  get mockFilterBy(): string {
    return "";
  }

  toggleFilterShow(): void {
    this.showFilter = !this.showFilter;
    console.log(this.showFilter);
  }

  private async requestDataLoad(): Promise<void> {
    await this.$refs.dataTable.processRows();
  }

  public fetchData: () => void = debounce(this.doFetch, 400);

  private async doFetch() {
    this.fetchingData = true;
    const filter = this.processor.doSearch();
    if (this.bindToQueryString) {
      if (await this.filterChanged(filter)) {
        return;
      }
    }
    this.$emit("filter-change", filter);

    var rowRes = await this.fetchRows(filter);

    this.pagingInfo.totalRows = rowRes.totalRowCount;
    this.pagingInfo.currentIdx = Math.max(
      filter.pageSize * filter.pageNumber - filter.pageSize + 1,
      1
    );
    this.pagingInfo.currentMaxIdx = Math.min(
      filter.pageSize * filter.pageNumber,
      rowRes.totalRowCount
    );

    return rowRes;
  }

  public async filterChanged(filter: FilterSet): Promise<boolean> {
    const query: Dictionary<string> = {};

    query.page = filter.pageNumber.toString();
    query.pagesize = filter.pageSize.toString();

    if (filter.sortBy) {
      query.sort = filter.sortBy;
      query.sort_dir = filter.sortDir;
    }

    const wrkFilters = asEnumerable(filter.filter.filterGroups)
      .SelectMany((fg: FilterGroup) => fg.filters)
      .Where((f: Filter) => {
        return f.value !== undefined;
      })
      .Select((f: Filter) => {
        return [
          {
            name: `${f.field.toLowerCase()}_a`,
            value: f.action,
          },
          {
            name: `${f.field.toLowerCase()}_v`,
            value: f.value,
          },
        ];
      })
      .SelectMany((l: Array<{ name: string; value: unknown }>) => l)
      .ToArray();

    wrkFilters.forEach((filter: { name: string; value: any }) => {
      query[filter.name] = filter.value;
    });
    var retValue = !DeepEqual(this.$route.query, query);
    if (!retValue) {
      return retValue;
    }
    return this.$router
      .push({
        query: query,
      })
      .then(() => false)
      .catch((err) => {
        if (err.name != "NavigationDuplicated") {
          throw err;
        }
        return true;
      });
  }

  created(): void {
    this.processor = new Processor(
      this.columns,
      [],
      this.page,
      this.pageSize,
      {
        field: "",
        label: "",
        visible: true,
      },
      SortDirection.Desc
    );

    Vue.set(this, "processor", this.processor);

    this.doFetch();
  }
}
</script>

<style lang="scss" scoped>
.slide-down-enter-active {
  -moz-transition-duration: 0.3s;
  -webkit-transition-duration: 0.3s;
  -o-transition-duration: 0.3s;
  transition-duration: 0.3s;
  -moz-transition-timing-function: ease-in;
  -webkit-transition-timing-function: ease-in;
  -o-transition-timing-function: ease-in;
  transition-timing-function: ease-in;
}

.slide-down-leave-active {
  -moz-transition-duration: 0.3s;
  -webkit-transition-duration: 0.3s;
  -o-transition-duration: 0.3s;
  transition-duration: 0.3s;
  -moz-transition-timing-function: cubic-bezier(0, 1, 0.5, 1);
  -webkit-transition-timing-function: cubic-bezier(0, 1, 0.5, 1);
  -o-transition-timing-function: cubic-bezier(0, 1, 0.5, 1);
  transition-timing-function: cubic-bezier(0, 1, 0.5, 1);
}

.slide-down-enter-to,
.slide-down-leave {
  max-height: 100px;
  overflow: hidden;
}

.slide-down-enter,
.slide-down-leave-to {
  overflow: hidden;
  max-height: 0;
}
</style>
