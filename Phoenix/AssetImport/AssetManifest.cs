using System;
using System.Collections.Generic;
using System.Text;

namespace Phoenix.AssetImport
{
    public sealed class AssetManifest
    {
        public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();
        public List<AssetEntry> Assets { get; set; } = new();
        
    }
}
