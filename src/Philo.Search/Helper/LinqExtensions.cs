using Philo.Search.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Philo.Search.Helper
{
  internal static class LinqExtensions
  {
    public static IOrderedEnumerable<TSource> OrderByWithDirection<TSource, TKey>
      (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, SortOrder sortOrder)
    {
      return sortOrder == SortOrder.Descending ? source.OrderByDescending(keySelector)
                        : source.OrderBy(keySelector);
    }

    public static IOrderedQueryable<TSource> OrderByWithDirection<TSource, TKey>
      (this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortOrder sortOrder)
    {
      return sortOrder == SortOrder.Descending ? source.OrderByDescending(keySelector)
                        : source.OrderBy(keySelector);
    }

    public static IOrderedQueryable<TSource> ThenByWithDirection<TSource, TKey>
      (this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortOrder sortOrder)
    {
      return sortOrder == SortOrder.Descending ? source.ThenByDescending (keySelector)
                        : source.ThenBy(keySelector);
    }
  }
}
