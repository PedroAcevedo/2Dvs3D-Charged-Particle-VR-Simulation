using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Particle
{
    public Transform transform;
    public float charge;
    public Vector3 initialPosition;
    public bool is2D;

    private Renderer renderer;
    private Material material;
    private TMP_Text text;

    private Image image;
    private TextMeshProUGUI textUI;

    public Particle(Transform transform, float charge, Vector3 initialPosition, GameObject particle, bool is2D)
    {
        this.transform = transform;
        this.charge = charge * 1e-9f;
        this.initialPosition = initialPosition;
        this.is2D = is2D;
        this.image = is2D? particle.GetComponentInChildren<Image>() : null;
        this.textUI = is2D ? particle.GetComponentInChildren<TextMeshProUGUI>() : null;
        this.renderer = !is2D ? particle.GetComponent<Renderer>() : null;
        this.material = !is2D ? particle.GetComponent<Renderer>().material : null;
        this.text = !is2D ? particle.GetComponentInChildren<TextMeshPro>() : null;
    }

    public Particle(Transform transform, Vector3 initialPosition, float charge, bool is2D, Renderer renderer, TMP_Text text)
    {
        this.transform = transform;
        this.charge = charge;
        this.initialPosition = initialPosition;
        this.is2D = is2D;
        this.renderer = renderer;
        this.material = renderer.material;
        this.text = text;
        this.image = null;
        this.textUI = null;
    }

    public void SetParticleMaterial()
    {
        if (is2D)
        {
            if (charge > 0)
            {
                image.color = Color.red;
                textUI.text = "+";
            }
            else
            {
                image.color = Color.blue;
                textUI.text = "-";
            }
        }
        else
        {
            if (charge > 0)
            {
                renderer.material = SimulationController.particleMaterials[0];
                text.text = "+";
            }
            else
            {
                renderer.material = SimulationController.particleMaterials[1];
                text.text = "-";
            }
        }
    }

    public void ResetPosition()
    {
        this.transform.localPosition = initialPosition;
    }

    public Particle Clone()
    {
        Particle clone = new Particle(transform, initialPosition, charge, is2D, renderer, text);
        return clone;
    }

    public Rigidbody GetRigidbody()
    {
        return transform.gameObject.GetComponent<Rigidbody>();
    }

    public OVRGrabbable GetOVRGrababble()
    {
        return transform.gameObject.GetComponent<OVRGrabbable>();
    }

    public void SetCharge(float signal)
    {
        this.charge *= signal;
    }

    public void SetChargeValue(float signal)
    {
        this.charge = signal * 1e-9f;
    }

    public void Show(bool show)
    {
        this.transform.gameObject.SetActive(show);
    }
}
