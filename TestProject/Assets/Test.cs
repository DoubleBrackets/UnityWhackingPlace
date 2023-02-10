using MushiCore.EditorAttributes;
using UnityEngine;

public class Test : MonoBehaviour
{
    [ColorHeader("Test")]
    public int field;
    
    [ColorHeader("Test", showDivider:true)]
    public int field2;
}
