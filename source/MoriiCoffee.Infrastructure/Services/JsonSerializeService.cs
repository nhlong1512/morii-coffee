using MoriiCoffee.Application.SeedWork.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// JSON serializer used by infrastructure services such as Redis caching.
/// </summary>
public class JsonSerializeService : ISerializeService
{
    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Converters = new List<JsonConverter>
        {
            new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        }
    };

    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, _serializerSettings);
    }

    public string Serialize<T>(T value, Type type)
    {
        return JsonConvert.SerializeObject(value, type, _serializerSettings);
    }

    public T? Deserialize<T>(string value)
    {
        return JsonConvert.DeserializeObject<T>(value, _serializerSettings);
    }
}
