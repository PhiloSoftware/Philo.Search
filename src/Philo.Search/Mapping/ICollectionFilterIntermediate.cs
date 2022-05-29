using System;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public interface ICollectionAggregation<TRootEntityType, TCollectionEntityType>
  {
    Expression<Func<TRootEntityType, bool>> GetPredicate<TCollectionSubType>(
      Expression<Func<TCollectionEntityType, bool>> methodPredicate
    );

    string GetField();
  }
}
