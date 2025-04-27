using UnityEngine;

namespace OBSControl.Utilities;

internal static class TextureExtensions
{
    public static Sprite? ToSprite(
        this Texture2D? tex,
        byte[]? data = null,
        string? rename = null,
        float pixelsPerUnit = 100)
    {
        if (tex == null || (data != null && !tex.LoadImage(data))) return null;
        var sprite = Sprite.Create(tex, new(0, 0, tex.width, tex.height), new(0.5f, 0.5f), pixelsPerUnit);
        if (!string.IsNullOrWhiteSpace(rename)) sprite.name = rename;
        return sprite;
    }
}