using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderDrag : MonoBehaviour, IPointerUpHandler
{
    public MarchingSquares marchingControl;
    public Slider slider;
    public OVR2DVRControlUI ovrControlUI;


    public void OnPointerUp(PointerEventData eventData)
    {
        // marchingControl.FixCurrentParticle();
        Debug.Log("Sliding finished");
    }

    // Update function for slider
    public void MoveVerticalControlPoint() { ovrControlUI.interactedPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2(ovrControlUI.interactedPoint.GetComponent<RectTransform>().anchoredPosition.x, (slider.value - 0.5f) * 45.0f); }
    public void MoveHorizontalControlPoint() { ovrControlUI.interactedPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2((slider.value - 0.5f) * 45.0f, ovrControlUI.interactedPoint.GetComponent<RectTransform>().anchoredPosition.y); }
}
