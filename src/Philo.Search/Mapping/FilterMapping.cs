using Philo.Search.Filter;
using Philo.Search.Helper;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Philo.Search.Mapping
{
  public class FilterMapping<TEntityType, TPropType>
    : IMapAFilter<TEntityType>
    where TEntityType : class
  {
    public FilterMapping(string field, Expression<Func<TEntityType, TPropType>> mapping)
    {
      Field = field;
      Mapping = mapping;

      //var memberExpression = mapping.Body as MemberExpression;
      //var binaryExpression = mapping.Body as BinaryExpression;
      //var methodExpression = mapping.Body as MethodCallExpression;

      //if (memberExpression == null && binaryExpression == null && methodExpression == null)
      //{
      //  throw new NotSupportedException("Expression type is not currently suppor");
      //}
      //if ((binaryExpression == null && memberExpression == null)
      //  || (memberExpression != null && memberExpression.Member.MemberType != MemberTypes.Property))
      //{
      //  throw new NotSupportedException("Expression must be member expression and refer to a property");
      //}
    }

    public string Field { get; set; }

    public bool IsDefaultSortFilter { get; set; } = false;
    private Expression<Func<TEntityType, TPropType>> Mapping { get; set; }

    public IQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, bool descending)
    {
      return query.OrderByWithDirection(Mapping, descending);
    }

    public Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator)
    {
      return SearchHelper.GetLambdaExpression<TEntityType, TPropType>(Field, Mapping, value, comparator);
    }
  }
}
