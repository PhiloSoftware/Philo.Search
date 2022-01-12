import { asEnumerable } from "linq-es2015";
import * as moment from "moment";
import {
  ColumnFilterType,
  ColumnFilterValue,
  Comparator,
  DataColumn,
  DataColumnFilterValue,
  Filter,
  FilterGroup,
  FilterOperator,
  FilterSet,
  RequiredFilter,
  SortDirection,
} from "./datastructure";

export default class Processor {
  columns!: Array<DataColumn>;
  requiredFilters!: Array<RequiredFilter>;
  page!: number;
  pageSize!: number;
  sortBy?: string;
  sortDir!: SortDirection;
  columnFilters: Array<DataColumnFilterValue> = [];

  constructor(
    columns: Array<DataColumn>,
    requiredFilters: Array<RequiredFilter>,
    page: number,
    pageSize: number,
    sortBy: string,
    sortDir: SortDirection
  ) {
    this.columns = columns;
    this.requiredFilters = requiredFilters;
    this.page = page;
    this.pageSize = pageSize;
    this.sortBy = sortBy;
    this.sortDir = sortDir;

    this.buildColumnFilters();
  }

  public setSort(sortBy: string, sortDir: SortDirection): void {
    this.sortBy = sortBy;
    this.sortDir = sortDir;
  }

  public setPagination(page: number, pageSize: number): void {
    this.page = page;
    this.pageSize = pageSize;
  }

  public doTextSearch(query: string): FilterSet {
    return this.buildFilter(query);
  }

  public doSearch(): FilterSet {
    return this.buildFilter();
  }

  public buildColumnFilters(): void {
    const filters: Array<DataColumnFilterValue> = asEnumerable(this.columns)
      .Where((c) => c.filter !== undefined)
      .Where((c) => (c.filter?.visible ?? true) === true)
      .SelectMany((c) => {
        const res: Array<DataColumnFilterValue> = [];

        if (c.filter === undefined) {
          return res;
        }

        let defaultValues = [];
        if (Array.isArray(c.filter.defaultValues)) {
          defaultValues = c.filter.defaultValues;
        } else {
          defaultValues.push(c.filter.defaultValues);
        }

        if (
          c.filter.type === ColumnFilterType.date ||
          c.filter.type === ColumnFilterType.unixdate
        ) {
          res.push({
            id: `${c.field}_from`,
            field: c.field,
            label: `From ${c.label}`,
            type: c.filter.type,
            value: defaultValues.length > 1 ? defaultValues[0] : "",
            action: Comparator.Gt,
            nullable: c.nullable ?? false,
            props: c.filter.props ?? {},
            options: c.filter.options,
          });
          res.push({
            id: `${c.field}_to`,
            field: c.field,
            label: `To ${c.label}`,
            type: c.filter.type,
            value: defaultValues.length > 1 ? defaultValues[1] : "",
            action: Comparator.Lt,
            nullable: c.nullable ?? false,
            props: c.filter.props ?? {},
            options: c.filter.options,
          });
        } else if (c.filter.type === ColumnFilterType.number) {
          res.push({
            id: `${c.field}`,
            field: c.field,
            label: c.label,
            type: c.filter?.type ?? ColumnFilterType.text,
            value: defaultValues[0],
            action: c.filter?.action ?? Comparator.Eq,
            nullable: c.nullable ?? false,
            props: c.filter.props ?? {},
            options: c.filter.options,
          });
        } else {
          res.push({
            id: `${c.field}`,
            field: c.field,
            label: c.label,
            type: c.filter?.type ?? ColumnFilterType.text,
            value: defaultValues[0],
            action: c.filter?.action ?? Comparator.Eq,
            nullable: c.nullable ?? false,
            props: c.filter.props ?? {},
            options: c.filter.options,
          });
        }

        return res;
      })
      .ToArray();

    this.columnFilters = filters;
  }

  private buildFilter(queryOverride?: string): FilterSet {
    const reqFilters: FilterGroup = this.getReqFilterGroupForFilters(
      this.requiredFilters
    );

    const colFilters: FilterGroup = this.getFilterGroupForFilters(
      this.columnFilters,
      queryOverride
    );

    const fgRet = [];
    if (reqFilters.filters.length > 0 || reqFilters.filterGroups.length > 0) {
      fgRet.push(reqFilters);
    }

    fgRet.push(colFilters);

    return {
      pageNumber: this.page,
      pageSize: this.pageSize,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      filter: {
        operator: FilterOperator.And,
        filters: [],
        filterGroups: fgRet,
      },
    };
  }

