<script setup lang="ts">
import { ref, computed, watch, inject, onMounted, onBeforeUnmount } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import DataTable from './DataTable/DataTable.vue'
import DeepEqual from "fast-deep-equal";
import {
  DataColumn,
  DataColumnFilterValue,
  FilterSet,
  Processor,
  SortDirection,
  ColumnFilterType,
  Comparator
} from 'philo-search-core'
import { debounce } from "./DataTable/utils/helpers";

const props = defineProps<{
  tableId?: string,
  theme?: string,
  bindToQueryString?: boolean,
  sort?: string,
  sortDir?: SortDirection
  page?: number,
  pageSize?: number,
  columns: Array<DataColumn>,
  fetchRequired: boolean,
  fetchRows:  (
    filter: FilterSet
  ) => Promise<{ rows: Array<any>; totalRowCount: number }>,
}>()

var emit = defineEmits(["fetchRequired"])

const processor = ref(new Processor(
  props.columns ?? [],
  [],
  props.page ?? 1,
  props.pageSize ?? 20,
  props.sort ?? "",
  props.sortDir ?? SortDirection.Descending
))

const rows = ref<any[]>([])

const showFilter = ref(false)
const toggleFilterShow = (): void => {
  showFilter.value = !showFilter.value
}

const cols = computed(() => {
  const cols = processor.value.columns
    .filter(c => c.visible !== false)

  const initialValue: { [id: string] : string; } = {};
  cols.forEach(c => {
    initialValue[c.field] = c.label
  })
  return initialValue
})

const pagination = ref({
  page: props.page,
  per_page: props.pageSize,
  total: 0
})

var hasMounted = ref(false);

const fetchingData = ref(false)
const loadData = async (query: {
  page: number,
  search: '',
  per_page: number | string
}) => {
  fetchingData.value = true;

  if (query) {
    pagination.value.page = query.page;
    // per_page comes through as a string
    pagination.value.per_page = parseInt(`${query.per_page}`);
    processor.value.setPagination(pagination.value.page, pagination.value.per_page);
  }

  // wait till mounted as we cannot pull route query params till then
  if (!hasMounted.value) {
    fetchingData.value = false
    return
  }

  
  const filter = processor.value.doSearch();
  if (props.bindToQueryString) {
    if (await filterChanged(filter)) {
      fetchingData.value = false
      return;
    }
  }

  // this.$emit("filter-change", filter);
  if (props.fetchRows) {
    const res = await props.fetchRows(filter)
    rows.value = res.rows
    pagination.value = { 
      ...pagination.value,
      page: filter.pageNumber,
      total: res.totalRowCount,
      per_page: filter.pageSize,
    }
  }
  else {
    console.log('cannot fetch data as no func supplied')
  }
  fetchingData.value = false;

  
  emit('fetchRequired', false)
};

const loadDataWithLastQuery = async() => {
  if (props.sortDir && props.sort) {
    processor.value.setSort(props.sort, props.sortDir)
  }
  if (props.sort === '') {
    processor.value.setSort('', SortDirection.Ascending)
  }
  return loadData({
    page: pagination.value.page ?? 1,
    search: "",
    per_page: pagination.value.per_page ?? 15
  })
}

const requestDataLoad: () => void = debounce(loadDataWithLastQuery, 400)

const routerLocator = (props.bindToQueryString ? inject('router', () => useRouter()) : undefined);
const router = routerLocator ? routerLocator() : undefined;

const routeLocator = props.bindToQueryString ? inject('route', () => useRoute()) : undefined;
const route  = routeLocator ? routeLocator() : undefined;

