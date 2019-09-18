# Philo.Search
Automated filter and sort of Queryable objects. Useful for automatically generating the Where clause of an IQuerable such as for Entity Framework.


## Setup  
Add the search service to your Startup.cs
```C#
services.AddScoped<ISearchService, SearchService>();
```

Inject the ISearchService as you would any other dependency. 

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
