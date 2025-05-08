using System;

namespace OBSControl.Utilities;

internal static class BytesFormatting
{
    public static string FormatBytes(this long bytes) => bytes switch
    {
        < 0 => $"-{(-bytes).FormatBytes()}",
        0 => "0 bytes",
        _ => bytes.FormatBytesUnsafe()
    };
    
    private static string[] SizeSuffixes { get; } = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

    private static string FormatBytesUnsafe(this long bytes)
    {
        var magnitude = (int)Math.Log(bytes, 1024);
        var adjustedSize = (decimal)bytes / (1L << (magnitude * 10));

        return adjustedSize.Round(2) < 1000 ? $"{adjustedSize:N2} {SizeSuffixes[magnitude]}"
            : $"{adjustedSize / 1024:N2} {SizeSuffixes[magnitude + 1]}";
    }
}