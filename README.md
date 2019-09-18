# Philo.Search
Automated filter and sort of Queryable objects. Useful for automatically generating the Where clause of an IQueryable such as for LINQ to SQL.


## Setup  
Add the search service to your Startup.cs
```C#
services.AddScoped<ISearchService, SearchService>();
```

Inject the ISearchService as you would any other dependency to where you plan on using it. 

Your front end should build the filters, but to test we can create our own:
```C#
var filterSet = new FilterSet
{
  PageNumber = 1,
  PageSize = 10,
  SortBy = "code",
  SortDir = "desc",
  Filter = new FilterGroup
  {
    Operator = FilterOperator.Or,
    Filters = new List<Filter>
    {
      new Filter
      {
        Action = Comparator.Like,
        Field = "code",
        Value = "test"
      }
    },
    FilterGroups = new List<FilterGroup>()
  }
};
```

### EntityFramework 
```C#
var query = dbContext.EntityTypes
  .Include(c => c.RelatedMember)
  .AsNoTracking()
  .AsQueryable();   
   
var searchRes = searchService.DoSearch<EntityType>(query, filterSet, GetMappings());
```

and build your mappings. 

```C#
public List<IMapAFilter<EntityType>> GetMappings()
    {
      return new List<IMapAFilter<EntityType>>
      {
        new FilterMapping<EntityType, Guid>("id", et => et.Id) { IsDefaultSortFilter = true },
        new FilterMapping<EntityType, string>("code", et => et.Code),
        new FilterMapping<EntityType, string>("description", et => et.Description),
        // you can construct simple computed fields
        new FilterMapping<EntityType, string>("fullName", et => u.Code + " " + u.Description),
        // Map Collections
        new CollectionMapping<EntityType>(
          "userName"
        )
        .Any(et => et.Users)
        .Property(u => u.FirstName + " " + u.LastName)
      };
    }
```

### Computed Column
```C#
new FilterMapping<EntityType, string>("fullName", et => u.Code + " " + u.Description)
```

### Count a Collection with fixed filter values
```C#
// count the number of paid orders a customer has 
new FilterMapping<Customer, int>("paidOrderCount", a => a.Orders.Where(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Processed || o.Status == OrderStatus.InTransit || o.Status == OrderStatus.Delivered).Count()),
```

### Filter a Collection before Checking Any
```C#
  new CollectionMapping<EntityType>(
    "adminUserName"
  )
  .Any(et => et.Users.Where(u => u.Role == UserRole.Admin))
  .Property(u => u.FirstName + " " + u.LastName)
```
