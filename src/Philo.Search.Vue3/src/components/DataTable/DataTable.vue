<template>
    <div class="data-table dt-flex dt-flex-col">
        <div class="dt-align-middle dt-min-w-full">

            <Filter v-if="filter && topPagination" :search="tableQuery.search ?? ''" @input="handleOnSearchChange"/>

            <div class="dt__wrapper dt-relative" :class="{'sm:dt-rounded-lg': rounded}">
                <slot v-if="loading" name="loading">
                    <Loading/>
                </slot>

                <TopPaginationWrapper v-if="showPagination" :with-pagination="topPagination">
                    <Pagination v-if="topPagination"
                                class="dt-flex-1 dt-pr-4"
                                :total="totalData"
                                :current-page="tableQuery.page"
                                :per-page="parseInt((tableQuery.per_page ?? 0).toString())"
                                @changed="handlePageChange">
                        <template #pagination-info="paginationInfo">
                            <slot name="pagination-info" :start="paginationInfo.start" :end="paginationInfo.end" :total="paginationInfo.total">
                                Showing
                                <span class="dt-font-medium" v-text="paginationInfo.start"/>
                                to
                                <span class="dt-font-medium" v-text="paginationInfo.end"/>
                                of
                                <span class="dt-font-medium" v-text="paginationInfo.total"/>
                                results.
                            </slot>
                        </template>
                    </Pagination>

                    <Filter v-if="filter && !topPagination" :search="tableQuery.search ?? ''" @input="handleOnSearchChange"/>

                    <PaginationSize :value="tableQuery.per_page ?? 10" :options="perPageOptions" @input="handleOnPaginationSizeChange"/>
                </TopPaginationWrapper>

                <TableWrapper>
                    <THead>
                        <slot v-if="sn" name="thead-sn">
                            <TableHeadCell class="dt__table__thead__th_sn" v-text="`S.N.`"/>
                        </slot>

                        <slot name="thead" :column="tableColumns">
                            <TableHeadCell v-for="(label, key) in tableColumns"
                                           :key="`datatable-thead-th-${key}`"
                                           v-text="label"/>
                        </slot>
                    </THead>

                    <TBody>
                        <TableRow v-for="(row, rowIndex) in tableRows"
                                  :key="`datatable-row-${uniqueId()}-${rowIndex}`"
                                  :hoverable="hoverable"
                                  :non-clickable="nonClickable"
                                  :row-index="rowIndex"
                                  :striped="striped"
                                  @clicked="rowClickHandler(row)">
                            <slot v-if="sn" name="tbody-sn" :sn="rowIndex + 1">
                                <TableBodyCell class="dt__table__tbody_td_sn" v-text="rowIndex + 1 + paginatedRowIndex"/>
                            </slot>

                            <TableBodyCell
                                v-for="(label, key) in tableColumns"
                                :key="`datatable-tbody-td-${uniqueId()}-${key}`"
                                :name="label"
                            >
                                <slot :name="`cell-${key}`" :row="row" :value="row[key]">
                                    {{ row[key] }}
                                </slot>
                            </TableBodyCell>
                        </TableRow>

                        <TableRow v-if="tableRows.length === 0" :row-index="0">
                            <slot name="empty"/>
                        </TableRow>
                    </TBody>
                </TableWrapper>

                <BottomPaginationWrapper v-if="showPagination && bottomPagination">
                    <pagination :total="totalData"
                                :current-page="tableQuery.page"
                                :per-page="parseInt((tableQuery.per_page ?? '1').toString())"
                                @changed="handlePageChange">
                        <template #pagination-info="paginationInfo">
                            <slot name="pagination-info" :start="paginationInfo.start" :end="paginationInfo.end" :total="paginationInfo.total">
                                Showing
                                <span class="dt-font-medium" v-text="paginationInfo.start"/>
                                to
                                <span class="dt-font-medium" v-text="paginationInfo.end"/>
                                of
                                <span class="dt-font-medium" v-text="paginationInfo.total"/>
                                results.
                            </slot>
                        </template>
                    </pagination>
                </BottomPaginationWrapper>
            </div>

        </div>
    </div>
</template>

