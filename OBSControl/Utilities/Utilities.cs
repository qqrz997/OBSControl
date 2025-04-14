using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OBSControl.Utilities;

public static class Utilities
{
    private static readonly string[] InvalidFileNameChars = Path.GetInvalidFileNameChars().Select(c => c.ToString()).ToArray();
    
    public static string GetSafeFilename(string fileName, string? substitute = null, string? spaceReplacement = null)
    {
        _ = fileName ?? throw new ArgumentNullException(nameof(fileName), "fileName cannot be null for GetSafeFilename");
        StringBuilder retStr = new StringBuilder(fileName);
        GetSafeFilename(ref retStr, substitute, spaceReplacement);
        return retStr.ToString();
    }

    public static void GetSafeFilename(ref StringBuilder filenameBuilder, string? substitute = null, string? spaceReplacement = null)
    {
        var invalidSubstitutes = substitute == null ? [] 
            : InvalidFileNameChars.Where(c => substitute.Contains(c)).ToArray();
        
        if (substitute == null || invalidSubstitutes.Length > 0)
        {
            substitute = string.Empty;
        }
        
        if (spaceReplacement != null && spaceReplacement != " ")
        {
            filenameBuilder.Replace(" ", spaceReplacement);
        }

        foreach (var character in InvalidFileNameChars)
        {
            filenameBuilder.Replace(character.ToString(), substitute);
        }
    }
}