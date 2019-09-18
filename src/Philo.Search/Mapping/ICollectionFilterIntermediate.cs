using System;
using System.Linq.Expressions;

namespace Philo.Search.Mapping
{
  public interface ICollectionFilterIntermediate<TRootEntityType, TCollectionEntityType>
    where TCollectionEntityType : class
  {
    Expression<Func<TRootEntityType, bool>> GetPredicate<TCollectionSubType>(
      Expression<Func<TCollectionEntityType, bool>> methodPredicate
    );

    string GetField();
  }
}
