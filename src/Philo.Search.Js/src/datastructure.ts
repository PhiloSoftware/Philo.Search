export enum ColumnFilterType {
  guid = "guid",
  bool = "bool",
  text = "text",
  date = "date",
  unixdate = "unixdate",
  number = "number",
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

export interface ColumnFilterProps {
  [key: string]: string;
}

export interface DataColumnFilterValue {
  id: string;
  field: string;
  label: string;
  type: ColumnFilterType;
  value?: string | Array<string>;
  action: Comparator;
  nullable: boolean;
  props: ColumnFilterProps;
  options?: Array<{ label: string; value: string }>;
}

export interface DataColumnFilter {
  type: ColumnFilterType;
  action: Comparator;
  visible: boolean;
  defaultValues: string | Array<string>;
  props?: ColumnFilterProps;
  options?: Array<{ label: string; value: string }>;
}

export interface DataColumn {
  field: string;
  label: string;
  visible: boolean;
  nullable?: boolean;
  filter?: DataColumnFilter;
}

export enum Comparator {
  "Eq" = "Eq",
  "Gt" = "Gt",
  "Lt" = "Lt",
  "Like" = "Like",
  "ILike" = "ILike",
  "GtEq" = "GtEq",
  "LtEq" = "LtEq",
  "NEq" = "NEq",
  "In" = "In",
}

export enum SortDirection {
  "Ascending" = "Ascending",
  "Descending" = "Descending",
}
