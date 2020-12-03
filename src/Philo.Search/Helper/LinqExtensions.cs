using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Philo.Search.Helper
{
  internal static class LinqExtensions
  {
    public static IOrderedEnumerable<TSource> OrderByWithDirection<TSource, TKey>
      (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool descending)
    {
      return descending ? source.OrderByDescending(keySelector)
                        : source.OrderBy(keySelector);
    }

    public static IOrderedQueryable<TSource> OrderByWithDirection<TSource, TKey>
      (this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool descending)
    {
      return descending ? source.OrderByDescending(keySelector)
                        : source.OrderBy(keySelector);
    }

    public static IOrderedQueryable<TSource> ThenByWithDirection<TSource, TKey>
      (this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, bool descending)
    {
      return descending ? source.ThenByDescending(keySelector)
                        : source.ThenBy(keySelector);
    }
  }
}
