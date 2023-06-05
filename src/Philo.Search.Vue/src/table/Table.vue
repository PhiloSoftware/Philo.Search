<script lang="ts" setup>
import Vue, { PropType, computed, inject, onMounted, ref, watch } from "vue";
import { debounce } from "ts-debounce";
import DeepEqual from "deep-equal";
import { asEnumerable } from "linq-es2015";
import { Dictionary, Route, VueRouter } from "vue-router/types/router";
import { IDataFnParams, VuejsDatatableFactory } from "vuejs-datatable";
import Processor from "philo-search-core/lib/processor";
import {
  DataColumn,
  DataColumnFilterValue,
  FilterSet,
  SortDirection,
  ColumnFilterType
} from "philo-search-core";
import PsVueDataTable from "./PsVueDataTable";
import { Comparator } from "philo-search-core/lib/datastructure";

Vue.use(VuejsDatatableFactory);

const props = defineProps({
  tableId: {
    type: String,
    default: "",
    required: false
  },
  theme: {
    type: String,
    default: "",
    required: false
  },
  rowClickable: {
    type: Boolean,
    default: false,
    required: false
  },
  bindToQueryString: {
    type: Boolean,
    default: false,
    required: false
  },
  sort: {
    type: String,
    default: "",
    required: false
  },
  sortDir: {
    type: String,
    default: "Descending",
    required: false
  },
  page: {
    type: Number,
    default: 1,
    required: false
  },
  pageSize: {
    type: Number,
    default: 20,
    required: false
  },
  fetchRequired: {
    type: Boolean,
    default: false,
    required: false
  },
  columns: {
    type: Array<DataColumn>,
    default: [],
    required: true
  },
  fetchRows: {
    type: Function as PropType<(
      filter: FilterSet
    ) => Promise<{ rows: Array<any>; totalRowCount: number }>>,
    default: [],
    required: true
  },
})

const processor = ref<Processor>(
  new Processor(
    props.columns,
    [],
    props.page,
    props.pageSize,
    "",
    SortDirection.Descending
  ));

const dataTable = ref<PsVueDataTable<{}>>();

const dataLoadFailed = ref(false);
const fetchingData = ref(false);
const showFilter = ref(false);
const pageModel = ref(props.page);
const items = ref<Record<string, any>[]>([]);

const pagingInfo = ref({
  currentIdx: 0,
  currentMaxIdx: 0,
  totalRows: 0,
});

const themes = ref(
  {
    bootstrap: {
      row: "row",
      col: "col",
    },
    vuetify: {
      row: "row",
      col: "col",
    }
  });

const rowClass = computed(() => {
  switch (props.theme) {
    case "bootstrap" :
      return themes.value.bootstrap.row;
    case "vuetify" :
      return themes.value.vuetify.row;
  }
  return "t-row";
})

const colClass = computed(() => {
  switch (props.theme) {
    case "bootstrap" :
      return themes.value.bootstrap.col;
    case "vuetify" :
      return themes.value.vuetify.col;
  }
  return "t-col";
})

const columnFilters = computed(() => {
  const cf = processor.value.columnFilters;

  cf.forEach(c => {
    if (!c.value && c.props.multiple) {
      c.value = []
    }
  })

  return cf;
});

const visibleColumns = computed(() => {
  return asEnumerable(processor.value.columns)
    .Where((c) => c.visible !== false)
    .ToArray();
})

const toggleFilterShow = () => {
    showFilter.value = !showFilter.value;
  }

const requestDataLoad = async () => {
  if (dataTable.value) {
    await dataTable.value.processRows();
  }
}

var emit = defineEmits([
  'filter-change',
  'fetchRequired',
  'fetching-data'
])

