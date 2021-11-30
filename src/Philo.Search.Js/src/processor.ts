import { asEnumerable } from 'linq-es2015';
import moment = require('moment');
import {
  Column, ColumnFilterType, ColumnFilterValue, Comparator, Filter, FilterGroup, FilterOperator, FilterSet, RequiredFilter, SortDirection
} from "./datastructure";

export default class Processor {
  columns!: Column[];
  requiredFilters!: RequiredFilter[];
  page!: number;
  pageSize!: number;
  sortBy!: Column;
  sortDir!: SortDirection;

  constructor(
    columns: Array<Column>,
    requiredFilters: Array<RequiredFilter>,
    page: number, 
    pageSize: number,
    sortBy: Column,
    sortDir: SortDirection
  ) {
    this.columns = columns
    this.requiredFilters = requiredFilters
    this.page = page
    this.pageSize = pageSize
    this.sortBy = sortBy
    this.sortDir = sortDir
  }

  public doTextSearch(query: string): FilterSet {
    return this.buildFilter(query)
  }

  public doSearch(): FilterSet  {
    return this.buildFilter()
  }
  
  private buildFilter(queryOverride?: string): FilterSet {
    const reqFilters: FilterGroup = this.getReqFilterGroupForFilters(
      this.requiredFilters
    );

    const colFilters: FilterGroup = this.getFilterGroupForFilters(
      this.columns,
      queryOverride
    )

    const fgRet = []
    if (reqFilters.filters.length > 0 || reqFilters.filterGroups.length > 0) {
      fgRet.push(reqFilters)
    }

    fgRet.push(colFilters)

    return {
      pageNumber: this.page,
      pageSize: this.pageSize,
      sortBy: this.sortBy.field,
      sortDir: this.sortDir,
      filter: {
        operator: FilterOperator.And,
        filters: [],
        filterGroups: fgRet
      },
    };
  }

  private getReqFilterGroupForFilters(filters: Array<RequiredFilter>) : FilterGroup {
    var regFilters: Array<Filter> = asEnumerable(filters)
      .Where(cf => cf.action != "In")
      .Where(cf => typeof(cf.value) === 'boolean' || cf.nullable || (cf.value !== '' && cf.value !== null))
      .Select(cf => {
        var retValue: ColumnFilterValue = undefined;

        if (!Array.isArray(cf.value)) {
          retValue = cf.value

          if (cf.nullable && cf.value === '') {
            retValue = '';
          }
          if (cf.type == ColumnFilterType.unixdate && cf.value) {
            retValue =`${moment(cf.value).unix()}`;
          }

          const returnVal: Filter = {
            field: cf.field,
            action: cf.action,
            value: retValue ?? ''
          }
          return returnVal
        }

        const emptyReturn: Filter = {
          field: cf.field,
          action: cf.action,
          value: retValue ?? ''
        }
        return emptyReturn
      })
      .Where(f => f !== undefined && f?.value !== '')
      .ToArray()

    var inFilters: Array<FilterGroup> = asEnumerable(filters)
      .Where(cf => cf.action === "In")
      .Where(cf => typeof(cf.value) === 'boolean' || (cf.value !== '' && cf.value !== null))
      .Where(cf => cf.value !== undefined)
      .Select(cf => {
        let retVal: Array<Filter> = []
        if (Array.isArray(cf.value)) {
          retVal = cf.value?.map(v => {
            return {
              field: cf.field,
              action: Comparator.Eq,
              value: v
            };
          });
        } else if (cf.value !== undefined) {
          retVal.push({
            field: cf.field,
            action: Comparator.Eq,
            value: cf.value
          })
        }

        return {
          operator: FilterOperator.And,
          filters: retVal,
          filterGroups: []
        }
      })
      .Where(f => f.filters.length > 0)
      .ToArray()

    return {
      operator: FilterOperator.And,
      filters: regFilters,
      filterGroups: inFilters
    }
  }

  private getFilterGroupForFilters(filters: Array<Column>, queryOverride?: string) : FilterGroup {
    // regular filters (not In)
    var regFilters: Array<Filter> = asEnumerable(filters)
      .Where(cf => cf.filter !== undefined)
      .Where(cf => cf.filter?.action !== Comparator.In)
      // get rid of columns that have no impact
      .Where(cf => 
        typeof(cf.filter?.value) === 'boolean'
        || cf.filter?.nullable
        || (cf.filter?.value !== '' && cf.filter?.value !== null)
      )
      .Select(cf => {
        var retValue: ColumnFilterValue = undefined;

        if (!Array.isArray(cf.filter?.value)) {
          retValue = cf.filter?.value;
          if (cf.filter?.nullable && cf.filter?.value === '') {
            retValue = '';
          }
          if (cf.filter?.type == ColumnFilterType.unixdate && cf.filter?.value) {
            retValue =`${moment(cf.filter?.value).unix()}`;
          }
        }

        const returnValue: Filter = {
          field: cf.field,
          action: cf.filter?.action ?? Comparator.Eq,
          value: retValue ?? ''
        }

        return returnValue
      })
      .Where(f => f !== undefined && f?.value !== '')
      .ToArray()

    // In filters, so we expect a list of possible values
    var inFilters: Array<FilterGroup> = asEnumerable(filters)
      .Where(cf => cf.filter?.action === Comparator.In)
      .Where(cf =>
        typeof(cf.filter?.value) === 'boolean'
        || (cf.filter?.value !== '' && cf.filter?.value !== null)
      )
      .Where(cf => cf.filter?.value !== undefined)
      .Select(cf => {
        let retVal: Array<Filter> = []
        if (Array.isArray(cf.filter?.value)) {
          retVal = cf.filter?.value?.map(v => {
            return {
              field: cf.field,
              action: Comparator.Eq,
              value: v
            };
          }) ?? [];
        } else if (cf.filter?.value !== undefined) {
          retVal.push({
            field: cf.field,
            action: Comparator.Eq,
            value: cf.filter?.value
          })
        }

        return {
          operator: FilterOperator.And,
          filters: retVal,
          filterGroups: []
        }
      })
      .Where(f => f.filters.length > 0)
      .ToArray()

    return {
      operator: FilterOperator.Or,
      filters: regFilters,
      filterGroups: inFilters
    }
  }
}