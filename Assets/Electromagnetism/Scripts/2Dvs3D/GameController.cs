using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameController : MonoBehaviour
{
    public static float initPerformance;
    public static float lastPointRelease;
    public static int currentRelation = 0;
    public static int relationPair;
    public static int mouseClicks = 0;

    public enum SceneCondition { desktop2D, desktop3D, headset2D, headset3D };

    public List<GameObject> UIPanel;
    public List<int> sequence;
    public List<InterestPoint2D> interestPoints2D;
    public List<InterestPoint> interestPoints3D;
    public GameObject simulationBox;    
    public GameObject playerRef;
    public MarchingSquares marchingSquare;
    public SimulationController simulationController;
    public TMPro.TextMeshProUGUI relationLabel;
    public SceneCondition scene;
    public int participant;
    public bool is3D;
    public bool isVR;

    private int currentStep = 0;
    private Vector3 playerPosition;
    private CharacterController characterController;
    private Transform trackingSpace;
    private SceneRelation sceneRelation;
    private List<string> relations;
    private List<float> completionTime;
    private RelationPair pairs;
    private bool endTracking = false;

    // STATS
    private Vector3 currentPlayerPosition;
    private SceneData currentScene;
    private DataCollectionController dataController;
    private float distance = 0f;
    private int clicks;


    // Start is called before the first frame update
    void Start()
    {
        this.playerPosition = playerRef.transform.position;
        this.currentPlayerPosition = playerPosition;
        completionTime = new List<float>();

        if (isVR)
        {
            this.currentScene = new SceneData();
            this.dataController = new DataCollectionController();

            characterController = playerRef.GetComponent<CharacterController>();
            trackingSpace = playerRef.transform.GetChild(1).GetChild(0);
        }

        InvokeRepeating("ReportUser", 2.0f, 2.0f);

        var jsonValue = Resources.Load<TextAsset>("Data/relation-pairs");

        pairs = JsonUtility.FromJson<RelationPair>(jsonValue.text);
        relations = pairs.GetRelations(relationPair);

        if (is3D)
        {
            simulationController.particleSetting = pairs.GetParticleSettings(relationPair);
        }
        else
        {
            marchingSquare.particleSetting = pairs.GetParticleSettings(relationPair);
        }

        relationLabel.text = relations[currentRelation];
    }

    // Update is called once per frame
    void Update()
    {
        if (!endTracking)
        {
            if (isVR && simulationBox.activeSelf)
            {
                dataController.ButtonsPressed(ref currentScene);
            }
        }
    }

    public void ShowUIStep()
    {
        for (int i = 0; i < UIPanel.Count; i++)
        {
            UIPanel[i].SetActive(sequence[currentStep] == i);
        }
    }

    public void ChangeStep()
    {
        currentStep++;

        if (isVR)
        {
            ResetPlayerPosition();
        }
        else
        {
            playerRef.transform.position = playerPosition;
            playerRef.transform.rotation = Quaternion.identity;
        }

        ShowUIStep();
    }

    public void ToggleSimulationBox(bool show)
    {
        simulationBox.SetActive(currentStep < (sequence.Count - 1)? show : false);
    }

    public void SaveCompletionTime()
    {
        float timeElapsed = lastPointRelease - initPerformance;
        Debug.Log(timeElapsed);
        completionTime.Add(timeElapsed);

        lastPointRelease = 0;
        initPerformance = 0;

        ReportUserCompletionTime();

        if (sequence[currentStep] == 1)
        {
            currentRelation++;
            relationLabel.text = relations[currentRelation];
        }
    }

    void ResetPlayerPosition()
    {
        characterController.enabled = false;
        playerRef.transform.position = playerPosition;
        playerRef.transform.rotation = Quaternion.identity;
        trackingSpace.rotation = Quaternion.identity;
        characterController.enabled = true;
    }

    public string GetInterestPointsValue()
    {
        string points = "";

        if (is3D)
        {
            foreach(InterestPoint point in interestPoints3D)
            {
                points += point.GetCurrentValue() + ",";
            }
        }
        else
        {
            foreach (InterestPoint2D point in interestPoints2D)
            {
                points += point.GetCurrentValue() + ",";
            }
        }

        return points.Remove(points.Length - 1, 1);  
    }

    public void ReportUserCompletionTime()
    {
        string destination = Application.dataPath + "/Resources/study_2dvs3d_participant_data.csv";

        StreamWriter writer = new StreamWriter(destination, true);

        int clicks = 0;

        if (isVR)
        {
            clicks = currentScene.GetButtonPressed();
        }
        else
        {
            clicks = mouseClicks;
        }

        writer.WriteLine(SceneLoader.participantId + "," + scene + "," + GetInterestPointsValue() + "," + relations[currentRelation] + "," + distance + "," + clicks + "," + completionTime[currentRelation]);

        mouseClicks = 0;
        distance = 0;
        currentPlayerPosition = this.playerPosition;

        if (isVR)
        {
            currentScene.rightHandButtonPress = 0;
            currentScene.leftHandButtonPress = 0;
        }

        writer.Close();
    }

    public void ReportUser()
    {
        distance += Utils.VectorDistance(currentPlayerPosition, playerRef.transform.position);
        currentPlayerPosition = playerRef.transform.position;
    }
}
