using System.Linq;
using UnityEngine;

namespace OBSControl;

internal static class SharedCoroutineStarter
{
    private static ICoroutineStarter? coroutineStarter;

    public static ICoroutineStarter Instance => 
        coroutineStarter ??= Resources.FindObjectsOfTypeAll<CoroutineStarter>().First();
}