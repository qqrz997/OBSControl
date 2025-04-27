using System;
using System.IO;
using UnityEngine;

namespace OBSControl.Utilities;

internal static class PluginResources
{
    private const string ResourcesPath = "OBSControl.Resources.";

    public static Sprite PinnedIcon { get; } = LoadSpriteResource("pinned.png");
    public static Sprite UnpinnedIcon { get; } = LoadSpriteResource("unpinned.png");
    
    private static Sprite LoadSpriteResource(string resourceName)
    {
        var imageData = ResourceLoading.GetResource(ResourcesPath + resourceName);
        return new Texture2D(2, 2).ToSprite(imageData, rename: Path.GetFileNameWithoutExtension(resourceName))
               ?? throw new InvalidOperationException("Failed to create a sprite from an internal image");
    }
}