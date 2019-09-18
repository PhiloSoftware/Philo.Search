using System;
using System.Runtime.Serialization;

namespace Philo.Search
{
  [Serializable]
  public class FilterException : Exception
  {
    public FilterException()
    {
    }

    public FilterException(string message) : base(message)
    {
    }

    public FilterException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected FilterException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadFilterValueException : FilterException
  {
    public BadFilterValueException()
    {
    }

    public BadFilterValueException(string message) : base(message)
    {
    }

    public BadFilterValueException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BadFilterValueException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadFilterFieldException : FilterException
  {
    public BadFilterFieldException()
    {
    }

    public BadFilterFieldException(string message) : base(message)
    {
    }

    public BadFilterFieldException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BadFilterFieldException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadSortFieldException : FilterException
  {
    internal BadSortFieldException(string message) : base(message)
    {
    }

    protected internal BadSortFieldException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadSortValueException : FilterException
  {
    public BadSortValueException()
    {
    }

    public BadSortValueException(string message) : base(message)
    {
    }

    public BadSortValueException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BadSortValueException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadFilterComparatorException : FilterException
  {
    public BadFilterComparatorException()
    {
    }

    public BadFilterComparatorException(string message) : base(message)
    {
    }

    public BadFilterComparatorException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BadFilterComparatorException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }

  [Serializable]
  public class BadFilterMappingException<T> : FilterException where T : class
  {
    public BadFilterMappingException()
    {
    }

    public BadFilterMappingException(string message) : base(message)
    {
    }

    public BadFilterMappingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected BadFilterMappingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
