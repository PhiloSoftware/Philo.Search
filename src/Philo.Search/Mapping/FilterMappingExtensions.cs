using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public static class FilterMappingExtensions
  {
    /// <summary>
    /// An intermediate mapping object that references a collection of <typeparamref name="TCollectionEntityType"/> found
    /// on <typeparamref name="TEntityType"/>
    /// </summary>
    /// <typeparam name="TRootEntityType"></typeparam>
    /// <typeparam name="TEntityType"></typeparam>
    /// <typeparam name="TCollectionEntityType"></typeparam>
    /// <param name="collection"></param>
    /// <param name="collectionExpression"></param>
    /// <returns></returns>
    public static ICollectionFilterIntermediate<TRootEntityType, TCollectionEntityType>
      Any<TRootEntityType, TCollectionEntityType>(
      this CollectionMapping<TRootEntityType> collectionRootMapping,
      Expression<Func<TRootEntityType, ICollection<TCollectionEntityType>>> collectionExpression
    )
      where TRootEntityType : class
      where TCollectionEntityType : class
    {
      return new CollectionExpression<TRootEntityType, TRootEntityType, TCollectionEntityType>(
        CollectionOperation.Any,
        collectionRootMapping,
        collectionExpression
      );
    }

    /// <summary>
    /// An intermediate mapping object that references a collection of <typeparamref name="TCollectionEntityCollectionType"/> found
    /// on <typeparamref name="TCollectionEntityType"/>
    /// </summary>
    /// <typeparam name="TRootEntityType"></typeparam>
    /// <typeparam name="TCollectionEntityType"></typeparam>
    /// <typeparam name="TCollectionEntityCollectionType"></typeparam>
    /// <param name="collection"></param>
    /// <param name="collectionExpression"></param>
    /// <returns></returns>
    public static ICollectionFilterIntermediate<TRootEntityType, TCollectionEntityCollectionType>
      Any<TRootEntityType, TCollectionEntityType, TCollectionEntityCollectionType>(
      this ICollectionFilterIntermediate<TRootEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, ICollection<TCollectionEntityCollectionType>>> collectionExpression
    )
      where TRootEntityType : class
      where TCollectionEntityType : class
      where TCollectionEntityCollectionType : class
    {
      return new CollectionExpression<TRootEntityType, TCollectionEntityType, TCollectionEntityCollectionType>(
        CollectionOperation.Any,
        collection,
        collectionExpression
      );
    }

    /// <summary>
    /// Generate the actual mapping object by navigating to a property.
    /// </summary>
    /// <typeparam name="TEntityType">The root entity type that we are creating a mapping for</typeparam>
    /// <typeparam name="TCollectionEntityType">The type of object we are getting the property of</typeparam>
    /// <typeparam name="TPropType">The property to access</typeparam>
    /// <param name="collection">The collection filter expression that identifies the collection</param>
    /// <param name="cpop">The expression to the property</param>
    /// <returns>A mapping object from <typeparamref name="TEntityType"/> to <typeparamref name="TPropType"/></returns>
    public static CollectionPropertyMapping<TEntityType, TCollectionEntityType, TPropType> Property<TEntityType, TCollectionEntityType, TPropType>(
      this ICollectionFilterIntermediate<TEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, TPropType>> cpop
    )
      where TEntityType : class
      where TCollectionEntityType : class
    {
      // todo, check cpop is actually a property

      return new CollectionPropertyMapping<TEntityType, TCollectionEntityType, TPropType>(collection, cpop);
    }
  }
}
