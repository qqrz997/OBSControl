using System;

namespace OBSControl.UI.Formatters;

internal class TimeFormatter : ICustomFormatter
{
    public string Format(string format, object arg, IFormatProvider formatProvider)
    {
        if (arg is not int intValue)
        {
            return "<ERROR>";
        }

        var timeSpan = TimeSpan.FromSeconds(intValue);
        
        return string.IsNullOrEmpty(format) ? timeSpan.ToString() : timeSpan.ToString(format);
    }
}