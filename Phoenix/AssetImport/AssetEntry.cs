using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Phoenix.AssetImport
{
    public sealed class AssetEntry
    {
        public string Path { get; set; } = "";
        public string RelativePath { get; set; } = "";

        public AssetType Type { get; set; }
    }
}
