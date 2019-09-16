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
    IQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, bool descending);
    Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator);
  }

  public static class FilterExtensions
  {
    public static object ParseValueAsType(this string value, Type mappedValueType)
    {
      object parsedValue = null;
      switch (mappedValueType.Name)
      {
        case nameof(Guid):
          parsedValue = Guid.Parse(value);
          break;
        case nameof(String):
          parsedValue = value;
          break;
        case nameof(Int32):
          if (!int.TryParse(value, out int parsedInt))
          {
            return null;
          }

          parsedValue = parsedInt;
          break;
      }

      return parsedValue;
    }
  }
}
