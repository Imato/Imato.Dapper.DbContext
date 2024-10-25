using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imato
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

        /// <summary>
        /// Pull parameter from string like this "param1=value1;param2=value2"
        /// </summary>
        /// <param name="parametersString"></param>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string PullParameter(
            this string parametersString,
            string parameterName,
            out string? value)
        {
            var search = $"{parameterName}=";
            if (parametersString.Contains(search))
            {
                string? valueString = null;
                string cs = "";
                foreach (string s in parametersString.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (s.Contains(search))
                    {
                        valueString = s.Replace(search, "");
                    }
                    else
                    {
                        cs += s + ";";
                    }
                }
                value = valueString;
                return cs;
            }
            value = null;
            return parametersString;
        }
    }
}