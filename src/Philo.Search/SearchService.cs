using LinqKit;
using Philo.Search.Filter;
using Philo.Search.Mapping;
using System.Collections.Generic;
using System.Linq;

namespace Philo.Search
{
  public interface ISearchService
  {
    /// <summary>
    /// Apply the filter to the queryable and return the 
    /// results.
    /// </summary>
    /// <typeparam name="TEntityType">Entity Type that is being queried</typeparam>
    /// <param name="query">The Queryable data set</param>
    /// <param name="filter">Filters to apply to the dataset</param>
    /// <param name="mappings">Mapping instructions for filters</param>
    /// <returns></returns>
    SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
       FilterSet filter,
       List<IMapAFilter<TEntityType>> mappings
     ) where TEntityType : class;
  }

  public class SearchService : ISearchService
  {
    public SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
      FilterSet filter,
      List<IMapAFilter<TEntityType>> mappings
    ) where TEntityType : class
    {
      if (filter.PageNumber < 1) filter.PageNumber = 1;

      if (filter.HasFilters())
      {
        query = query.AsExpandable().Where(
          SearchHelper.CreateSearchPredicate(filter, mappings)
        );
      }

      query = ApplySort(query, filter, mappings);

      var resIdx = (filter.PageSize * filter.PageNumber) - filter.PageSize;

      return new SearchResult<TEntityType>
      {
        TotalResults = query.Count(),
        Results = query.Skip(resIdx)
          .Take(filter.PageSize)
          .ToList()
      };
    }

    private IQueryable<TEntityType> ApplySort<TEntityType>(
      IQueryable<TEntityType> query,
      FilterSet filter,
      List<IMapAFilter<TEntityType>> mappings
    ) where TEntityType : class
    {
      var isDescending = filter.SortDir == "desc";
      if (!string.IsNullOrWhiteSpace(filter.SortBy))
      {
        var mapping = mappings
          .FirstOrDefault(m => m.Field == filter.SortBy);

        if (mapping == null)
        {
          throw new BadSortValueException($"Field {filter.SortBy} does not exist");
        }

        return mapping.ApplySort(query, isDescending);
      }

      var defaultSort = mappings
        .FirstOrDefault(m => m.IsDefaultSortFilter);

      if (defaultSort != null)
      {
        return defaultSort.ApplySort(query, isDescending);
      }

      throw new BadSortValueException($"Please provide the SortBy field");
    }
  }

  public class SearchResult<T>
  {
    public List<T> Results { get; set; }
    public int TotalResults { get; set; }
  }
}
