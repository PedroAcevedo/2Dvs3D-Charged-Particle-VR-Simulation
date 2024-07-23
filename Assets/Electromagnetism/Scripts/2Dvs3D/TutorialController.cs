using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public List<GameObject> UISequence;
    public GameObject simulationBox;
    public GameObject simulationBox2D;
    public GameObject interestPoint;
    public GameObject playerRef;
    public bool isVR;

    private int currentStep = 0;
    private CharacterController characterController;
    private Transform trackingSpace;
    private Vector3 playerPosition;

    void Start()
    {
        playerPosition = playerRef.transform.position;

        if (isVR)
        {
            characterController = playerRef.GetComponent<CharacterController>();
            trackingSpace = playerRef.transform.GetChild(1).GetChild(0);
        }
    }

    public void ChangeSequence()
    {
        currentStep++;

        ShowSimulationBox(false);
        ShowSimulationBox2D(false);
        ShowInterestPoint(false);

        if (isVR)
        {
            ResetPlayerPosition();
        }
        else
        {
            playerRef.transform.position = playerPosition;
            playerRef.transform.rotation = Quaternion.identity;
        }

        for (int i = 0; i < UISequence.Count; i++)
        {
            UISequence[i].SetActive(i == currentStep);
        }
    }

    public void ShowSimulationBox(bool show)
    {
        simulationBox.SetActive(show);
    }

    public void ShowSimulationBox2D(bool show)
    {
        simulationBox2D.SetActive(show);
    }

    public void ShowInterestPoint(bool show)
    {
        interestPoint.SetActive(show);
    }

    void ResetPlayerPosition()
    {
        characterController.enabled = false;
        playerRef.transform.position = playerPosition;
        playerRef.transform.rotation = Quaternion.identity;
        trackingSpace.rotation = Quaternion.identity;
        characterController.enabled = true;
    }
}
