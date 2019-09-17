using LinqKit;
using Philo.Search.Filter;
using Philo.Search.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Philo.Search
{
  public static class SearchHelper
  {
    public static Expression<Func<T, bool>>
        CreateSearchPredicate<T>(FilterSet filterSet,
        List<IMapAFilter<T>> mappings
      ) where T : class
    {
      return GetExpression<T>(mappings, filterSet.Filter);
    }

    private static Expression<Func<T, bool>> GetExpression<T>(
      List<IMapAFilter<T>> mappings,
      FilterGroup filterGroup
    ) where T : class
    {
      var predicate = PredicateBuilder.New<T>(filterGroup.Operator == FilterOperator.And);
      var filterApplied = false;

      foreach (var filter in filterGroup.Filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
      {
        var mapping = mappings.FirstOrDefault(m => m.Field == filter.Field);

        if (mapping == null)
        {
          // todo throw exception so consumer knows their filters aren't being applied
          throw new BadFilterFieldException($"{filter.Field} is not known");
        }

        var expression = mapping.GetFilterLambda(filter.Value, filter.Action);

        if (expression == null)
        {
          continue;
        }

        filterApplied = true;

        if (filterGroup.Operator == FilterOperator.And)
        {
          predicate = predicate.And(expression);
        }
        else
        {
          predicate = predicate.Or(expression);
        }
      }

      // if an or filter doesnt actually do any filtering, we return true. 
      // this allows us to ignore empty predicates
      if (!filterApplied && filterGroup.Operator == FilterOperator.Or)
      {
        predicate = PredicateBuilder.New<T>(true);
      }

      if (filterGroup.FilterGroups != null)
      {
        foreach (var group in filterGroup.FilterGroups)
        {
          if (filterGroup.Operator == FilterOperator.And)
          {
            predicate = predicate.And(GetExpression<T>(mappings, group));
          }
          else
          {
            predicate = predicate.Or(GetExpression<T>(mappings, group));
          }
        }
      }

      return predicate;
    }

    public static Expression<Func<TEntityType, bool>> GetLambdaExpression<TEntityType, TPropType>(
      Expression<Func<TEntityType, TPropType>> mapping,
      string value,
      Comparator comparator
    ) where TEntityType : class
    {
      if (mapping.ReturnType.IsEnum)
      {
        return HandleEnum(mapping, value, comparator);
      }

      object parsedValue;
      switch (mapping.ReturnType.Name)
      {
        case nameof(Guid):
          parsedValue = Guid.Parse(value);
          break;
        case nameof(String):
          parsedValue = value;
          break;
        case nameof(Int32):
          if (!int.TryParse(value, out int parsedInt))
          {
            return null;
          }

          parsedValue = parsedInt;
          break;
        case nameof(Boolean):
          {
            if (!bool.TryParse(value, out bool parsedBool))
            {
              return null;
            }

            parsedValue = parsedBool;
            break;
          }
        default:
          throw new NotImplementedException($"Cannot map {mapping.ReturnType.Name} at this time");
      }

      Expression filterValue = Expression.Constant(parsedValue);

      Expression theOperation;

      switch (comparator)
      {
        case Comparator.Eq:
          theOperation = Expression.MakeBinary(ExpressionType.Equal, mapping.Body, filterValue);
          break;
        case Comparator.Gt:
          theOperation = Expression.MakeBinary(ExpressionType.GreaterThan, mapping.Body, filterValue);
          break;
        case Comparator.Lt:
          theOperation = Expression.MakeBinary(ExpressionType.LessThan, mapping.Body, filterValue);
          break;
        case Comparator.Like:
          if (mapping.ReturnType != typeof(string))
          {
            throw new BadFilterComparatorException($"Field cannot use the {comparator} comparator");
          }

          MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string), typeof(StringComparison) });

          // coalesce the string value to an empty string if null
          var nullCoalesce = Expression.Coalesce(mapping.Body, Expression.Constant(string.Empty));
          theOperation = Expression.Call(nullCoalesce, containsMethod, filterValue, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
          break;
        default:
          throw new NotSupportedException($"Action {comparator} is not supported yet");
      }

      return Expression.Lambda<Func<TEntityType, bool>>(theOperation, mapping.Parameters[0]);
    }

    private static Expression<Func<TEntityType, bool>> HandleEnum<TEntityType, TPropType>(
      Expression<Func<TEntityType, TPropType>> mapping,
      string value,
      Comparator comparator)
    {
      var enumValuesToMatchTo = new List<TPropType>();

      switch (comparator)
      {
        case Comparator.Gt:
          {
            var gtEqEnumValue = EnumValueExact<TPropType>(value);
            if (gtEqEnumValue == null)
            {
              return BoolResult<TEntityType>(false, mapping.Parameters[0]);
            }

            enumValuesToMatchTo = EnumIntValues<TPropType>()
              .Where(ev => Convert.ToInt32(ev) > Convert.ToInt32(gtEqEnumValue))
              .ToList();
            break;
          }
        case Comparator.GtEq:
          {
            var gtEqEnumValue = EnumValueExact<TPropType>(value);
            if (gtEqEnumValue == null)
            {
              return BoolResult<TEntityType>(false, mapping.Parameters[0]);
            }

            enumValuesToMatchTo = EnumIntValues<TPropType>()
              .Where(ev => Convert.ToInt32(ev) >= Convert.ToInt32(gtEqEnumValue))
              .ToList();
            break;
          }
        case Comparator.Lt:
          {
            var gtEqEnumValue = EnumValueExact<TPropType>(value);
            if (gtEqEnumValue == null)
            {
              return BoolResult<TEntityType>(false, mapping.Parameters[0]);
            }

            enumValuesToMatchTo = EnumIntValues<TPropType>()
              .Where(ev => Convert.ToInt32(ev) < Convert.ToInt32(gtEqEnumValue))
              .ToList();
            break;
          }
        case Comparator.Like:
          {
            enumValuesToMatchTo = EnumLike<TPropType>(value);
            break;
          }
        case Comparator.Eq:
        default:
          {
            var eqEnumValue = EnumValueExact<TPropType>(value);
            if (eqEnumValue != null)
            {
              Expression eqValue = Expression.Constant(eqEnumValue, typeof(TPropType));
              Expression eqOperation = Expression.MakeBinary(ExpressionType.Equal, mapping.Body, eqValue);
              return Expression.Lambda<Func<TEntityType, bool>>(eqOperation, mapping.Parameters[0]);
            }

            // no matching enum values, return false
            return BoolResult<TEntityType>(false, mapping.Parameters[0]);
          }
      }

      if (enumValuesToMatchTo.Count > 0)
      {
        var containsMethod = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
          .First(m => m.Name == "Contains" && m.GetParameters().Count() == 2)
          .MakeGenericMethod(typeof(TPropType));

        Expression enumValues = Expression.Constant(enumValuesToMatchTo);

        var containsExpression = Expression.Call(
          null,
          containsMethod,
          enumValues,
          mapping.Body
        );

        return Expression.Lambda<Func<TEntityType, bool>>(containsExpression, mapping.Parameters[0]);
      }

      // no matching enum values, return false
      return BoolResult<TEntityType>(false, mapping.Parameters[0]);
    }

    private static object EnumValueExact<TEnumType>(string value)
    {
      try
      {
        var res = Enum.Parse(typeof(TEnumType), value);
        return res;
      } catch (Exception)
      {
        return null;
      }
    }

    private static List<TEnumType> EnumLike<TEnumType>(string value)
    {
      var values = Enum.GetValues(typeof(TEnumType));
      var matched = new List<TEnumType>();

      foreach (var item in values)
      {
        var itemValue = item.ToString();
        if (itemValue.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0)
        {
          matched.Add((TEnumType)Enum.Parse(typeof(TEnumType), itemValue));
        }
      }

      return matched;
    }

    private static List<TEnumType> EnumIntValues<TEnumType>()
    {
      return Enum.GetValues(typeof(TEnumType))
        .Cast<TEnumType>()
        .ToList();
    }

    private static Expression<Func<TEntityType, bool>> BoolResult<TEntityType>(bool result, ParameterExpression param)
    {
      Expression filterValue = Expression.Constant(result);
      Expression theOperation = Expression.MakeBinary(ExpressionType.Equal, filterValue, filterValue);
      return Expression.Lambda<Func<TEntityType, bool>>(theOperation, param);
    }
  }
}
