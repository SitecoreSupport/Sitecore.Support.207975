using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Marketing.Taxonomy.Data.Entities;
using Sitecore.Marketing.Taxonomy.Mapping;

namespace Sitecore.Support.Marketing.Taxonomy.Mapping
{
  /// <summary>Defines the TaxonomyTypeMapper class.</summary>
  public class TaxonomyTypeMapper : ITaxonomyTypeMapper
  {
    /// <summary>The map cache.</summary>
    private readonly IDictionary<string, IMapper> _mapCache = new ConcurrentDictionary<string, IMapper>();

    /// <summary>The mappers.</summary>
    private readonly List<IMapper> _mappers = new List<IMapper>();

    /// <summary>Maps the specified data.</summary>
    /// <param name="data">The data source to map.</param>
    /// <param name="type">The type to map the source to.</param>
    /// <returns>The derived object instance.</returns>
    public object Map(TaxonEntity data, Type type)
    {
      Assert.ArgumentNotNull(type, "type");
      Assert.ArgumentNotNull(data, "data");
      var mapper = ResolveMapper(type);

      return mapper.Map(data);
    }

    /// <summary>Maps the specified data.</summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="data">The data source to map.</param>
    /// <returns>The derived object instance.</returns>
    public T Map<T>(TaxonEntity data)
    {
      return (T)Map(data, typeof(T));
    }

    /// <summary>Tries to map the given object to the specified type.</summary>
    /// <param name="data">The object to map from.</param>
    /// <param name="type">The type to map to.</param>
    /// <param name="result">An instance of type [to].</param>
    /// <returns>True if successful, otherwise false.</returns>
    public bool TryMap(TaxonEntity data, Type type, out object result)
    {
      Assert.ArgumentNotNull(type, "type");
      try
      {
        var mapper = ResolveMapper(type);
        return mapper.TryMap(data, out result);
      }
      catch
      {
        result = null;
        return false;
      }
    }

    /// <summary>Maps the given object to the specified type.</summary>
    /// <typeparam name="T">The type to map to.</typeparam>
    /// <param name="data">The source data.</param>
    /// <param name="result">An instance of type [T].</param>
    /// <returns>An instance of [T].</returns>
    public bool TryMap<T>(TaxonEntity data, out T result)
    {
      object untypedResult;
      var res = TryMap(data, typeof(T), out untypedResult);
      result = (T)untypedResult;
      return res;
    }

    /// <summary>Maps the given object to a <see cref="TaxonEntity"/>.</summary>
    /// <param name="data">The data to map.</param>
    /// <returns>An instance of <see cref="TaxonEntity" />.</returns>
    public TaxonEntity MapToEntity(object data)
    {
      Assert.ArgumentNotNull(data, "data");
      var type = data.GetType();
      var mapper = ResolveMapper(type);

      return mapper.MapToEntity(data);
    }

    /// <summary>Adds the mapper.</summary>
    /// <param name="mapper">The mapper.</param>
    public void AddMapper([NotNull] IMapper mapper)
    {
      Assert.ArgumentNotNull(mapper, "mapper");
      lock (_mappers)
      {
        _mappers.Add(mapper);
      }
    }

    /// <summary>Resolves the mapper.</summary>
    /// <param name="type">The type to map to.</param>
    /// <returns>An IMapper for the type, otherwise null.</returns>
    [NotNull]
    private IMapper ResolveMapper([NotNull] Type type)
    {
      Assert.ArgumentNotNull(type, "type");
      var cacheKey = type.ToString();

      if (_mapCache.ContainsKey(cacheKey))
      {
        return _mapCache[cacheKey];
      }

      IMapper mapper;
      lock (_mappers)
      {
        mapper = _mappers.FirstOrDefault(b => b.Maps(type));
      }

      if (mapper != null)
      {
        _mapCache[cacheKey] = mapper;
      }

      if (mapper == null)
      {
        throw new MapperNotFoundException(type);
      }

      return mapper;
    }
  }
}