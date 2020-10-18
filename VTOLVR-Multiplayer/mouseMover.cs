using UnityEngine;


public class mouseMover : MonoBehaviour
{

    private Vector3 screenPoint;
    private Vector3 offset;

    private Vector3 lastmouse;
    void Start()
    {
        lastmouse = new Vector3(Input.mousePosition.x, 0.0f, Input.mousePosition.y);
    }
    void OnMouseDown()
    {



    }

    void LateUpdate()
    {
        offset = lastmouse - new Vector3(Input.mousePosition.x, 0.0f, Input.mousePosition.y);
        lastmouse = new Vector3(Input.mousePosition.x, 0.0f, Input.mousePosition.y);
        transform.position += (offset * 0.3f);
    }

}