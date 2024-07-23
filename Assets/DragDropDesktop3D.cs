using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class DragDropDesktop3D : MonoBehaviour
{
    public bool dragging = false;
    public SimulationController simulationController;

    private Transform draggedObject;
    private Vector3 mousePosition;
    private float dist;
    private Vector3 offset;
    private bool rotating = false;
    private int layerMask;
    private Color currentColor;
    private Color hoverColor = Color.grey;

    // Start is called before the first frame update
    void Start()
    {
        layerMask = (-1) - (1 << LayerMask.NameToLayer("Default"));
    }

    // Upd ate is called once per frame
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

        Vector3 vector3;
        Vector3 mousePosition = Input.mousePosition;

        // When click the mouse left button, enable drag
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            dragging = true;
            GameController.mouseClicks++;

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * 1000.0f, Color.magenta);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // When the ray collide with a control point or interest point
                if (hit.collider.tag == "Control Point" || hit.collider.tag == "Interest Point")
                {
                    draggedObject = hit.transform;

                    currentColor = draggedObject.gameObject.GetComponent<MeshRenderer>().material.color;
                    draggedObject.gameObject.GetComponent<MeshRenderer>().material.color = hoverColor;

                    if (GameController.initPerformance == 0)
                    {
                        GameController.initPerformance = Time.time;
                    }

                    // Calculate offset to calibrate the position of point from ScreenToWorldPoint
                    dist = Vector3.Distance(hit.transform.position, Camera.main.transform.position);
                    vector3 = new Vector3(mousePosition.x, mousePosition.y, dist);
                    vector3 = Camera.main.ScreenToWorldPoint(vector3);
                    offset = draggedObject.position - vector3;
                }
            }

        }
        else if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            dragging = false;
            // When release the mouse left button, udpate the value of interest point
            if (draggedObject)
            {
                if (draggedObject.tag == "Control Point") { 
                
                    draggedObject.gameObject.GetComponent<MeshRenderer>().material.color = currentColor;
                    GameController.lastPointRelease = Time.time;
                    draggedObject = null;
                }
                else
                {
                    if (draggedObject.tag == "Interest Point") {
                        draggedObject.GetComponent<InterestPoint>().ShowValueDefault(); 
                    }

                }

            }
        }

        // When the point is dragged
        if (dragging && draggedObject)
        {
            // update the position based on the delta of position
            vector3 = new Vector3(Input.mousePosition.x, Input.mousePosition.y, dist);
            vector3 = Camera.main.ScreenToWorldPoint(vector3);
            draggedObject.position = vector3 + offset;
        }
    }

}
