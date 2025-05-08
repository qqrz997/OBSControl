using System;
using UnityEngine;

namespace OBSControl.Utilities;

internal static class MathfExtensions
{
    public static bool Approximately(this float a, float b) => Mathf.Approximately(a, b);
    
    public static decimal Round(this decimal a, int decimalPlaces) => Math.Round(a, decimalPlaces);
}