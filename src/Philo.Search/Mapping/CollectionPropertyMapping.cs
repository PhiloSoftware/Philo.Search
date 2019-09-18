using Philo.Search.Filter;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public class CollectionPropertyMapping<TEntityType, TCollectionEntityType, TPropertyType> : IMapAFilter<TEntityType>
    where TEntityType : class
    where TCollectionEntityType : class
  {
    private readonly ICollectionFilterIntermediate<TEntityType, TCollectionEntityType> collection;
    private readonly Expression<Func<TCollectionEntityType, TPropertyType>> property;

    public CollectionPropertyMapping(
      ICollectionFilterIntermediate<TEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, TPropertyType>> property
    )
    {
      this.collection = collection;
      this.property = property;
    }

    public bool IsDefaultSortFilter { get; set; }

    public string Field => collection.GetField();

    public IQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, bool descending)
    {
      // TODO - how do we build the sort expression??
      return query;
    }

    public Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator)
    {
      var propertyPredicate = SearchHelper.GetLambdaExpression<TCollectionEntityType, TPropertyType>(property, value, comparator);

      return this.collection.GetPredicate<TPropertyType>(propertyPredicate);
    }
  }
}
