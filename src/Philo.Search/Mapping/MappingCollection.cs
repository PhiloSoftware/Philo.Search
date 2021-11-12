using Philo.Search.Filter;
using System.Collections.Generic;
using System.Linq;

namespace Philo.Search.Mapping
{
  public class MappingCollection<TEntityType>
    where TEntityType : class
  {
    private readonly IEnumerable<IMapAFilter<TEntityType>> mappings;
    private string defaultMapField;
    private SortOrder sortOrder;

    /// <summary>
    /// Create a collection of mapping 
    /// </summary>
    /// <param name="defaultMapping"></param>
    /// <param name="mappings"></param>
    public MappingCollection(IEnumerable<IMapAFilter<TEntityType>> mappings)
    {
      this.mappings = mappings;
    }

    internal DefaultSort<TEntityType> DefaultSort
    {
      get
      {
        if (!string.IsNullOrWhiteSpace(this.defaultMapField))
        {
          return new DefaultSort<TEntityType>(
            mappings.First(m => m.Field == defaultMapField),
            sortOrder
          );
        }

        var defaultSort = mappings
          .FirstOrDefault(m => m.IsDefaultSortFilter);

        if (defaultSort != null)
        {
          return new DefaultSort<TEntityType>(defaultSort, sortOrder);
        }

        if (mappings.Count() > 0)
        {
          return new DefaultSort<TEntityType>(mappings.First(), sortOrder);
        }

        throw new BadSortFieldException($"No default mapping found");
      }
    }

    internal IMapAFilter<TEntityType> GetMapping(string field)
    {
      var mapping = mappings.FirstOrDefault(m => m.Field == field);

      if (mapping == null)
      {
        // todo throw exception so consumer knows their filters aren't being applied
        throw new BadFilterFieldException($"{field} is not known");
      }

      return mapping;
    }

    public MappingCollection<TEntityType> WithDefaultSort(string field, SortOrder sortOrder)
    {
      if (!this.mappings.Any(m => m.Field == field))
      {
        throw new BadSortFieldException($"No mapping found for {field}");
      }

      defaultMapField = field;
      this.sortOrder = sortOrder;
      return this;
    }
  }

  internal class DefaultSort<TEntityType>
    where TEntityType : class
  {
    public DefaultSort(IMapAFilter<TEntityType> mapping, SortOrder sortOrder)
    {
      Mapping = mapping;
      SortOrder = sortOrder;
    }

    public IMapAFilter<TEntityType> Mapping { get; }
    public SortOrder SortOrder { get; }
  }
}
