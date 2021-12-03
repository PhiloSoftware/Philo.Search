<template>
  <div :class="rowClass">
    <div :class="colClass">
      <div :class="rowClass">
        <div :class="colClass" class="t-tiny-col">
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
                <template v-if="filter.options">
                  <div class="label">{{ filter.label }}</div>
                  <select
                    v-model="filter.value"
                    :name="filter.id"
                    :multiple="filter.props.multiple"
                    @change="requestDataLoad"
                  >
                    <option
                      v-for="option in filter.options"
                      :key="option.value"
                      :value="option.value"
                    >
                      {{ option.label }}
                    </option>
                  </select>
                </template>
                <template v-else-if="filter.type === 'number'">
                  <div class="label">{{ filter.label }}</div>
                  <input
                    type="number"
                    v-model="filter.value"
                    :min="filter.props.min"
                    :max="filter.props.max"
                    :step="filter.props.step"
                    @change="requestDataLoad"
                  />
                </template>
                <template v-else-if="filter.type === 'bool'">
                  <b-input-group size="sm" :prepend="filter.label">
                    <select
                      v-model="filter.value"
                      :name="filter.id"
                      @change="requestDataLoad"
                    >
                      <option
                        v-if="filter.nullable === true"
                        :value="null"
                      ></option>
                      <option :value="true">Yes</option>
                      <option :value="false">No</option>
                    </select>
                  </b-input-group>
                </template>
                <template
                  v-else-if="
                    filter.type === 'date' || filter.type === 'unixdate'
                  "
                >
                  <div class="label">{{ filter.label }}</div>
                  <input
                    type="date"
                    v-model="filter.value"
                    :min="filter.props.min"
                    :max="filter.props.max"
                    @keyup="requestDataLoad"
                  />
                </template>
                <template v-else>
                  <div class="label">{{ filter.label }}</div>
                  <input
                    type="text"
                    v-model="filter.value"
                    @keyup="requestDataLoad"
                  />
                </template>
              </div>
            </div>
          </div>
        </transition>
        <div :class="colClass" class="t-tiny-col t-ml-auto t-text-right">
          <div class="reload">
            <svg
              aria-hidden="true"
              focusable="false"
              data-prefix="fas"
              data-icon="sync-alt"
              role="img"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 512 512"
              :class="{ rotating: fetchingData }"
              @click="requestDataLoad"
            >
              <path
                fill="currentColor"
                d="M370.72 133.28C339.458 104.008 298.888 87.962 255.848 88c-77.458.068-144.328 53.178-162.791 126.85-1.344 5.363-6.122 9.15-11.651 9.15H24.103c-7.498 0-13.194-6.807-11.807-14.176C33.933 94.924 134.813 8 256 8c66.448 0 126.791 26.136 171.315 68.685L463.03 40.97C478.149 25.851 504 36.559 504 57.941V192c0 13.255-10.745 24-24 24H345.941c-21.382 0-32.09-25.851-16.971-40.971l41.75-41.749zM32 296h134.059c21.382 0 32.09 25.851 16.971 40.971l-41.75 41.75c31.262 29.273 71.835 45.319 114.876 45.28 77.418-.07 144.315-53.144 162.787-126.849 1.344-5.363 6.122-9.15 11.651-9.15h57.304c7.498 0 13.194 6.807 11.807 14.176C478.067 417.076 377.187 504 256 504c-66.448 0-126.791-26.136-171.315-68.685L48.97 471.03C33.851 486.149 8 475.441 8 454.059V320c0-13.255 10.745-24 24-24z"
                class=""
              ></path>
            </svg>
          </div>
        </div>
      </div>
      <div :class="rowClass">
        <div :class="colClass">
          <datatable
            ref="dataTable"
            :name="tableId"
            :columns="visibleColumns"
            :data="fetchData"
            :perPage="pageSize"
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
          <datatable-pager
            v-model="pageModel"
            :table="tableId"
            type="abbreviated"
            class="pagination"
          ></datatable-pager>
        </div>
        <div :class="colClass" class="pagecount">
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
import { IDataFnParams, VuejsDatatableFactory } from "vuejs-datatable";
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

  get rowClass(): string {
    return "t-row";
  }

  get colClass(): string {
    return "t-col";
  }

  get columnFilters(): Array<DataColumnFilterValue> {
    return this.processor.columnFilters;
  }

  get visibleColumns(): Array<DataColumn> {
    return asEnumerable(this.processor.columns)
      .Where((c) => c.visible !== false)
      .ToArray();
  }

  toggleFilterShow(): void {
    this.showFilter = !this.showFilter;
  }

  private async requestDataLoad(): Promise<void> {
    await this.$refs.dataTable.processRows();
  }

  // eslint-disable-next-line @typescript-eslint/ban-types
  public fetchData: (args: IDataFnParams<{}>) => void = debounce(
    this.doFetch,
    400
  );

  // eslint-disable-next-line @typescript-eslint/ban-types
  private async doFetch(args: IDataFnParams<{}>) {
    this.fetchingData = true;

    console.log(args);
    if (args && args.sortBy !== null) {
      this.processor.setSort(
        args.sortBy,
        // webpack dies when importing ESortDir :/
        args.sortDir.toString() === "Asc"
          ? SortDirection.Asc
          : SortDirection.Desc
      );
    }

    this.processor.setPagination(args.page, args.perPage);

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

    this.fetchingData = false;

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
      "",
      SortDirection.Desc
    );

    Vue.set(this, "processor", this.processor);

    // this.doFetch();
  }
}
</script>