  private getReqFilterGroupForFilters(
    filters: Array<RequiredFilter>
  ): FilterGroup {
    const regFilters: Array<Filter> = asEnumerable(filters)
      .Where((cf) => cf.action !== "In")
      .Where(
        (cf) =>
          typeof cf.value === "boolean" ||
          cf.nullable ||
          (cf.value !== "" && cf.value !== null)
      )
      .Select((cf) => {
        let retValue: ColumnFilterValue = "";

        if (!Array.isArray(cf.value)) {
          retValue = cf.value;

          if (cf.nullable && cf.value === "") {
            retValue = "";
          }
          if (cf.type === ColumnFilterType.unixdate && cf.value) {
            retValue = `${moment(cf.value).unix()}`;
          }

          const returnVal: Filter = {
            field: cf.field,
            action: cf.action,
            value: retValue ?? "",
          };
          return returnVal;
        }

        const emptyReturn: Filter = {
          field: cf.field,
          action: cf.action,
          value: retValue ?? "",
        };
        return emptyReturn;
      })
      .Where((f) => f !== undefined && f?.value !== "")
      .ToArray();

    const inFilters: Array<FilterGroup> = asEnumerable(filters)
      .Where((cf) => cf.action === "In")
      .Where(
        (cf) =>
          typeof cf.value === "boolean" ||
          (cf.value !== "" && cf.value !== null)
      )
      .Where((cf) => cf.value !== undefined)
      .Select((cf) => {
        let retVal: Array<Filter> = [];
        if (Array.isArray(cf.value)) {
          retVal = cf.value?.map((v) => {
            return {
              field: cf.field,
              action: Comparator.Eq,
              value: v,
            };
          });
        } else if (cf.value !== undefined) {
          retVal.push({
            field: cf.field,
            action: Comparator.Eq,
            value: cf.value,
          });
        }

        return {
          operator: FilterOperator.And,
          filters: retVal,
          filterGroups: [],
        };
      })
      .Where((f) => f.filters.length > 0)
      .ToArray();

    return {
      operator: FilterOperator.And,
      filters: regFilters,
      filterGroups: inFilters,
    };
  }

  private getFilterGroupForFilters(
    filters: Array<DataColumnFilterValue>,
    queryOverride?: string
  ): FilterGroup {
    // regular filters (not In)
    const regFilters: Array<Filter> = asEnumerable(filters)
      .Where((cf) => cf.action !== Comparator.In)
      // get rid of columns that have no impact
      .Where(
        (cf) =>
          cf.type === ColumnFilterType.bool ||
          cf.nullable ||
          (cf.value !== "" && cf.value !== undefined)
      )
      .Select((cf) => {
        let retValue: ColumnFilterValue = "";

        if (!Array.isArray(cf.value)) {
          retValue = cf.value;
          if (cf.nullable && cf.value === "") {
            retValue = "";
          }
          if (cf.type === ColumnFilterType.unixdate && cf.value) {
            retValue = `${moment(cf.value).unix()}`;
          }
        }

        const returnValue: Filter = {
          field: cf.field,
          action: cf.action ?? Comparator.Eq,
          value: retValue ?? "",
        };

        return returnValue;
      })
      .Where((f) => f !== undefined && f.value !== "")
      .ToArray();

    // In filters, so we expect a list of possible values
    const inFilters: Array<FilterGroup> = asEnumerable(filters)
      .Where((cf) => cf.action === Comparator.In)
      .Where(
        (cf) =>
          typeof cf.value === "boolean" ||
          (cf.value !== "" && cf.value !== null)
      )
      .Where((cf) => cf.value !== undefined)
      .Select((cf) => {
        let retVal: Array<Filter> = [];
        if (Array.isArray(cf.value)) {
          retVal =
            cf.value?.map((v) => {
              return {
                field: cf.field,
                action: Comparator.Eq,
                value: v,
              };
            }) ?? [];
        } else if (cf.value !== undefined) {
          retVal.push({
            field: cf.field,
            action: Comparator.Eq,
            value: cf.value,
          });
        }

        return {
          operator: FilterOperator.And,
          filters: retVal,
          filterGroups: [],
        };
      })
      .Where((f) => f.filters.length > 0)
      .ToArray();

    return {
      operator: FilterOperator.And,
      filters: regFilters,
      filterGroups: inFilters,
    };
  }
}
