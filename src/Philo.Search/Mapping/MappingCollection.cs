using System.Collections.Generic;
using System.Linq;

namespace Philo.Search.Mapping
{
  public class MappingCollection<TEntityType>
    where TEntityType : class
  {
    private readonly IEnumerable<IMapAFilter<TEntityType>> mappings;
    private string defaultMapField;
    private bool defaultSortDescending;

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
            defaultSortDescending
          );
        }

        var defaultSort = mappings
          .FirstOrDefault(m => m.IsDefaultSortFilter);

        if (defaultSort != null)
        {
          return new DefaultSort<TEntityType>(defaultSort, defaultSortDescending);
        }

        if (mappings.Count() > 0)
        {
          return new DefaultSort<TEntityType>(mappings.First(), defaultSortDescending);
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

    public MappingCollection<TEntityType> WithDefaultSort(string field, bool isDescending)
    {
      if (!this.mappings.Any(m => m.Field == field))
      {
        throw new BadSortFieldException($"No mapping found for {field}");
      }

      defaultMapField = field;
      defaultSortDescending = isDescending;
      return this;
    }
  }

  internal class DefaultSort<TEntityType>
    where TEntityType : class
  {
    public DefaultSort(IMapAFilter<TEntityType> mapping, bool descending)
    {
      Mapping = mapping;
      Descending = descending;
    }

    public IMapAFilter<TEntityType> Mapping { get; }
    public bool Descending { get; }
  }
}
