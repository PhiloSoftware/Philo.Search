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
  internal static class SearchHelper
  {
    public static Expression<Func<T, bool>>
        CreateSearchPredicate<T>(FilterSet filterSet,
        MappingCollection<T> mappings
      ) where T : class
    {
      return GetExpression<T>(mappings, filterSet.Filter);
    }

    private static Expression<Func<T, bool>> GetExpression<T>(
      MappingCollection<T> mappings,
      FilterGroup filterGroup
    ) where T : class
    {
      var predicate = PredicateBuilder.New<T>(filterGroup.Operator == FilterOperator.And);
      var filterApplied = false;

      foreach (var filter in filterGroup.Filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
      {
        var mapping = mappings.GetMapping(filter.Field);

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
      var returntype = mapping.ReturnType;

      // if it is nullable, lets escape this first up
      var nullable = Nullable.GetUnderlyingType(returntype);
      if (nullable != null)
      {
        returntype = nullable;
      }

      // Enums. We do our processing server side as they generally
      // aren't a DB construct.
      if (returntype.IsEnum)
      {
        return HandleEnum(mapping, value, comparator);
      }

      object parsedValue = null;
      if (returntype == typeof(DateTimeOffset) || returntype == typeof(DateTime))
      {
        parsedValue = ParseDateTime(mapping, value, comparator);
      }

      // convert the string value to the actual type
      var getConvertedValue = new Func<Expression>(() =>
      {
        if (parsedValue != null)
        {
          return Expression.Constant(parsedValue, mapping.ReturnType);
        }

        return Expression.Constant(Convert.ChangeType(value, returntype), mapping.ReturnType);
      });

      Expression theOperation;
      switch (comparator)
      {
        case Comparator.Eq:
          theOperation = Expression.MakeBinary(ExpressionType.Equal, mapping.Body, getConvertedValue());
          break;
        case Comparator.Gt:
          theOperation = Expression.MakeBinary(ExpressionType.GreaterThan, mapping.Body, getConvertedValue());
          break;
        case Comparator.Lt:
          theOperation = Expression.MakeBinary(ExpressionType.LessThan, mapping.Body, getConvertedValue());
          break;
        case Comparator.Like:
          {
            // if it is a string
            Expression valueExpression;

            if (returntype == typeof(string))
            {
              // coalesce the string value to an empty string if null
              valueExpression = Expression.Coalesce(mapping.Body, Expression.Constant(string.Empty));
            }
            else
            {
              // otherwise just ToString it and the LINQ to SQL provider can deal with it
              valueExpression = Expression.Call(mapping.Body, typeof(object).GetMethod("ToString"));
            }

            MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string), typeof(StringComparison) });

            Expression stringFilter = Expression.Constant(value);
            theOperation = Expression.Call(valueExpression, containsMethod, stringFilter, Expression.Constant(StringComparison.InvariantCultureIgnoreCase));
            break;
          }
        default:
          throw new NotSupportedException($"Action {comparator} is not supported yet");
      }

      return Expression.Lambda<Func<TEntityType, bool>>(theOperation, mapping.Parameters[0]);
    }

    private static object ParseDateTime<TEntityType, TPropType>(
      Expression<Func<TEntityType, TPropType>> mapping,
      string value,
      Comparator comparator
    ) where TEntityType : class
    {
      if (!DateTimeOffset.TryParse(value, out DateTimeOffset result))
      {
        throw new BadFilterValueException($"Value {value} could not be parsed as a date");
      }

      var propertyType = typeof(TPropType);
      var nullable = Nullable.GetUnderlyingType(propertyType);
      if (nullable != null)
      {
        if (nullable == typeof(DateTimeOffset))
        {
          return (DateTimeOffset?)result;
        }
        else if (nullable == typeof(DateTime))
        {
          return (DateTime?)result.Date;
        }
        else
        {
          throw new NotImplementedException("This nullable date cannot be interpreted at this time");
        }
      }

      if (propertyType == typeof(DateTimeOffset))
      {
        return result;
      }
      else if (propertyType == typeof(DateTime))
      {
        return result.Date;
      }
      else
      {
        throw new NotImplementedException("This date type cannot be interpreted at this time");
      }
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
      }
      catch (Exception)
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
