using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.Collections.LowLevel.Unsafe;

public class DragDropDesktop2D : MonoBehaviour, IPointerEnterHandler
{
    public bool isInterestPoint;

    private RectTransform rectTransform;

    private Vector3 mousePositionRect;
    private OVR2DVRControlUI ovr2DVRContorlUI;

    private float grabTime;

    void Awake()
    {
        if (transform.parent != null) { ovr2DVRContorlUI = GameObject.Find("Interaction 2D Canvas").GetComponent<OVR2DVRControlUI>(); }
    }

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    //Detect if the Cursor starts to pass over the GameObject
    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        //Output to console the GameObject's name and the following message
        Debug.Log("Point Enter");


    }

    //Detect when Cursor leaves the GameObject

    private Vector3 GetMousePos() { return Camera.main.WorldToScreenPoint(transform.position); }

    public void OnMouseDown()
    {
        // Convert the screen point to world point in RectTransform
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, Input.mousePosition, Camera.main, out mousePositionRect);
        
        // Get local position of point
        mousePositionRect = mousePositionRect - transform.parent.parent.GetComponent<RectTransform>().anchoredPosition3D;
        // Add mouse click counter
        GameController.mouseClicks++;

        // If the ray collide with a control point, update the interacted point
        if (transform.tag == "Control Point")
        {
            ovr2DVRContorlUI.SetInteractedPoint(this.gameObject);

            if (GameController.initPerformance == 0)
            {
                GameController.initPerformance = Time.time;
            }
        }

        Debug.Log("Mouse Down:" + mousePositionRect);
    }

    public void OnMouseUp()
    {
        // If the ray collide with a control point, update the interacted point
        if (transform.tag == "Control Point")
        {
            ovr2DVRContorlUI.RealeaseInteractedPoint();
            GameController.lastPointRelease = Time.time;
        }
    }

    private void OnMouseDrag()
    {
        // Convert the screen point to world point in RectTransform, get local position of point, and update it
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, Input.mousePosition, Camera.main, out mousePositionRect);
        mousePositionRect = mousePositionRect - transform.parent.parent.GetComponent<RectTransform>().anchoredPosition3D;
        rectTransform.anchoredPosition = mousePositionRect;

        // If it is a control point, update UI (dropdown and sliders)
        if (isInterestPoint)
        {
            this.GetComponent<InterestPoint2D>().ShowValueDefault();
        }
    }

}