<script lang="ts" setup>
import {
    computed,
    defineComponent,
    PropType,
    ref,
    SetupContext,
    watch,
}                              from "vue"
import { PaginationProps }     from "./@types/PaginationProps"
import { QueryProps }          from "./@types/QueryProps"
import { TableQuery }          from "./@types/TableQuery"
import Filter                  from "./Components/Filter/Filter.vue"
import Loading                 from "./Components/Loading.vue"
import BottomPaginationWrapper from "./Components/Pagination/BottomPaginationWrapper.vue"
import Pagination              from "./Components/Pagination/Pagination.vue"
import PaginationSize          from "./Components/Pagination/PaginationSize.vue"
import TopPaginationWrapper    from "./Components/Pagination/TopPaginationWrapper.vue"
import TableBodyCell           from "./Components/Table/TableBodyCell.vue"
import TableHeadCell           from "./Components/Table/TableHeadCell.vue"
import TableRow                from "./Components/Table/TableRow.vue"
import TableWrapper            from "./Components/Table/TableWrapper.vue"
import TBody                   from "./Components/Table/TBody.vue"
import THead                   from "./Components/Table/THead.vue"
import {
    debounce,
    formatString,
}                              from "./utils/helpers"

const PER_PAGE = 10

const props = defineProps({
    rows: {
        type: Array<{}>, required: true 
    },
    columns: { 
        type: Object, required: false, default: null 
    },
    pagination: { 
        type: Object as PropType<PaginationProps>,
        required: false, 
        default: null 
    },
    rounded: {
        type: Boolean, required: false, default: false
    },
    striped: {
        type: Boolean, required: false, default: false
    },
    sn: {
        type: Boolean, required: false, default: false
    },
    filter: { 
        type: Boolean, required: false, default: false 
    },
    loading: {
        type: Boolean, required: false, default: false
    },
    perPageOptions: { 
        type: Array as PropType<Array<string | number>>, 
        required: false, 
        default: () =>  [5, 10, 15, 25, 50, 75, 100] 
    },
    query: { 
        type: Object as PropType<QueryProps>, required: false, 
        default: () => ({}) 
    },
    topPagination: { type: Boolean, required: false, default: false },
    bottomPagination: { type: Boolean, required: false, default: true },
    hoverable: { type: Boolean, required: false, default: false },
    nonClickable: { type: Boolean, required: false, default: false }
})

const emits = defineEmits( ["loadData", "rowClicked"])

const tableQuery = ref<TableQuery>({
    page: props.pagination?.page || 1,
    search: props.query.search || "",
    per_page: props.pagination?.per_page || PER_PAGE,
})

const showPagination = computed(() => !!props.pagination)
const totalData = computed(() => props.pagination?.total || props.rows.length)
const tableRows = computed<Array<{}>>(() => props.rows)

const tableColumns = computed(() => {
    if (props.columns) {
        return props.columns
    }

    if (props.rows.length === 0) {
        return {}
    }

    return Object.keys(props.rows[0]).reduce((cols, key) => ({ ...cols, [key]: formatString(key) }), {})
})

const paginatedRowIndex = computed(() => {
    let per_page = 10;
    if (typeof tableQuery.value.per_page == 'string') {
        per_page = parseInt(tableQuery.value.per_page) ?? per_page
    }
    else if (typeof tableQuery.value.per_page == 'number') {
        per_page = tableQuery.value.per_page
    }

    const page = tableQuery.value.page ?? 1;

    return showPagination.value
        ? per_page * (page - 1)
        : 0
})

const uniqueId = () => Math.floor(Math.random() * 100)

const fireDataLoad = () => {
    emits("loadData", tableQuery.value)
}

watch(() => ({ ...tableQuery.value }), () => {
    fireDataLoad()
}, {
    deep: true,
    immediate: true,
})

const handlePageChange = (page: number | undefined) => {
    tableQuery.value.page = page
}

const handleOnSearchChange = debounce((value) => {
    tableQuery.value = { ...tableQuery.value, page: 1, search: value }
})

const handleOnPaginationSizeChange = (value: string | number | undefined) => {
    tableQuery.value = { ...tableQuery.value, page: 1, per_page: value }
}

const rowClickHandler = (row: {}) => {
    if (props.nonClickable || !props.hoverable) {
        return
    }

    emits("rowClicked", row)
}
</script>

<style lang="scss">

@import "tailwindcss/base";
@import "tailwindcss/components";
@import "tailwindcss/utilities";


select.dt__pagination_size {
    background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
    background-position: right 0.5rem center;
    background-repeat: no-repeat;
    background-size: 1.5em 1.5em;
    padding-right: 2.5rem;
    color-adjust: exact;
    appearance: none;
}
</style>
