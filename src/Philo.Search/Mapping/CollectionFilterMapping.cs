using Philo.Search.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Philo.Search.Mapping
{
  public enum CollectionOperation
  {
    Any
  }

  public class CollectionFilterMapping<TEntityType, TCollectionType, TPropType>
    : IMapAFilter<TEntityType>
    where TEntityType : class
    where TCollectionType : class
  {
    private readonly CollectionOperation operation;
    private readonly Expression<Func<TEntityType, ICollection<TCollectionType>>> mapping;
    private readonly Expression<Func<TCollectionType, TPropType>> collectionMapping;

    public CollectionFilterMapping(
      string field,
      CollectionOperation operation,
      Expression<Func<TEntityType, ICollection<TCollectionType>>> mapping,
      Expression<Func<TCollectionType, TPropType>> collectionMapping
    )
    {
      Field = field;
      this.operation = operation;
      this.mapping = mapping;
      this.collectionMapping = collectionMapping;
    }

    public string Field { get; set; }
    public bool IsDefaultSortFilter { get; set; } = false;

    public IQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, bool descending)
    {
      return query;
    }

    public Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator)
    {
      Expression filterValue = Expression.Constant(value.ParseValueAsType(collectionMapping.ReturnType));

      // Step 1: build the expression for the collection operation e.g Any(x => x [comparator] p)
      var collectionPredicate = SearchHelper.GetLambdaExpression<TCollectionType, TPropType>(collectionMapping, value, comparator);

      // Step 2: Build the collection filter
      Expression finalExpression;
      switch (operation)
      {
        case CollectionOperation.Any:
          // public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate);
          var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                      .First(m => m.Name == "Any" && m.GetParameters().Count() == 2)
                      .MakeGenericMethod(typeof(TCollectionType));

          /*
           * call the Any method. As it is static we call it using null, first param the source and second param
           * the predicate built in Step 1
           */
          finalExpression = Expression.Call(null, anyMethod, mapping.Body, collectionPredicate);
          break;
        default:
          throw new NotImplementedException($"CollectionOperation.{operation} has not been implemented");
      }

      return Expression.Lambda<Func<TEntityType, bool>>(finalExpression, mapping.Parameters[0]);
    }
  }

}
