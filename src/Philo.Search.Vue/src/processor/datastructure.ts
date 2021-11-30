export enum ColumnFilterType {
  guid,
  bool,
  text,
  date,
  unixdate,
}

export enum FilterOperator {
  "And" = "And",
  "Or" = "Or",
}

export interface Filter {
  field: string;
  value: string;
  action: Comparator;
}

export interface FilterGroup {
  operator: FilterOperator;
  filters: Array<Filter>;
  filterGroups: Array<FilterGroup>;
}

export interface FilterSet {
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDir: string;
  filter: FilterGroup;
}

export type ColumnFilterValue = string | undefined | Array<string>;

export interface ColumnFilter {
  type: ColumnFilterType;
  nullable?: boolean;
  value: ColumnFilterValue;
  action: Comparator;
}

export interface RequiredFilter extends ColumnFilter {
  field: string;
}

export interface Column {
  field: string;
  label: string;
  visible: boolean;
  filter?: ColumnFilter;
  width?: string | number;
}

export enum Comparator {
  "Eq" = "Eq",
  "Gt" = "Gt",
  "Lt" = "Lt",
  "Like" = "Like",
  "GtEq" = "GtEq",
  "LtEq" = "LtEq",
  "NEq" = "NEq",
  "In" = "In",
}

export enum SortDirection {
  "Asc" = "Asc",
  "Desc" = "Desc",
}
