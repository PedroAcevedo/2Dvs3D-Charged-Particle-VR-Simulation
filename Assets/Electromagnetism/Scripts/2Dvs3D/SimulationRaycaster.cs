using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System;

public class SimulationRaycaster : OVRRaycaster, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public Transform hitPoint;
    public Canvas frameCanvas;
    public bool IsReleased;

    private OVR2DVRControlUI ovr2DVRContorlUI;

    [NonSerialized]
    private Canvas m_Canvas;

    [NonSerialized]
    private RectTransform rectTransform;

    [NonSerialized]
    private Vector2 rectAnchorPosition;

    [NonSerialized]
    private Vector2 prePosition;

    [NonSerialized]
    private Vector2 curPosition;

    protected override void Awake()
    {
        if (transform.parent != null) { ovr2DVRContorlUI = GameObject.Find("Interaction 2D Canvas").GetComponent<OVR2DVRControlUI>(); }
    }

    protected override void Start()
    {
        base.Start();
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // If the ray collides with a control point or interest point,
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }

            // Get the initial point position in RectTransform
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, eventCamera, out prePosition);

            Debug.Log("On Begin Drag: " + prePosition);
        }
    }

    public void OnDrag(PointerEventData e)
    {
        // If the ray collides with a control point or interest point,
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }

            // Get the current point position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, eventCamera, out curPosition);

            // Get delta of point poisition
            Vector2 deltaPosition = curPosition - prePosition;
            deltaPosition = deltaPosition / frameCanvas.scaleFactor;

            // Update previous point position
            prePosition = curPosition;

            // Exception for 3D interaction
            if (transform.name != "Interaction 2D Canvas") { rectTransform.position = hitPoint.position; }

        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        // If the ray collides with a control point or interest point
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }
        }
    }

    public new void OnPointerEnter(PointerEventData e)
    {
        // If the ray collides with a control point or interest point,
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }

            // Check the position of collision on the RectTransform (siimilar with 2D canvas)
            Debug.Log("On Pointer Enter: " + rectAnchorPosition);
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        // If the ray collides with a control point or interest point,
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            Debug.Log("Down on gameObject: " + transform.tag);

            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, eventCamera, out prePosition);

            // If the ray collide with a control point, update the interacted point
            if (transform.tag == "Control Point")
            {
                ovr2DVRContorlUI.SetInteractedPoint(this.gameObject);

                if (GameController.initPerformance == 0)
                {
                    GameController.initPerformance = Time.time;
                }

            }
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.IsVRPointer() && (transform.tag == "Control Point" || transform.tag == "Interest Point"))
        {
            // Gaze has entered this canvas. We'll make it the active one so that canvas-mouse pointer can be used.
            OVRInputModule inputModule = EventSystem.current.currentInputModule as OVRInputModule;
            if (inputModule != null)
            {
                inputModule.activeGraphicRaycaster = this;
            }

            // Print release
            IsReleased = true;
            if (transform.tag == "Control Point")
            {
                ovr2DVRContorlUI.RealeaseInteractedPoint();
                GameController.lastPointRelease = Time.time;
            }
        }
    }
}