const populateFiltersFromQuery = async () => {
  if (!hasMounted.value) {
    return
  }
  if (props.bindToQueryString && route) {
    let hasChanged = false;
    var queryPrefix = props.tableId !== "" ? `${props.tableId}_` : "";

    const page = route.query[`${queryPrefix}page`];
    const pageSize = route.query[`${queryPrefix}pagesize`];
    if (page !== `${pagination.value.page}` || pageSize !== `${props.pageSize}`) {
      hasChanged = true;

      pagination.value.page = Number.parseInt(`${page ?? props.page}`);
      if (isNaN(pagination.value.page)) {
        pagination.value.page = 1;
      };
      
      let lclPageSize = Number.parseInt(`${pageSize || props.pageSize}`);
      if (isNaN(lclPageSize)) {
        lclPageSize = props.pageSize ?? 15;
      };

      processor.value.setPagination(pagination.value.page, lclPageSize)
    }

    const sort = route.query[`${queryPrefix}sort`];
    const sortDir = route.query[`${queryPrefix}sort_dir`];
    if (props.sort !== sort || props.sortDir !== sortDir) {
      hasChanged = true;

      processor.value.setSort(
        `${sort ?? props.sort}`,
        sortDir === "Asc"
          ? SortDirection.Ascending
          : SortDirection.Descending
      );
    }

    processor.value.columnFilters.forEach(cf => {
      const queryActionParam = route.query[`${queryPrefix}${cf.id.toLowerCase()}_a`]
      const queryValueParam = route.query[`${queryPrefix}${cf.id.toLowerCase()}_v`]

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
        cf.value = `${queryValueParam}`
      }
    })
    
    return hasChanged
  }
}

watch(() => props.fetchRequired, (nval: boolean) => {
  if (nval === true) {
    requestDataLoad();
  }
})

if (route) {
  
  watch(() => route.query, async () => {
    if (!hasMounted.value) {
      return;
    }
    const hasChanged = await populateFiltersFromQuery()
    
    if (hasChanged) {
      requestDataLoad();
    }
  })
}

onMounted(async () => {
  hasBeenUnmounted.value = false
  hasMounted.value = true
  await populateFiltersFromQuery()
  requestDataLoad()
})

const hasBeenUnmounted = ref(false)
onBeforeUnmount(() => {
  hasBeenUnmounted.value = true
})

