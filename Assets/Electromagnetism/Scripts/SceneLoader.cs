using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour
{
    public static int participantId;

    public int participant;
    public int condition;

    private int assignedCondition;
    private int assignedRelationPair;

    // Start is called before the first frame update
    void Start()
    {
        LoadParticipantData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadParticipantData()
    {
        string destination = Application.dataPath + "/Resources/Data/participant-condition-latin-square.csv";

        string[] lines = System.IO.File.ReadAllLines(destination);

        string[] participantData = lines[participant].Split(",");

        assignedCondition = int.Parse(participantData[condition]);
        assignedRelationPair = int.Parse(participantData[4 + condition]);

        Debug.Log("Condition " + assignedCondition);
        Debug.Log("Pair " + assignedRelationPair);

        GameController.relationPair = assignedRelationPair - 1;
        SceneLoader.participantId = participant;

        LoadConditionScene();
    }

    public void LoadConditionScene()
    {
        switch (assignedCondition)
        {
            case 1:
                SceneManager.LoadScene("Desktop2DScene");    
                break;
            case 2:
                SceneManager.LoadScene("Desktop3DScene");
                break;
            case 3:
                SceneManager.LoadScene("VR2DScene");
                break;
            case 4:
                SceneManager.LoadScene("VR3DScene");
                break;
        }
    }

}
