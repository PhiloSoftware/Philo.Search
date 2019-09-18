using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Philo.Search.Mapping
{

  public interface ICollectionFilter<TRootEntityType, TCollectionEntityType>
    where TCollectionEntityType : class
  {
    Expression<Func<TRootEntityType, bool>> GetPredicate<TCollectionSubType>(
      Expression<Func<TCollectionEntityType, bool>> methodPredicate
    );

    string GetField();
  }

  /// <summary>
  /// Represents a filter on an ICollection
  /// </summary>
  /// <typeparam name="TRootEntityType">The type of the root entity being queried</typeparam>
  /// <typeparam name="TCollectionEntityType">The Entity within the collection</typeparam>
  internal class CollectionWhere<TRootEntityType, TCollectionEntityType, TSubCollectionType>
    : ICollectionAggregation<TRootEntityType, TSubCollectionType>
    where TRootEntityType : class
    where TCollectionEntityType : class
    where TSubCollectionType : class
  {
    private readonly ICollectionAggregation<TRootEntityType, TCollectionEntityType> collection;
    private readonly CollectionMapping<TRootEntityType> collectionMapping;
    private readonly Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression;

    internal CollectionWhere(
      ICollectionAggregation<TRootEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression)
    {
      this.collectionMapping = null;
      this.collection = collection;
      this.collectionExpression = collectionExpression;
    }

    internal CollectionWhere(
      CollectionMapping<TRootEntityType> collectionMapping,
      Expression<Func<TCollectionEntityType, ICollection<TSubCollectionType>>> collectionExpression)
    {
      this.collection = null;
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

      var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                  .First(m => m.Name == "Where" && m.GetParameters().Count() == 2)
                  .MakeGenericMethod(typeof(TSubCollectionType));

      /*
        * call the Any method. As it is static we call it using null, first param the source and second param
        * the predicate built in Step 1
        */
      finalExpression = Expression.Call(null, anyMethod, collectionExpression.Body, methodPredicate);

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
