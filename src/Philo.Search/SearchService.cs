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
    /// <returns>The results that match the <paramref name="filter"/></returns>  
    SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
       FilterSet filter,
       List<IMapAFilter<TEntityType>> mappings
     ) where TEntityType : class;

    /// <summary>
    /// Apply the filter to the queryable and return the 
    /// results.
    /// </summary>
    /// <typeparam name="TEntityType">Entity Type that is being queried</typeparam>
    /// <param name="query">The Queryable data set</param>
    /// <param name="filter">Filters to apply to the dataset</param>
    /// <param name="mappings">Mapping instructions for filters</param>  
    /// <returns>The results that match the <paramref name="filter"/></returns>  
    SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
       FilterSet filter,
       MappingCollection<TEntityType> mappings
     ) where TEntityType : class;
  }

  public class SearchService : ISearchService
  {
    public SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
      FilterSet filter,
      MappingCollection<TEntityType> mappings
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
      };
    }

    public SearchResult<TEntityType> DoSearch<TEntityType>(
      IQueryable<TEntityType> query,
      FilterSet filter,
      List<IMapAFilter<TEntityType>> mappings
    ) where TEntityType : class
    {
      var mappingDef = new MappingCollection<TEntityType>(mappings);

      return DoSearch(query, filter, mappingDef);
    }

    private IQueryable<TEntityType> ApplySort<TEntityType>(
      IQueryable<TEntityType> query,
      FilterSet filter,
      MappingCollection<TEntityType> mappings
    ) where TEntityType : class
    {
      if (!string.IsNullOrWhiteSpace(filter.SortBy))
      {
        var mapping = mappings.GetMapping(filter.SortBy);

        var sorted = mapping.ApplySort(query, filter.SortDir ?? mappings.DefaultSort.SortOrder);

        return mappings.DefaultSort.Mapping.ApplyThenSort(sorted, filter.SortDir ?? mappings.DefaultSort.SortOrder);

      }

      var defaultSort = mappings.DefaultSort;
      return defaultSort.Mapping.ApplySort(query, filter.SortDir ?? mappings.DefaultSort.SortOrder);
    }
  }

  public class SearchResult<T>
  {
    public IQueryable<T> Results { get; set; }
    public int TotalResults { get; set; }
  }
}