const doFetch = async (args: IDataFnParams<{}>) => {
  fetchingData.value = true;

  if (args && args.sortBy !== null) {
    processor.value.setSort(
      args.sortBy,
      // webpack dies when importing ESortDir :/
      args.sortDir?.toString() === "asc"
        ? SortDirection.Ascending
        : SortDirection.Descending
    );
  }

  processor.value.setPagination(args.page, args.perPage);

  const filter = processor.value.doSearch();
  if (props.bindToQueryString) {
    if (await filterChanged(filter)) {
      return;
    }
  }
  emit("filter-change", filter);

  var rowRes = await props.fetchRows(filter);

  emit("fetchRequired", false);

  pagingInfo.value.totalRows = rowRes.totalRowCount;
  pagingInfo.value.currentIdx = Math.max(
    filter.pageSize * filter.pageNumber - filter.pageSize + 1,
    1
  );
  pagingInfo.value.currentMaxIdx = Math.min(
    filter.pageSize * filter.pageNumber,
    rowRes.totalRowCount
  );

  fetchingData.value = false;

  return rowRes;
}

const fetchData = debounce(
doFetch,
400
);


var route = inject<Route>('route')
var router = inject<VueRouter>('router')

const filterChanged = (filter: FilterSet) => {
  var queryPrefix = props.tableId !== "" ? `${props.tableId}_` : "";

  const query: Dictionary<string> = {};

  query[`${queryPrefix}page`] = filter.pageNumber.toString();
  query[`${queryPrefix}pagesize`] = filter.pageSize.toString();

  if (filter.sortBy) {
    query[`${queryPrefix}sort`] = filter.sortBy;
    query[`${queryPrefix}sort_dir`] = filter.sortDir;
  }

  const wrkFilters = asEnumerable(columnFilters.value)
    .Where((f: DataColumnFilterValue) => {
      return f.value !== undefined && f.value !== '';
    })
    .Select((f: DataColumnFilterValue) => {
      return [
        {
          name: `${queryPrefix}${f.id.toLowerCase()}_a`,
          value: f.action,
        },
        {
          name: `${queryPrefix}${f.id.toLowerCase()}_v`,
          value: f.value,
        },
      ];
    })
    .SelectMany((l: Array<{ name: string; value: unknown }>) => l)
    .ToArray();

  wrkFilters.forEach((filter: { name: string; value: any }) => {
    query[filter.name] = filter.value;
  });
  

  if (route) {
    var retValue = !DeepEqual(route.query, query);
    if (!retValue) {
      return retValue;
    }

    return router
      ?.push({
        query: query,
      })
      .then(() => false)
      .catch((err: { name: string }) => {
        if (err.name != "NavigationDuplicated") {
          throw err;
        }
        return true;
      });
  }

  return false;
}

  // created(): void {
    
  //   // @ts-ignore
  //   if (this.$route) {
  //     this.querychanged();
  //   }

  //   Vue.set(this, "processor", this.processor);
  // }

onMounted(() => {
  if (dataTable.value) {
    dataTable.value.page = pageModel.value
  }
})

watch(() => fetchingData, (to) => {
  emit('fetching-data', to)
})

watch(() => props.fetchRequired, (to) => {
  if (to === true) {
    requestDataLoad();
  }
})

if (route?.query) {
  watch(() => route?.query, (to) => {
    if (props.bindToQueryString && route) {
      let hasChanged = false;
      var queryPrefix = props.tableId !== "" ? `${props.tableId}_` : "";

      const page = route.query[`${queryPrefix}page`];
      const pageSize = route.query[`${queryPrefix}pagesize`];
      if (page !== `${pageModel.value}` || pageSize !== `${props.pageSize}`) {
        hasChanged = true;

        pageModel.value = Number.parseInt(`${page ?? props.page}`);
        if (isNaN(pageModel.value)) {
          pageModel.value = 1;
        };
        
        let lclPageSize = Number.parseInt(`${pageSize || props.pageSize}`);
        if (isNaN(lclPageSize)) {
          lclPageSize = props.pageSize;
        };

        processor.value.setPagination(pageModel.value, lclPageSize)
      }

      const sort = route.query[`${queryPrefix}sort`];
      const sortDir = route.query[`${queryPrefix}sort_dir`];
      if (props.sort !== sort || props.sortDir !== sortDir) {
        hasChanged = true;

        processor.value.setSort(
          `${sort ?? props.sort}`,
          // webpack dies when importing ESortDir :/
          sortDir === "Asc"
            ? SortDirection.Ascending
            : SortDirection.Descending
        );
      }

      columnFilters.value.forEach(cf => {
        const queryActionParam = route?.query[`${queryPrefix}${cf.id.toLowerCase()}_a`]
        const queryValueParam = route?.query[`${queryPrefix}${cf.id.toLowerCase()}_v`]
        
        if (queryActionParam === undefined || queryValueParam === undefined) {
          cf.action = cf.action;
          cf.value = undefined;
          return;
        }

        if (!Array.isArray(queryActionParam) && queryActionParam != cf.action) {
          cf.action = Comparator[queryActionParam as keyof typeof Comparator];
          hasChanged = true;
        }
        if (!Array.isArray(queryValueParam) && queryValueParam != cf.value) {
          hasChanged = true;
          cf.value = queryValueParam
        }
      })
      
      if (hasChanged) {
        requestDataLoad();
      }
    }

  })
}
</script>