<style lang="scss" scoped>
.t-row {
  display: flex;
  width: 100%;
  flex-wrap: wrap;
  flex-grow: 1;
  flex-basis: fit-content;
  .t-col {
    flex: 0 0 100%;
    -ms-flex-preferred-size: 0;
    flex-basis: 0;
    -webkit-box-flex: 1;
    -ms-flex-positive: 1;
    flex-grow: 1;
    max-width: 100%;
  }
  .t-tiny-col {
    flex: 0 0 80px;
  }
}
.t-text-right {
  text-align: right;
}
.t-ml-auto {
  margin-left: auto;
}
.reload {
  cursor: pointer;
  width: 20px;
  transform: scale(-1, -1);
  display: inline-block;
  margin-bottom: 0.4em;
  svg.rotating {
    @keyframes rotating {
      from {
        transform: rotate(0deg);
        -o-transform: rotate(0deg);
        -ms-transform: rotate(0deg);
        -moz-transform: rotate(0deg);
        -webkit-transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
        -o-transform: rotate(360deg);
        -ms-transform: rotate(360deg);
        -moz-transform: rotate(360deg);
        -webkit-transform: rotate(360deg);
      }
    }
    @-webkit-keyframes rotating {
      from {
        transform: rotate(0deg);
        -webkit-transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
        -webkit-transform: rotate(360deg);
      }
    }
    -webkit-animation: rotating 2s linear infinite;
    -moz-animation: rotating 2s linear infinite;
    -ms-animation: rotating 2s linear infinite;
    -o-animation: rotating 2s linear infinite;
    animation: rotating 2s linear infinite;
  }
}
.tableFilters {
  select,
  input {
    width: calc(100% - 20px);
    margin-right: 10px;
  }
}
.filteredTable {
  width: 100%;
  border-spacing: 0;

  ::v-deep thead th {
    text-align: left !important;
    border-top: 1px solid #c8ced3;
    border-bottom: 2px solid #c8ced3;
    padding: 0.75rem;
  }
  ::v-deep tbody td {
    padding: 0.75rem;
    border-top: 1px solid #c8ced3;
  }
}
.pagination {
  ::v-deep ul {
    list-style: none;
    padding: 0;
    display: flex;
    li {
      padding: 10px;
      width: 1em;
      height: 1em;
      text-align: center;
      border: 1px solid gray;
      &.active {
        background-color: gray;
      }
    }
  }
}
.pagecount {
  text-align: right;
}

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
