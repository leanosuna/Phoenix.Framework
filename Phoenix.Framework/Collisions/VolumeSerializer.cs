using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Phoenix.Framework.Collisions
{
    public static class VolumeSerializer
    {
        const string _path = "Volumes.json";

        public static void Save(List<SerializableVolume> volumes)
        {
            var array = new JsonArray();
            foreach (var v in volumes)
                array.Add(v.Serialize());

            var json = array.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }

        public static List<SerializableVolume> Load()
        {
            if (!File.Exists(_path))
                return new List<SerializableVolume>();

            JsonNode? parsed;
            try
            {
                parsed = JsonNode.Parse(File.ReadAllText(_path));
            }
            catch (JsonException)
            {
                return new List<SerializableVolume>();
            }

            if (parsed is not JsonArray array)
                return new List<SerializableVolume>();

            var list = new List<SerializableVolume>();

            foreach (var node in array)
            {
                try
                {
                    if (node is JsonObject obj)
                        list.Add(SerializableVolume.Deserialize(obj));
                }
                catch (Exception)
                {
                    // Skip malformed entries instead of aborting the whole load
                }
            }

            return list;
        }
    }
}
