import { Column, ColumnFilterType, Comparator, DataColumn, FilterOperator, RequiredFilter, SortDirection } from '../src/datastructure';
import Processor from '../src/processor';
test('Simple Filter', () => {
  const cols: Array<DataColumn> = [
    {
      field: "col1",
      label: "Column 1",
      visible: true,
      filter: {
        action: Comparator.Eq,
        type: ColumnFilterType.text,
        visible: true,
        defaultValues: "filter one"
      }
    }
  ]

  const processor = new Processor(
    cols,
    [],
    1,
    10,
    "col1",
    SortDirection.Descending
  )

  const fs = processor.doSearch();

  expect(fs.pageNumber).toBe(1);
  expect(fs.pageSize).toBe(10);
  expect(fs.sortBy).toBe(cols[0].field);
  expect(fs.sortDir).toBe(SortDirection.Descending);

  expect(fs.filter.filters.length).toBe(0);
  
  expect(fs.filter.filterGroups.length).toBe(1);
  if (fs.filter.filterGroups.length > 0) {
    const fg = fs.filter.filterGroups[0];

    expect(fg.operator).toBe(FilterOperator.And);
    expect(fg.filterGroups.length).toBe(0);
    expect(fg.filters.length).toBe(1);
    
    if (fg.filters.length > 0) {
      const filt = fg.filters[0]
      expect(filt.field).toBe('col1');
      expect(filt.value).toBe('filter one');
      expect(filt.action).toBe(Comparator.Eq);
    }
  }
});

test('Required Filter', () => {
  const reqFilters: Array<RequiredFilter> = [
    {
      field: "col2",
      type: ColumnFilterType.text,
      action: Comparator.Eq,
      value: "req filter",
    }
  ]

  const cols: Array<DataColumn> = [
    {
      field: "col1",
      label: "Column 1",
      visible: true,
      filter: {
        action: Comparator.Eq,
        type: ColumnFilterType.text,
        visible: true,
        defaultValues: "filter one"
      }
    }
  ]

  const processor = new Processor(
    cols,
    reqFilters,
    1,
    10,
    "col1",
    SortDirection.Descending
  )

  const fs = processor.doSearch();

  expect(fs.pageNumber).toBe(1);
  expect(fs.pageSize).toBe(10);
  expect(fs.sortBy).toBe(cols[0].field);
  expect(fs.sortDir).toBe(SortDirection.Descending);

  expect(fs.filter.filters.length).toBe(0);
  
  expect(fs.filter.filterGroups.length).toBe(2);
  if (fs.filter.filterGroups.length > 0) {
    const fg = fs.filter.filterGroups[0];

    expect(fg.operator).toBe(FilterOperator.And);
    expect(fg.filterGroups.length).toBe(0);
    expect(fg.filters.length).toBe(1);
    
    if (fg.filters.length > 0) {
      const filt = fg.filters[0]
      expect(filt.field).toBe('col2');
      expect(filt.value).toBe('req filter');
      expect(filt.action).toBe(Comparator.Eq);
    }

    const fgcol = fs.filter.filterGroups[1];

    expect(fgcol.operator).toBe(FilterOperator.And);
    expect(fgcol.filterGroups.length).toBe(0);
    expect(fgcol.filters.length).toBe(1);
    
    if (fgcol.filters.length > 0) {
      const filt = fgcol.filters[0]
      expect(filt.field).toBe('col1');
      expect(filt.value).toBe('filter one');
      expect(filt.action).toBe(Comparator.Eq);
    }
  }
});