const filterChanged = async (filter: FilterSet): Promise<boolean> => {
  if (hasBeenUnmounted.value) {
    return false;
  }
    var queryPrefix = props.tableId !== "" ? `${props.tableId}_` : "";

    const query: { [id: string] : string; } = {};

    query[`${queryPrefix}page`] = filter.pageNumber.toString();
    query[`${queryPrefix}pagesize`] = filter.pageSize.toString();

    if (filter.sortBy) {
      query[`${queryPrefix}sort`] = filter.sortBy;
      query[`${queryPrefix}sort_dir`] = filter.sortDir;
    }

    const wrkFilters = processor.value.columnFilters
      .filter((f: DataColumnFilterValue) => {
        return f.value !== undefined && f.value !== '';
      })
      .map((f: DataColumnFilterValue) => {
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
      .flatMap((l: Array<{ name: string; value: unknown }>) => l);

    wrkFilters.forEach((filter: { name: string; value: any }) => {
      query[filter.name] = filter.value;
    });

    if (route && router) {
      var retValue = !DeepEqual(route.query, query);
      if (!retValue) {
        return retValue;
      }

      return router
        .push({
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
};


const themes = {
  bootstrap: {
    row: "row",
    col: "col",
  },
  vuetify: {
    row: "v-row",
    col: "v-col",
    colAuto: "v-col-auto",
    tiny: "v-col-2",
    max: "v-col-12",
  }
}

const rowClass = computed(() => {
  
  switch (props.theme) {
    case "bootstrap":{
      return themes.bootstrap.row;
    }
    case "vuetify":{
      return  themes.vuetify.row;
    }
    default: {
      return "t-row"
    }
  }

})

const colClass = computed(() => {
  switch (props.theme) {
    case "bootstrap": {
      return themes.bootstrap.col;
    }
    case "vuetify":{
      return themes.vuetify.col;
    }
      default: {
        return "t-col"
      }
  }
})

const colClassAuto = computed(() => {
  switch (props.theme) {
    case "vuetify":{
      return themes.vuetify.colAuto;
    }
    default: {
      return "t-tiny-col"
    }
  }
})

const colClassMax = computed(() => {
  switch (props.theme) {
    case "bootstrap": {
      return 'col'
    }
    case "vuetify":{
      return themes.vuetify.max;
    }
    default: {
      return "t-col"
    }
  }
})

const btnClass = computed(() => {
  switch (props.theme) {
    case "bootstrap":
      return 'btn btn-outline-primary';
    case "vuetify":
      return 'v-btn';
  }
  return "t-btn"
})

const columnFilterType = ColumnFilterType;

const asBool = (val: string) => {
  return val.toLowerCase() === 'true'
}
</script>

<template>
  <div class="filteredTable" :class="rowClass">
    <div :class="colClass">
      <div :class="rowClass">
        <div :class="colClass" class="t-tiny-col">
          <button class='show-filters' type="button" :class="btnClass" @click="toggleFilterShow">
            Filters
          </button>
        </div>
        <div v-if="theme === 'vuetify'" class="v-spacer"></div>
        <div :class="colClassAuto + ' t-ml-auto t-text-right'">
          <button class="reload" @click="requestDataLoad">
            <div :class="{ rotating: fetchingData }">
            <svg
              aria-hidden="true"
              focusable="false"
              data-prefix="fas"
              data-icon="sync-alt"
              role="img"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 512 512"
            >
              <path
                fill="currentColor"
                d="M370.72 133.28C339.458 104.008 298.888 87.962 255.848 88c-77.458.068-144.328 53.178-162.791 126.85-1.344 5.363-6.122 9.15-11.651 9.15H24.103c-7.498 0-13.194-6.807-11.807-14.176C33.933 94.924 134.813 8 256 8c66.448 0 126.791 26.136 171.315 68.685L463.03 40.97C478.149 25.851 504 36.559 504 57.941V192c0 13.255-10.745 24-24 24H345.941c-21.382 0-32.09-25.851-16.971-40.971l41.75-41.749zM32 296h134.059c21.382 0 32.09 25.851 16.971 40.971l-41.75 41.75c31.262 29.273 71.835 45.319 114.876 45.28 77.418-.07 144.315-53.144 162.787-126.849 1.344-5.363 6.122-9.15 11.651-9.15h57.304c7.498 0 13.194 6.807 11.807 14.176C478.067 417.076 377.187 504 256 504c-66.448 0-126.791-26.136-171.315-68.685L48.97 471.03C33.851 486.149 8 475.441 8 454.059V320c0-13.255 10.745-24 24-24z"
              ></path>
            </svg>
            </div>  
          </button>
        </div>
      </div>
      <div :class="rowClass">
        <div :class="colClass">
          <div :class="rowClass">
            <div :class="colClass">
              <transition name="slide-down">
                <div v-if="showFilter" :class="colClass">
                  <div :class="rowClass" class="tableFilters">
                    <div
                      :class="colClass"
                      v-for="filter in processor.columnFilters"
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
                            :multiple="asBool(filter.props.multiple)"
                            class="w-100"
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
                      <template v-else-if="filter.type === columnFilterType.number">
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
                      <template v-else-if="filter.type === columnFilterType.bool">
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
                            <option value="true">Yes</option>
                            <option value="false">No</option>
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
                      <template v-else-if="filter.type === columnFilterType.text">
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
                      <template v-else-if="filter.type === columnFilterType.guid">
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
              
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>  
  <div :class="rowClass">
    <div :class="colClassMax">
      <slot
        name="data-table"
        v-bind="{ pagination, rows, cols, loadData }"
      >
        <DataTable
          :pagination="pagination"  
          :rows="rows"
          :columns="cols"
          striped
          @loadData="loadData"
        >
          <template
            v-for="col in columns"
            :key="col.field"
            v-slot:[`cell-${col.field}`]="{ row, value }"
          >
            <slot 
              :name="`cell-${col.field}`"
              :row="row"
              :value="value"
            >
            </slot>
          </template>
        </DataTable>
      </slot>
    </div>
  </div>
</template>

<style scoped>

.t-row {
  display: flex;
  width: 100%;
  flex-wrap: wrap;
  flex-grow: 1;
  flex-basis: fit-content;
}
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
.t-ml-auto {
   margin-left:auto;
}
.t-btn {
  border: 1px solid gray;
  padding-left: 1em;
  padding-right: 1em;
  padding-top: 0.3em;
  padding-bottom: 0.3em;
  border-radius: 5px;
  margin-bottom: 0.5em;
}
.reload {
  cursor: pointer;
  width: 20px;
  transform: scale(-1, -1);
  display: inline-block;
  margin-bottom: 0.4em;
}

@-moz-keyframes spin { 
    100% { -moz-transform: rotate(360deg); } 
}
@-webkit-keyframes spin { 
    100% { -webkit-transform: rotate(360deg); } 
}
@keyframes spin { 
    100% { 
        -webkit-transform: rotate(360deg); 
        transform:rotate(360deg); 
    } 
}
.reload .rotating {
  animation: spin 2s linear infinite;
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
.w-100 {
  width: 100%;
}
</style>