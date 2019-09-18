using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Philo.Search.Mapping
{
  internal enum AggregateOperation
  {
    Any = 0
  }

  /// <summary>
  /// Represents access to an ICollection
  /// </summary>
  /// <typeparam name="TRootEntityType">The type of the root entity being queried</typeparam>
  /// <typeparam name="TCollectionEntityType">The Entity within the collection</typeparam>
  internal class CollectionAggregation<TRootEntityType, TCollectionEntityType, TSubCollectionType>
    : ICollectionAggregation<TRootEntityType, TSubCollectionType>
    where TRootEntityType : class
    where TCollectionEntityType : class
    where TSubCollectionType : class
  {
    private readonly AggregateOperation operation;
    private readonly ICollectionAggregation<TRootEntityType, TCollectionEntityType> collection;
    private readonly CollectionMapping<TRootEntityType> collectionMapping;
    private readonly Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression;

    internal CollectionAggregation(
      AggregateOperation operation,
      ICollectionAggregation<TRootEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression)
    {
      this.collectionMapping = null;
      this.operation = operation;
      this.collection = collection;
      this.collectionExpression = collectionExpression;
    }

    internal CollectionAggregation(
      AggregateOperation operation,
      CollectionMapping<TRootEntityType> collectionMapping,
      Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression)
    {
      this.collection = null;
      this.operation = operation;
      this.collectionMapping = collectionMapping;
      this.collectionExpression = collectionExpression;
    }

    public string GetField()
    {
      if (collectionMapping != null)
      {
        return collectionMapping.GetField();
      }

      return collection.GetField();
    }

    public Expression<Func<TRootEntityType, bool>> GetPredicate<TCollectionSubType>(
      Expression<Func<TSubCollectionType, bool>> methodPredicate
    )
    {
      Expression finalExpression;
      switch (operation)
      {
        case AggregateOperation.Any:
          // public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate);
          var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                      .First(m => m.Name == "Any" && m.GetParameters().Count() == 2)
                      .MakeGenericMethod(typeof(TSubCollectionType));

          /*
           * call the Any method. As it is static we call it using null, first param the source and second param
           * the predicate built in Step 1
           */
          finalExpression = Expression.Call(null, anyMethod, collectionExpression.Body, methodPredicate);
          break;
        default:
          throw new NotImplementedException($"CollectionOperation.{operation} has not been implemented");
      }

      if (this.collectionMapping != null)
      {
        return Expression.Lambda<Func<TRootEntityType, bool>>(finalExpression, collectionExpression.Parameters[0]);
      }

      var parentExp = this.collection.GetPredicate<TCollectionSubType>(
        Expression.Lambda<Func<TCollectionEntityType, bool>>(finalExpression, collectionExpression.Parameters[0])
      );

      return Expression.Lambda<Func<TRootEntityType, bool>>(parentExp.Body, parentExp.Parameters[0]);
    }
  }
}
