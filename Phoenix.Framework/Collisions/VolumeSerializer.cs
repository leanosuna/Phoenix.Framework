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

            var json = File.ReadAllText(_path);
            var array = JsonNode.Parse(json)!.AsArray();
            var list = new List<SerializableVolume>();

            foreach (var node in array)
                list.Add(SerializableVolume.Deserialize(node!.AsObject()));

            return list;
        }
    }
}