<template>
  <div :class="rowClass">
    <div :class="colClass">
      <div :class="rowClass">
        <div :class="colClass" class="t-tiny-col">
          <button class='show-filters' variant="outline-primary" type="button" @click="toggleFilterShow">
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
                  <slot
                    name="field-select" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <select
                      v-model="filter.value"
                      :name="filter.id"
                      :multiple="!!filter.props.multiple"
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
                  </slot>
                </template>
                <template v-else-if="filter.type === ColumnFilterType.number">
                  <slot
                    name="field-number" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <input
                      type="number"
                      v-model="filter.value"
                      :min="filter.props.min"
                      :max="filter.props.max"
                      :step="filter.props.step"
                      @change="requestDataLoad"
                    />
                  </slot>
                </template>
                <template v-else-if="filter.type === ColumnFilterType.bool">
                  <slot
                    name="field-bool" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <select
                      v-model="filter.value"
                      :name="filter.id"
                      @change="requestDataLoad"
                    >
                      <option
                        v-if="filter.nullable === true"
                        :value="undefined"
                      ></option>
                      <option :value="'true'">Yes</option>
                      <option :value="'false'">No</option>
                    </select>
                  </slot>
                </template>
                <template
                  v-else-if="
                    filter.type === 'date' || filter.type === 'unixdate'
                  "
                >
                  <slot
                    name="field-date" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <input
                      type="date"
                      v-model="filter.value"
                      :min="filter.props.min"
                      :max="filter.props.max"
                      @change="requestDataLoad"
                    />
                  </slot>
                </template>
                <template v-else-if="filter.type === ColumnFilterType.text">
                  <slot
                    name="field-text" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <input
                      type="text"
                      v-model="filter.value"
                      @keyup="requestDataLoad"
                    />
                  </slot>
                </template>
                <template v-else-if="filter.type === ColumnFilterType.guid">
                  <slot
                    name="field-guid" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <input
                      type="text"
                      v-model="filter.value"
                      @keyup="requestDataLoad"
                    />
                  </slot>
                </template>
                <template v-else>
                  <slot
                    name="field-default" 
                    v-bind="{ filter, change: requestDataLoad }"
                  >
                    <div class="label">{{ filter.label }}</div>
                    <input
                      type="text"
                      v-model="filter.value"
                      @keyup="requestDataLoad"
                    />
                  </slot>
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
            :page="pageModel"
            :perPage="pageSize"
            :waitForPager="true"
            responsive
            class="filteredTable"
          >
            <template v-slot:default="{ row }">
              <slot name="row" v-bind="row">
                <tr @click="rowClickable ? $emit('rowclick', row) : null" :class="rowClickable ? 'hover' : ''">
                  <td v-for="col in visibleColumns" :key="col.field">
                    <slot
                      :name="`cell-${col.field}`"
                      v-bind="{ col, row, value: row[col.field] }"
                    >
                      {{ row[col.field] }}
                    </slot>
                  </td>
                </tr>
              </slot>
            </template>
          </datatable>
        </div>
      </div>
      <div :class="rowClass">
        <div :class="colClass">
          <datatable-pager
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

.hover {
  &:hover {
    background-color: #cccccc;
  }
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
