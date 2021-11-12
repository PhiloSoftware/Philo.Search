using Philo.Search.Filter;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public interface IMapAFilter<TEntityType> where TEntityType : class
  {
    bool IsDefaultSortFilter { get; }
    string Field { get; }
    IQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, SortOrder sortBy);
    Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator);
  }
}
