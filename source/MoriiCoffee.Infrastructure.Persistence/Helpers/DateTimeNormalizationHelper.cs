using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MoriiCoffee.Infrastructure.Persistence.Helpers;

public static class DateTimeNormalizationHelper
{
    public static void NormalizeTrackedDateTimes(EntityEntry entry)
    {
        foreach (var property in entry.Properties)
        {
            var clrType = property.Metadata.ClrType;
            if (clrType == typeof(DateTime))
            {
                if (property.CurrentValue is DateTime value)
                {
                    property.CurrentValue = NormalizeToUtc(value);
                }
            }
            else if (clrType == typeof(DateTime?))
            {
                if (property.CurrentValue is DateTime value)
                {
                    property.CurrentValue = NormalizeToUtc(value);
                }
            }
        }
    }

    public static void NormalizeObjectGraph(object entity)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        NormalizeObjectGraph(entity, visited);
    }

    public static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
    }

    private static void NormalizeObjectGraph(object? entity, HashSet<object> visited)
    {
        if (entity is null || !visited.Add(entity))
        {
            return;
        }

        var type = entity.GetType();
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
        {
            return;
        }

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            var propertyType = property.PropertyType;
            var value = property.GetValue(entity);

            if (propertyType == typeof(DateTime) && value is DateTime dateTime)
            {
                property.SetValue(entity, NormalizeToUtc(dateTime));
                continue;
            }

            if (propertyType == typeof(DateTime?) && value is DateTime nullableDateTime)
            {
                property.SetValue(entity, NormalizeToUtc(nullableDateTime));
                continue;
            }

            if (value is null || value is string)
            {
                continue;
            }

            if (value is System.Collections.IEnumerable enumerable && propertyType != typeof(byte[]))
            {
                foreach (var item in enumerable)
                {
                    NormalizeObjectGraph(item, visited);
                }

                continue;
            }

            if (!propertyType.IsPrimitive && !propertyType.IsEnum)
            {
                NormalizeObjectGraph(value, visited);
            }
        }
    }
}
