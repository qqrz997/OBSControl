using System;
using System.IO;

namespace OBSControl.Utilities;

internal static class PathExt
{
    public static string UniqueCombine(string path1, string path2) => 
        GetUniqueFilePath(Path.Combine(path1, path2));

    private static string GetUniqueFilePath(string fullPath)
    {
        string ret = fullPath;
        string directoryName = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException("Invalid path");
        string fileName = Path.GetFileNameWithoutExtension(fullPath);
        string extension = Path.GetExtension(fullPath);
        int count = 2;
        while (File.Exists(ret))
        {
            ret = $"{Path.Combine(directoryName, fileName)} ({count++}){extension}";
        }
        return ret;
    } 
}