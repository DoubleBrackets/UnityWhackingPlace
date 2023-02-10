#region

using UnityEngine;

#endregion

public class HorizontalMover : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position += Vector3.up;
        }
    }
}