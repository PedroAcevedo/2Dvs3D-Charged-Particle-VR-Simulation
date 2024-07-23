using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Interest point control for 2D scene
public class InterestPoint2D : MonoBehaviour
{
    public int TimeActive = 2;
    public GameObject nextPoint;
    public GameObject newtonCanvas;
    public TextMeshProUGUI newtonLabel;
    public MarchingSquares MarchingRef;
    public bool isVR;

    public float interactionTime = 0.0f;

    public void Reset()
    {
        //this.gameObject.GetComponent<Renderer>().material.color = originColor;
        newtonCanvas.SetActive(false);
        this.newtonLabel.text = "";
    }

    private void Update()
    {
        ShowValueDefault();
        transform.hasChanged = false;
    }

    public void ShowValueDefault()
    {
        newtonCanvas.SetActive(true);
        changeValue();
    }

    public void changeValue()
    {
        this.newtonLabel.text
            = MarchingRef.getPointValue(this.gameObject.transform.position) + " N";
    }

    public string GetCurrentValue()
    {
        return this.newtonLabel.text.Replace(" N", "");
    }

}
