using UnityEngine;

public class DescriptionMonoBehavior : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField, TextArea] private string description;
#endif
}