#region

using UnityEngine;

#endregion

public static class ExtensionMethods
{
    public static float AtLeast(this float val, float min)
    {
        return Mathf.Max(min, val);
    }

    public static int AtLeast(this int val, int min)
    {
        return Mathf.Max(min, val);
    }
}