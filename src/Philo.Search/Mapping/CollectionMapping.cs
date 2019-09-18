namespace Philo.Search.Mapping
{
  /// <summary>
  /// Start the process of mapping a collection to a specific value
  /// </summary>
  /// <typeparam name="TEntityType">The type of entity to be mapped</typeparam>
  public class CollectionMapping<TEntityType>
  {
    private readonly string field;

    public CollectionMapping(
      string field
    )
    {
      this.field = field;
    }

    internal string GetField()
    {
      return field;
    }
  }
}
