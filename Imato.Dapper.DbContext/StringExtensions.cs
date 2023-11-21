using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imato.Dapper.DbContext
{
    public static class StringExtensions
    {
        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        static StringExtensions()
        {
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static T? Deserialize<T>(this string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public static string Serialize(this object obj)
        {
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }
    }
}