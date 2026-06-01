using System.Text.Json;

namespace TripMate_WebAPI.Extensions
{
    public static class JsonExtensions
    {
        public static T? SafeDeserialize<T>(this string json, JsonSerializerOptions? options = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Deserialization failed for type {typeof(T).Name}: {ex.Message}");
                Console.WriteLine($"JSON Content: {json}");
                return null;
            }
        }

        public static List<T>? SafeDeserializeList<T>(this string json, JsonSerializerOptions? options = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return new List<T>();

            try
            {
                // Try as array first
                var list = JsonSerializer.Deserialize<List<T>>(json, options);
                return list ?? new List<T>();
            }
            catch (JsonException)
            {
                try
                {
                    // Try as single object
                    var single = JsonSerializer.Deserialize<T>(json, options);
                    return single != null ? new List<T> { single } : new List<T>();
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Deserialization failed for List<{typeof(T).Name}>: {ex.Message}");
                    Console.WriteLine($"JSON Content: {json}");
                    return new List<T>();
                }
            }
        }

        public static bool IsValidJson(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}