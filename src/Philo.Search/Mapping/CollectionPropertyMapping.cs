using Philo.Search.Filter;
using Philo.Search.Helper;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public class CollectionPropertyMapping<TEntityType, TCollectionEntityType, TPropertyType> : IMapAFilter<TEntityType>
    where TEntityType : class
    where TCollectionEntityType : class
  {
    private readonly ICollectionAggregation<TEntityType, TCollectionEntityType> collection;
    private readonly Expression<Func<TCollectionEntityType, TPropertyType>> property;

    internal CollectionPropertyMapping(
      ICollectionAggregation<TEntityType, TCollectionEntityType> collection,
      Expression<Func<TCollectionEntityType, TPropertyType>> property
    )
    {
      this.collection = collection;
      this.property = property;
    }

    public bool IsDefaultSortFilter { get; set; }

    public string Field => collection.GetField();

    public IOrderedQueryable<TEntityType> ApplySort(IQueryable<TEntityType> query, SortOrder sortOrder)
    {
      // TODO - how do we build the sort expression??
      return query.OrderByWithDirection(x => true, sortOrder);
    }

    public IOrderedQueryable<TEntityType> ApplyThenSort(IOrderedQueryable<TEntityType> query, SortOrder sortOrder)
    {
      // TODO - how do we build the sort expression??
      return query.OrderByWithDirection(x => true, sortOrder);
    }

    public Expression<Func<TEntityType, bool>> GetFilterLambda(string value, Comparator comparator)
    {
      var propertyPredicate = SearchHelper.GetLambdaExpression<TCollectionEntityType, TPropertyType>(Field, property, value, comparator);

      return this.collection.GetPredicate<TPropertyType>(propertyPredicate);
    }
  }
}
