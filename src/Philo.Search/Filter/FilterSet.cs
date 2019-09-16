using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Philo.Search.Filter
{
  public class FilterSet
  {
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortBy { get; set; }
    public string SortDir { get; set; }
    public FilterGroup Filter { get; set; }

    public void AddRequiredFilterGroup(FilterGroup filterGroup)
    {
      if (Filter.Operator == FilterOperator.And)
      {
        this.Filter.FilterGroups.Add(filterGroup);
      }
      else
      {
        Filter = new FilterGroup
        {
          Operator = FilterOperator.And,
          Filters = new List<Filter>(),
          FilterGroups = new FilterGroup[]
          {
            new FilterGroup
            {
              Filters = Filter.Filters,
              FilterGroups = Filter.FilterGroups,
              Operator = Filter.Operator
            },
            filterGroup
          }
        };
      }
    }

    public void AddRequiredFilter(string field, string value)
    {
      if (Filter.Operator == FilterOperator.And)
      {
        this.Filter.Filters.Add(new Filter
        {
          Action = Comparator.Eq,
          Field = field,
          Value = value
        });
      }
      else
      {
        Filter = new FilterGroup
        {
          Operator = FilterOperator.And,
          Filters = new List<Filter>
          {
            new Filter
            {
              Action = Comparator.Eq,
              Field = field,
              Value = value
            }
          },
          FilterGroups = new FilterGroup[]
          {
            new FilterGroup
            {
              Filters = Filter.Filters,
              FilterGroups = Filter.FilterGroups,
              Operator = Filter.Operator
            }
          }
        };
      }
    }

    public bool HasFilters()
    {
      return this.Filter.Filters.Any() || Filter.FilterGroups.Any();
    }
  }

  public interface IFilter { }

  public class FilterGroup : IFilter
  {
    public FilterOperator Operator { get; set; }

    public ICollection<Filter> Filters { get; set; }
    public ICollection<FilterGroup> FilterGroups { get; set; }
  }

  public class Filter : IFilter
  {
    public string Field { get; set; }
    public string Value { get; set; }
    public Comparator Action { get; set; }
  }

  public enum FilterOperator
  {
    And,
    Or
  }

  public enum Comparator
  {
    Eq,
    Gt,
    Lt,
    Like,
    GtEq
  }
}
