namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Serializes and deserializes values for infrastructure concerns such as caching.
/// </summary>
public interface ISerializeService
{
    /// <summary>
    /// Serializes a value to string form.
    /// </summary>
    string Serialize<T>(T value);

    /// <summary>
    /// Serializes a value using an explicit runtime type.
    /// </summary>
    string Serialize<T>(T value, Type type);

    /// <summary>
    /// Deserializes a string payload into the requested type.
    /// </summary>
    T? Deserialize<T>(string value);
}
