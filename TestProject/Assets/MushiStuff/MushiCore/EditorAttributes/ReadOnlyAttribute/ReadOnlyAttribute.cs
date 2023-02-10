using System;
using UnityEngine;

namespace MushiCore.EditorAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}