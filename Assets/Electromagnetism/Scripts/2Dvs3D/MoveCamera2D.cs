using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera2D : MonoBehaviour
{
    public float scrollSpeed = 10;
    public bool activateCamera;

    private Camera zoomCamera;
    private bool rotating = false;

    // Start is called before the first frame update
    void Start()
    {
        zoomCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Update the player position by pressing key WSAD
        float moveZ = 0.0f;
        float moveX = 0.0f;
        if (Input.GetKey(KeyCode.W)) { moveZ += 1.0f; }
        if (Input.GetKey(KeyCode.S)) { moveZ -= 1.0f; }
        if (Input.GetKey(KeyCode.A)) { moveX -= 1.0f; }
        if (Input.GetKey(KeyCode.D)) { moveX += 1.0f; }
        transform.Translate(new Vector3(moveX, 0.0f, moveZ) * 0.1f);

        // When click the mouse right button, enable rotation
        if (Input.GetMouseButtonDown(1)) { rotating = true; }
        else if (Input.GetMouseButtonUp(1)) { rotating = false; }

        // Based on the mouse movement, the camera is rotated
        if (rotating)
        {
            transform.Rotate(0.0f, Input.GetAxis("Mouse X") * 1.0f, 0.0f, Space.World);
            transform.Rotate(-Input.GetAxis("Mouse Y") * 1.0f, 0.0f, 0.0f);
        }
    }
}
