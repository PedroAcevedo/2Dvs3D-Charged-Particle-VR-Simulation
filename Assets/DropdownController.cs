using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropdownController : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    private List<GameObject> listControlPoint;
    private OVR2DVRControlUI ovr2DVRControlUI;
    private List<string> listControlPointName;


    // Start is called before the first frame update
    void Start()
    {
        ovr2DVRControlUI = GameObject.Find("Interaction 2D Canvas").GetComponent<OVR2DVRControlUI>();
        listControlPointName = new List<string>();

        listControlPoint = ovr2DVRControlUI.UpdateControlPointList();

        dropdown.ClearOptions();
        foreach (GameObject t in listControlPoint)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData() { text = t.name });
            listControlPointName.Add(t.name);
        }

        listControlPoint[dropdown.value].GetComponent<DragDropDesktop2D>().OnMouseDown();
        listControlPoint[dropdown.value].GetComponent<Image>().color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        if (ovr2DVRControlUI.interactedPoint != listControlPoint[dropdown.value])
        {
            for (int i = 0; i < listControlPoint.Count; i++)
            {
                if (ovr2DVRControlUI.interactedPoint == listControlPoint[i]) { dropdown.value = i; break; }
            }
        }
    }

    // Update the interacted control point through dropdown
    public void SelectControlPointFromDropDown()
    {
        ovr2DVRControlUI.SetInteractedPoint(listControlPoint[dropdown.value]);
    }

    // Update the interacted control point through dropdown in desktop version
    public void SelectControlPointDesktop()
    {
        ovr2DVRControlUI.SetInteractedDesktopPoint(listControlPoint[dropdown.value]);
    }
}
