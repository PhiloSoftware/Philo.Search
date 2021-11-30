<template>
  <div :class="rowClass">
    <div :class="colClass">
      <div :class="rowClass">
        <div :class="colClass" class="mb-1">
          <button variant="outline-primary" @click="showFilter = !showFilter">
            Filters
          </button>
        </div>
        <transition name="slide-down">
          <div v-if="showFilter" :class="colClass" class="order-4 order-md-2">
            <div :class="rowClass" class="tableFilters">
              <div
                :class="colClass"
                v-for="filter in columnFilters"
                v-bind:key="filter.id"
              >
                <template v-if="filter.type === 'text'">
                  <b-input-group size="sm" :prepend="filter.label">
                    <b-form-input
                      v-model="filter.value"
                      @keyup="filterChanged"
                    />
                  </b-input-group>
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
import {
  ColumnFilter,
  Filter,
  FilterGroup,
  FilterSet,
  SortDirection,
} from "@/processor/datastructure";
import Processor from "@/processor/processor";

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

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  @Prop({ default: () => {} })
  // eslint-disable-next-line @typescript-eslint/ban-types
  public fetchRows!: Function;

  dataLoadFailed = false;
  fetchingData = false;
  showFilter = false;
  items: Record<string, any>[] = [];
  rowCount = 0;
  pagingInfo: {
    currentIdx: number;
    currentMaxIdx: number;
    totalRows: number;
  } = {
    currentIdx: 0,
    currentMaxIdx: 0,
    totalRows: 0,
  };

  get rowClass(): string {
    return "";
  }

  get colClass(): string {
    return "";
  }

  get columnFilters(): Array<ColumnFilter> {
    return [];
  }

  get visibleColumns(): Array<any> {
    return [];
  }

  get mockFilterBy(): string {
    return "";
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

    this.fetchRows(filter, (items: Record<string, any>[], rowCount: number) => {
      this.items = items;
      this.rowCount = rowCount;
      this.fetchingData = false;
    });
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
      [],
      [],
      1,
      1,
      {
        field: "",
        label: "",
        visible: true,
      },
      SortDirection.Desc
    );
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
