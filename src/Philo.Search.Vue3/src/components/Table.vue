<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { DataTable } from '@jobinsjp/vue3-datatable'
import { asEnumerable } from "linq-es2015";
import DeepEqual from "deep-equal";
import {
  DataColumn,
  DataColumnFilterValue,
  FilterSet,
  Processor,
  SortDirection,
  ColumnFilterType
} from 'philo-search-core'
const props = defineProps<{
  tableId: string,
  theme?: string,
  rowClickable: boolean,
  bindToQueryString: boolean,
  sort: string,
  sortDir: SortDirection,
  page: number,
  pageSize: number,
  columns: Array<DataColumn>,
  fetchRows:  (
    filter: FilterSet
  ) => Promise<{ rows: Array<any>; totalRowCount: number }>,
}>()
const processor = ref(new Processor(
  props.columns ?? [],
  [],
  props.page ?? 1,
  props.pageSize ?? 20,
  props.sort,
  props.sortDir
))

const rows = ref<any[]>([])
const pagination = ref({})

const showFilter = ref(false)
const toggleFilterShow = (): void => {
  showFilter.value = !showFilter.value
}

const cols = computed(() => {
  const cols = asEnumerable(processor.value.columns)
    .Where((c) => c.visible !== false)
    .ToArray();

  const initialValue: { [id: string] : string; } = {};
  cols.forEach(c => {
    initialValue[c.field] = c.label
  })
  return initialValue
})

const router = props.bindToQueryString ? useRouter() : undefined;
const route  = props.bindToQueryString ? useRoute() : undefined;
const filterChanged = async (filter: FilterSet): Promise<boolean> => {
    var queryPrefix = props.tableId !== "" ? `${props.tableId}_` : "";

    const query: { [id: string] : string; } = {};

    query[`${queryPrefix}page`] = filter.pageNumber.toString();
    query[`${queryPrefix}pagesize`] = filter.pageSize.toString();

    if (filter.sortBy) {
      query[`${queryPrefix}sort`] = filter.sortBy;
      query[`${queryPrefix}sort_dir`] = filter.sortDir;
    }

    const wrkFilters = asEnumerable(processor.value.columnFilters)
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

const fetchingData = ref(false)
const loadData = async () => {
  fetchingData.value = true;

  const filter = processor.value.doSearch();
  if (props.bindToQueryString) {
    if (await filterChanged(filter)) {
      return;
    }
  }
  // this.$emit("filter-change", filter);
  const res = await props.fetchRows(filter)
  rows.value = res.rows
  pagination.value = { ...pagination.value, page: filter.pageNumber, total: res.totalRowCount }
};

import { debounce } from "ts-debounce";
const requestDataLoad: () => void = debounce(loadData, 400)

const themes = {
  bootstrap: {
    row: "row",
    col: "col",
  },
  vuetify: {
    row: "row",
    col: "col",
  }
}

const rowClass = ref("t-row")
switch (props.theme) {
  case "bootstrap":
    rowClass.value = themes.bootstrap.row;
  case "vuetify":
    rowClass.value =  themes.vuetify.row;
}

const colClass = ref("t-col")
switch (props.theme) {
  case "bootstrap":
    colClass.value = themes.bootstrap.row;
  case "vuetify":
    colClass.value = themes.vuetify.row;
}

const columnFilterType = ColumnFilterType;

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
                        :value="null"
                      ></option>
                      <option :value="true">Yes</option>
                      <option :value="false">No</option>
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
    </div>
  </div>  
  <div :class="rowClass">
    <div :class="colClass">
      <DataTable
        :pagination="pagination"  
        :rows="rows"
        :columns="cols"
        striped
        @loadData="loadData"
      ></DataTable>
    </div>
  </div>
</template>

<style src="@jobinsjp/vue3-datatable/dist/style.css">
</style>
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
.reload {
  cursor: pointer;
  width: 20px;
  transform: scale(-1, -1);
  display: inline-block;
  margin-bottom: 0.4em;
}
.reload svg.rotating {
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
</style>