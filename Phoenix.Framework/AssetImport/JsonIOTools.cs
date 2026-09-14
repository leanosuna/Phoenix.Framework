using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Phoenix.Framework.AssetImport
{
    public static class JsonIOTools
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static bool Load<T>(string path, out T classFile)
        {
            if (!File.Exists(path))
            {
                classFile = default!;
                return false;
            }
            classFile = JsonSerializer.Deserialize<T>(
                File.ReadAllText(path),
                _jsonOptions
            )!;
            return true;
        }

        public static void Save<T>(string path, T classFile)
        {
            File.WriteAllText(
                path,
                JsonSerializer.Serialize(classFile, _jsonOptions)
            );
        }
    }

}
