using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Phoenix.Framework.Collisions
{
    public static class VolumeSerializer
    {
        const string _path = "Volumes.json";

        public static void Save(List<BoundingVolume> volumes)
        {
            var array = new JsonArray();
            foreach (var v in volumes)
                array.Add(v.Serialize());

            var json = array.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }

        public static List<BoundingVolume> Load()
        {
            if (!File.Exists(_path))
                return new List<BoundingVolume>();

            var json = File.ReadAllText(_path);
            var array = JsonNode.Parse(json)!.AsArray();
            var list = new List<BoundingVolume>();

            foreach (var node in array)
                list.Add(BoundingVolume.Deserialize(node!.AsObject()));

            return list;
        }
    }
}
