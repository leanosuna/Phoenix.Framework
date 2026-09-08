using System.Reflection;

namespace Phoenix;

public static class EmbeddedHelper
{
    /// <summary>
    /// Extracts an embedded resource to a temporary file on disk and returns its absolute path.
    /// </summary>
    public static string ExtractPath(string fileName, string assemblyPath)
    {
        var resourceName = $"Phoenix.Framework.{assemblyPath}.{fileName}";

        string tempFile = Path.Combine(Path.GetTempPath(), fileName);
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
                throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");

            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }
        return tempFile;
    }

    /// <summary>
    /// Reads all bytes of an embedded assembly resource directly into a byte array.
    /// </summary>
    public static byte[] ReadBytes(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var targetName = resourceName.StartsWith("Phoenix.Framework.") ? resourceName : $"Phoenix.Framework.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(targetName);
        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new FileNotFoundException($"Embedded resource '{targetName}' not found. Available resources: [{available}]");
        }

        byte[] bytes = new byte[stream.Length];
        int bytesRead = 0;
        while (bytesRead < bytes.Length)
        {
            int read = stream.Read(bytes, bytesRead, bytes.Length - bytesRead);
            if (read == 0)
                break;
            bytesRead += read;
        }

        return bytes;
    }
}


