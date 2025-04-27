using System.IO;
using System.Reflection;

namespace OBSControl.Utilities;

internal static class ResourceLoading
{
    private static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

    public static byte[] GetResource(string resourcePath) =>
        GetResource(Assembly, resourcePath);

    private static byte[] GetResource(Assembly assembly, string resourcePath)
    {
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        using var memoryStream = new MemoryStream();
        stream?.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}