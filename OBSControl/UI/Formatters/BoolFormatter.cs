using System;

namespace OBSControl.UI.Formatters;

internal class BoolFormatter : ICustomFormatter
{
    public string Format(string format, object arg, IFormatProvider formatProvider) =>
        arg is not bool boolVal || !format.Contains('|') ? "<ERROR>"
        : boolVal ? format.Substring(0, format.IndexOf('|')) 
        : format.Substring(format.IndexOf('|') + 1);
}