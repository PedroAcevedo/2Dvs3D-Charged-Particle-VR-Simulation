using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class OVR2DVRControlUI : MonoBehaviour
{
    public GameObject interactedPoint;
    public Transform parentControlPoint;
    public bool transformChanged;

    private List<GameObject> listControlPoint;
    private RectTransform interactedRectTransform;
    private RectTransform rectTransform;
    private float rectWidth;
    private float rectHeight;
    private float midWidth;
    private float midHeight;
    private Color currentColor;
    private Color hoverColor = Color.grey;

    // Start is called before the first frame update
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rectWidth = rectTransform.sizeDelta.x;
        rectHeight = rectTransform.sizeDelta.y;

        midWidth = rectWidth * 0.5f;
        midHeight = rectHeight * 0.5f;

        listControlPoint = new List<GameObject>();

        for (int i = 0; i < parentControlPoint.childCount; i++) { listControlPoint.Add(parentControlPoint.GetChild(i).gameObject); }

        interactedPoint = listControlPoint[0];
    }
    
    private void Start()
    {
        currentColor = interactedPoint.GetComponent<Image>().color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RealeaseInteractedPoint()
    {
        if (interactedPoint != null) { 
            interactedPoint.GetComponent<Image>().color = currentColor;
            interactedPoint = null;
        }
    }

    // Update interacted point and the UI
    public void SetInteractedPoint(GameObject tmp)
    {
        interactedPoint = tmp;
        currentColor = interactedPoint.GetComponent<Image>().color;
        interactedPoint.GetComponent<Image>().color = hoverColor;

        interactedRectTransform = interactedPoint.GetComponent<RectTransform>();

    }

    // Update interacted point and the UI
    public void SetInteractedDesktopPoint(GameObject tmp)
    {
        interactedPoint = tmp;
        currentColor = interactedPoint.GetComponent<Image>().color;
        interactedPoint.GetComponent<Image>().color = hoverColor;

        interactedRectTransform = interactedPoint.transform.GetComponent<RectTransform>();

    }

    public List<GameObject> UpdateControlPointList()
    {
        return listControlPoint;
    }

    public RectTransform GetCurrentControlPoint()
    {
        return interactedRectTransform;
    }
}
