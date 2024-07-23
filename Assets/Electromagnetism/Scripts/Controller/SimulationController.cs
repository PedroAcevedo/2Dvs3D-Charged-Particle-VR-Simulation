using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OVRTouchSample;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.EventSystems;
using Unity.Jobs;
using Unity.Collections;

public class SimulationController : MonoBehaviour
{

    #region Public Fields

    //Public variables
    public MeshFilter meshFilter;       // Mesh for marching cubes visualization
    public int dimension;               // Resolution of the surface
    public int n;                       // Number of particles
    public int GridSize;
    public List<Particle> particleList;
    public GameObject[] cubeQuadrants;
    public GameObject RHand;            // VR right controller
    public GameObject LHand;            // VR left controller
    public GameObject MenuCanvas;
    public GameObject SceneControl;
    public GameObject[] interestPoints;
    public GameObject Indicators;
    public GameObject LobbyMenu;
    public GameObject LobbyQuestion;
    public GameObject InitialInstruction;
    public GameObject recordingIndicator;
    public GameObject boxPrefab;
    public GameObject EFParent;
    public GameObject testPoint;
    public GameObject particle;
    public Transform simulationCenter;
    public TMPro.TextMeshProUGUI playerIDLabel;
    public TMPro.TextMeshProUGUI partLabel;
    public TMPro.TextMeshProUGUI phaseLabel;
    public TMPro.TextMeshProUGUI lobbyLabel;
    public TMPro.TextMeshProUGUI phaseInstruction;
    public TMPro.TextMeshProUGUI phaseInstruction2;
    public TMPro.TextMeshProUGUI vibrationDEBUG;
    public Material negativeParticle;
    public Material positiveParticle;
    public int HMDNumber;           // Change between HeadSets
    public float Gamma;
    public List<Vector3> particleSetting;

    public static GameObject playerMicrophone;
    public static int currentScene = 0;
    public static string playerID;
    public static Material[] particleMaterials;
    //For debugging 
    public bool DEBUG_GRID = false;
    public bool DEBUG_CUBE_VALUES = false;
    public GameObject referenceText;
    public GameObject reference;

    // Menu options
    public bool showLines;
    public bool Mode2D;
    public bool hapticFeedback;
    public bool showSurface;
    public bool simpleMode;
    public bool showMenu;
    public bool particleInteraction;
    public bool showSimulation;
    public bool showDefault;
    public bool noActivityReport;
    public bool isDesktop;
    public bool isRecording;

    // Phase control
    public List<string> instructions;

    #endregion

    #region MarchingCubes Fields

    //Boundary values for Marching Cubes
    int MINX;
    int MAXX;
    int MINY;
    int MAXY;
    int MINZ;
    int MAXZ;
    public static float K = 8.98685134e9f;      //Coulomb's law constant
    int nX, nY, nZ;                    //number of cells on each axis for Marching cubes

    Vector4[] points;                  // Vertex on the grid
    float[] pointsCharges;             // Log of Electric field applied of each point of the grid 
    float[] currentPointsCharge;             // Electric field applied of each point of the grid 
    float[] pointsAngles;             // Electric field applied of each point of the grid 
    float[] vibrationMapping;
    float[] vibrationIntervals;

    private float maxCharge = -1.0f;
    private float minCharge = 10000.0f;

    int numTriangles = 0;         //Obtained by Marching Cubes

    // Variables being changed on runtime by user.
    float minValueForSingle = 0.5f;
    Vector3[] lookUpTable = {
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(1.0f, 1.0f, -1.0f),
        new Vector3(1.0f, -1.0f, 1.0f),
        new Vector3(1.0f, -1.0f, -1.0f),
        new Vector3(-1.0f, 1.0f, 1.0f),
        new Vector3(-1.0f, 1.0f, -1.0f),
        new Vector3(-1.0f, -1.0f, 1.0f),
        new Vector3(-1.0f, -1.0f, -1.0f),
    };

    //factor of influence
    Vector3[] elements = {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
    };
    float[] c = { 2.3f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

    // Lists for Mesh cosntruction
    HashSet<Vector3> vertices = new HashSet<Vector3>();
    Int32[] triangles;
    List<Vector3> QuadrantsLimits = new List<Vector3>();
    List<Bounds> QuadrantsBounds = new List<Bounds>();
    List<String> QuadrantsElements = new List<String>();

    private TRIANGLE[] Triangles;
    private Vector3 stepSize;
    private bool isHandsGrabbing;

    // Variable for exporting result through .txt file
    private DirectoryInfo dir_info;
    private FileStream fs;
    private StreamWriter sw;
    public GameObject rightHand;            // VR right controller
    public GameObject leftHand;            // VR left controller

    #endregion

    #region Private Fields

    // Lines class
    private ParticleLines lineController;

    // For Scene control
    private int[] numberOfParticles = { 2, 2, 2, 3 };
    private int[] negativeCharges = { 0, 2, 1, 2 };
    private Dictionary<String, Vector3> initialPositions = new Dictionary<String, Vector3>();
    private Particle[] particlesOnScene;
    private float[] chargesOnScene;
    private float[] initialTimePerParticle;
    private Transform MainCamera;
    private GameObject[] particleSignText;
    private GameObject arrowInField;
    private int simulationMode = -1;
    private int currentPhase = 0;
    private float currentPhaseTime = 0;
    private CharacterController characterController;
    private Transform trackingSpace;

    //Update actual view
    private bool updateSurface = false;

    //User stats
    public static UserReportController controller;
    private GameObject player;
    private SceneData mainScene;
    private DataCollectionController dataController;
    private bool reportData = false;

    private string[] PhaseNames = { "Exploration Phase", "Experimental Phase", "Interactive Phase" };
    private MicrophoneController microphone;
    private Vector2[] simulationLimits;
    private Color currentColor;
    private Color hoverColor = Color.grey;

    private NativeArray<Vector4> points2;
    private NativeArray<float> currentPointsCharge2;
    private NativeArray<float> pointsCharges2;

    private MarchingCubes marchingCubes;

    #endregion

    #region MonoBehaviour Callbacks

    // Start is called before the first frame update
    void Start()
    {
        // TO DO a graph of the vibration mapping
        vibrationMapping = Utils.linspace(0, 1, 12); // TODO ANALYZE VIBRATION

        // TODO REVISE WHY THIS WORKS

        for (int i = 0; i < lookUpTable.Length; ++i)
        {
            QuadrantsLimits.Add(new Vector3(MAXX * lookUpTable[i].x, MAXY * lookUpTable[i].y, MAXZ * lookUpTable[i].z));
            Bounds bound = new Bounds();
            bound.Encapsulate(cubeQuadrants[i].GetComponent<BoxCollider>().bounds);
            QuadrantsBounds.Add(bound);
            QuadrantsElements.Add("");
        }

        initialTimePerParticle = new float[particleList.Count];

        for (int i = 0; i < particleList.Count; ++i)
        {
            initialTimePerParticle[i] = 0.0f;
            initialPositions[particleList[i].transform.gameObject.name] = particleList[i].transform.position;
        }

        //User stats
        if(playerID != null)
            getUserID();

        if (isRecording)
        {
            playerMicrophone = recordingIndicator;
            microphone = new MicrophoneController(GetComponent<AudioSource>());
        }
        dataController = new DataCollectionController();

        // Assign materials
        particleMaterials = new Material[2];

        particleMaterials[0] = positiveParticle;
        particleMaterials[1] = negativeParticle;

        // Select the scene
        arrowInField = Resources.Load("Prefabs/arrow_in_field") as GameObject;
        //MainCamera = GameObject.Find("OVRCameraRig").transform;
        player = GameObject.Find(isDesktop? "Player" : "OVRCustomPlayer");
        
        characterController = player.GetComponent<CharacterController>();
        
        if(!isDesktop)
            trackingSpace = player.transform.GetChild(1).GetChild(0);

        for (int i = 0; i < particleList.Count; i++)
        {
            particleList[i] = new Particle(particleList[i].transform, particleList[i].charge, particleList[i].initialPosition, particleList[i].transform.gameObject, false);
            if(particleSetting.Count > 0)
                particleList[i].SetChargeValue(GetVector3Value(particleSetting[GameController.currentRelation], i));
            particleList[i].SetParticleMaterial();
            particleList[i].transform.LookAt(player.transform);
            particleList[i].ResetPosition();
        }

        particlesOnScene = GetParticlesOnScene();

        if (isRecording)
            SetupCurrentScene();

        marchingCubes = new MarchingCubes(ref particleList, simulationCenter.position, GridSize, dimension, meshFilter);
        marchingCubes.Init();

        simulationLimits = new Vector2[4];

        lineController = new ParticleLines(ref particleList);

        Vector2 simulationCenter2D = new Vector2(simulationCenter.position.x, simulationCenter.position.y);

        simulationLimits[0] = new Vector2(-GridSize, -GridSize) + simulationCenter2D;
        simulationLimits[1] = new Vector2(-GridSize, GridSize) + simulationCenter2D;
        simulationLimits[2] = new Vector2(GridSize, GridSize) + simulationCenter2D;
        simulationLimits[3] = new Vector2(GridSize, -GridSize) + simulationCenter2D;

        lineController.simulationLimits = simulationLimits;
        lineController.linesLimit = new Vector3(10 + simulationCenter2D.x, 10 + simulationCenter2D.y, 10 + simulationCenter.position.z);
        lineController.zOffset = simulationCenter.position.z;
        lineController.parent = EFParent;

        if (showLines)
        {
            lineController.Draw(this.Mode2D);
        }



        marchingCubes.RunMarchingCubes();
    }

    void FixedUpdate()
    {
        if (updateSurface == true)
        {
            marchingCubes.RunMarchingCubes();
            updateSurface = false;
        }

        for (int i = 0; i < particleList.Count; ++i)
        {
            if (particleList[i].transform.hasChanged)
            {
                particleList[i].transform.hasChanged = false;

                if (showLines)
                {
                    lineController.Draw(this.Mode2D);
                }
                if (showSurface)
                {
                    updateIsosurface();
                    trackParticle();

                    if(!isDesktop)
                        if (particleList[i].transform.gameObject.GetComponent<OVRGrabbable>().isGrabbed)
                        {
                            if(initialTimePerParticle[i] == 0)
                            {
                                currentColor = particleList[i].transform.gameObject.GetComponent<MeshRenderer>().material.color;
                                particleList[i].transform.gameObject.GetComponent<MeshRenderer>().material.color = hoverColor;

                                if (GameController.initPerformance == 0)
                                {
                                    GameController.initPerformance = Time.time;
                                }
                            }

                            initialTimePerParticle[i] = Time.time;
                        }
                }
                break;
            }
        }

        if (!isDesktop)
        {
            bool isStatic = true;

            for (int i = 0; i < particleList.Count; ++i)
            {
                validateParticleGrab(i);
                isStatic = isStatic && !particleList[i].transform.gameObject.GetComponent<OVRGrabbable>().isGrabbed;
            }

            isHandsGrabbing = !isStatic;

            if (isHandsGrabbing) resetHaptic();
        }

    }

    void Update()
    {
        //findHandsVibration();
        if (hapticFeedback && !isHandsGrabbing)
        {
            findHandsVibrationOptimized();
        }

        if (rightHand == null)
        {
            rightHand = GameObject.Find("Reference");
        }

        // User stats
        if (noActivityReport)
            UpdateStats();
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            foreach (Bounds bound in QuadrantsBounds)
            {
                Gizmos.color = new Color(0, 1, 0);
                Gizmos.DrawWireCube(bound.center, bound.size);
            }
        }
    }

    #endregion

    #region Public Methods

    public void showLine(bool value)
    {
        this.showLines = value;
        if (value)
        {
            lineController.Draw(this.Mode2D);
        }
        else
        {
            lineController.CleanLines();
        }
    }

    public void Mode2DState(bool value)
    {
        this.Mode2D = value;
        if (this.showLines)
            lineController.Draw(this.Mode2D);
    }

    public void hapticState(bool value)
    {
        this.hapticFeedback = value;
    }

    public void hapticSimple(bool value)
    {
        this.simpleMode = value;
    }

    public void showSurfaceState(bool value)
    {
        this.showSurface = value;
        if (value)
        {
            marchingCubes.RunMarchingCubes();
        }
        else
        {
            meshFilter.mesh = null;
        }
    }

    public void showForceDirection(bool value)
    {
        this.lineController.showForces = value;
        if (this.showLines)
            lineController.Draw(this.Mode2D);
    }

    public void removeParticleInteraction(bool value)
    {
        for (int i = 0; i < particlesOnScene.Length; ++i)
        {
            particlesOnScene[i].GetRigidbody().freezeRotation = value;

            if (value)
            {
                particlesOnScene[i].GetRigidbody().constraints = RigidbodyConstraints.FreezePosition;
            }
            else
            {
                particlesOnScene[i].GetRigidbody().constraints = RigidbodyConstraints.None;
                particlesOnScene[i].GetRigidbody().constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
            }
        }
    }

    public void ResetParticlesPositons()
    {
        for (int i = 0; i < particleList.Count; i++)
        {
            particleList[i].SetChargeValue(GetVector3Value(particleSetting[GameController.currentRelation], i));
            particleList[i].SetParticleMaterial();
            particleList[i].transform.LookAt(player.transform);
            particleList[i].ResetPosition();
        }
    }

    public void updateIsosurface()
    {
        IsosurfaceCalculate();
        updateSurface = true;
        showSurface = true;
    }
    
    public string getPointValue2(Vector3 point)
    {
        int quadIndexR = findQuadrant(point);

        float currentValue = 0;

        String[] indexQuad = QuadrantsElements[quadIndexR].Split('-');

        float lessDistance = 1000f;

        for (int i = 0; i < indexQuad.Length - 1; ++i)
        {
            Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z);

            if (Vector3.Distance(pointPos, point) < lessDistance)
            {
                currentValue = currentPointsCharge[Int32.Parse(indexQuad[i])];
                lessDistance = Vector3.Distance(pointPos, point);
            }
        }

        return (int)currentValue + "";
    }

    public string getPointValue(Vector3 point)
    {
        return (int) marchingCubes.ElectromagnetismCharge(point) + "";
    }

    #endregion

    #region Scene Control


    public Particle[] GetParticlesOnScene()
    {
        Particle[] particleTransforms = new Particle[particleList.Count];

        for(int i = 0; i < particleTransforms.Length; i++)
        {
            particleTransforms[i] = particleList[i].Clone();
        }

        return particleTransforms;
    }

    //Setup the Scene
    void SetupCurrentScene()
    {
        List<Particle> tempParticles = new List<Particle>();

        int particleNumber = numberOfParticles[currentScene];
        int negatives = negativeCharges[currentScene];

        for (int i = 0; i < particleNumber; ++i)
        {
            tempParticles.Add(particlesOnScene[i]);

            int signal = 1;

            if (negatives > 0 && i >= (tempParticles.Count - negatives))
            {
                signal = -1;
                negatives--;
            }

            tempParticles[i].SetChargeValue(signal);
            tempParticles[i].SetParticleMaterial();
            tempParticles[i].transform.LookAt(new Vector3(0.0f, 5.30000019f, -15.0f));
        }

        particleList.Clear();

        particleList = tempParticles;

        n = particleList.Count;

        if(partLabel != null)
        {
            partLabel.text = "Part " + (currentScene + 1);
            setPhaseLabel();
        }
    }

    //Hand raycasting
    void verifyHand()
    {
        Debug.Log("Left Hander" + PlayerPrefs.GetInt("LeftHander"));

        if (PlayerPrefs.GetInt("LeftHander") == 1)
        {
            FindObjectOfType<OVRInputModule>().rayTransform = LHand.transform;
        }
        else
        {
            FindObjectOfType<OVRInputModule>().rayTransform = RHand.transform;
        }
    }

    //Reset interest point
    void resetInterestPoint()
    {
        if (interestPoints.Length > currentScene && currentScene >= 0)
        {
            interestPoints[currentScene].SetActive(false);

            GameObject points = interestPoints[currentScene].transform.GetChild(0).gameObject;

            points.SetActive(true);
            points.GetComponent<InterestPoint>().Reset();

            for (int i = 1; i < interestPoints[currentScene].transform.childCount; i++)
            {
                points = interestPoints[currentScene].transform.GetChild(i).gameObject;
                points.SetActive(false);
                points.GetComponent<InterestPoint>().Reset();
            }
        }
    }

    public void nextScene()
    {
        if(currentScene == 0)
        {
            microphone.stopRecording(playerID + "_scene1_voice");
        }

        ChangeScene(1);
    }

    public void backScene()
    {
        ChangeScene(-1);
    }

    public void returnToHome()
    {
        currentScene = 0;

        for (int i = 0; i < particleList.Count; ++i)
        {
            particleList[i].Show(false);
        }

        ChangeScene(0);

        simulationMode = -1;
        resetPlayerPosition();

        showLines = false;
        Mode2D = true;
        hapticFeedback = false;
        simpleMode = true;
        showMenu = false;
        particleInteraction = false;
        //showSurfaceState(false);
        lineController.CleanLines();

        MenuCanvas.SetActive(true);
        SceneControl.SetActive(false);
    }

    public void ChangeScene(int direction)
    {
        resetInterestPoint();

        currentScene += direction;

        if (currentScene >= 0 && currentScene < numberOfParticles.Length)
        {
            for (int i = 0; i < particleList.Count; ++i)
            {
                particleList[i].Show(false);
            }

            resetPlayerPosition();

            maxCharge = -1.0f;
            minCharge = 10000.0f;

            //showSurfaceState(false);

            SetupCurrentScene();

            for (int i = 0; i < particleList.Count; ++i)
            {
                particleList[i].Show(true);
                particleList[i].transform.position = initialPositions[particleList[i].transform.gameObject.name];
            }

            lineController.CleanLines();
            lineController = new ParticleLines(ref particleList) ;
            if (showLines)
            {
                lineController.Draw(this.Mode2D);
            }

            updateIsosurface();

        } else
        {
            if (currentScene == numberOfParticles.Length)
            {
                if(simulationMode == 3)
                {
                    simulationMode = 2;
                    currentScene = -1;
                    showLine(true);
                    ChangeScene(1);

                } else
                {
                    moveToLobby();
                    lobbyLabel.text = "Thanks for your participation!";
                    GameObject.Find("GoToQuestion").SetActive(false);
                }
            }
        }

    }

    public bool getCurrentMode()
    {
        return simulationMode == 0 || simulationMode == 2 || (simulationMode == 3 && showLines);
    }

    public void selectMode(int modeSelected)
    {
        simulationMode = modeSelected;
        microphone.startRecording();

        switch (simulationMode)
        {
            case 0: // Condition 1: No force label
                showLine(true);
                hapticFeedback = false;
                simpleMode = false;
                break;
            case 1: // Condition 1: force label
                showLines = false;
                hapticFeedback = true;
                simpleMode = true;
                break;
            case 2: // Condition 1: force label
                showLine(true);
                hapticFeedback = true;
                simpleMode = true;
                break;
            case 3: // Condition 1: No force label only firts step
                showLines = false;
                hapticFeedback = true;
                simpleMode = true;
                break;
        }

        particleInteraction = true;
        MenuCanvas.SetActive(false);
        InitIntructions();

        for (int i = 0; i < particleList.Count; ++i)
        {
            particleList[i].Show(true);
        }

        showSurfaceState(true);

        //Start Scene 1 stats
        startScene();
        initReport();
        UIClick();
        initPhaseTime();
    }

    public void selectCond1()
    {
        selectMode(0);
    }

    public void selectCond2()
    {
        selectMode(1);
    }

    public void selectCond3()
    {
        selectMode(2);
    }

    public void selectCond4()
    {
        selectMode(3);
    }

    public void goToQuestion() 
    {
        LobbyMenu.SetActive(false);
        LobbyQuestion.SetActive(true);
    }

    public void goBackToMenu()
    {
        LobbyQuestion.SetActive(false);
        LobbyMenu.SetActive(true);
    }

    public void returnToMain()
    {
        if(currentScene < numberOfParticles.Length)
        {
            currentPhase = 0;
            resetPlayerPosition();
            LobbyQuestion.SetActive(false);
            LobbyMenu.SetActive(true);
            startScene();
        }
        else
        {
            saveJson();
        }
    }

    public void changePhase()
    {

        currentPhase++;
        cleanPointsLabels();
        UIClick();
        setPhaseTime();
        initPhaseTime();
        resetPlayerPosition();

        switch (currentPhase)
        {
            case 1:
                if (interestPoints.Length > currentScene)
                {
                    interestPoints[currentScene].SetActive(true);
                    interestPoints[currentScene].transform.GetChild(0).gameObject.SetActive(true);
                }

                removeParticleInteraction(true);
                resetParticlePosition();
                setPhaseLabel();
                break;
            case 2:
                Indicators.SetActive(true);
                removeParticleInteraction(false);
                resetParticlePosition();
                setPhaseLabel();
                // Show indicators to move particles
                break;
            case 3:
                Indicators.SetActive(false);
                Indicators.GetComponent<IndicatorController>().resetIndicators();
                currentPhase = 0;

                saveSceneData();
                nextScene();
                moveToLobby();
                break;
        }

        InitIntructions();
    }

    public void hideInstruction()
    {
        InitialInstruction.SetActive(false);
        SceneControl.SetActive(true);

    }

    #endregion

    #region Haptic Methods

    void clasifyPoint(Vector3 point, int index)
    {
        int quadrant = findQuadrant(point);

        if(quadrant != -1)
            QuadrantsElements[quadrant] += index + "-";
    }

    int findQuadrant(Vector3 point)
    {
        for (int i = 0; i < QuadrantsBounds.Count; ++i)
        {
            if (QuadrantsBounds[i].Contains(point))
            {
                return i;
            }
        }

        return -1;
    }

    float normalizeCharge(float charge)
    {
        float vibration = Math.Abs((charge - minCharge) / (maxCharge - minCharge));

        if (charge < 100)
        {
            for (int i = 1; i < vibrationIntervals.Length; i++)
            {
                if (charge > vibrationIntervals[i - 1] && charge < vibrationIntervals[i] && charge >= 1.5)
                {
                    vibration += vibrationMapping[i + 1];

                    if (vibration > 1)
                    {
                        vibration = 1;
                    }

                    return vibration;
                }
            }
        }

        return vibration;
    }

    public void findHandsVibrationOptimized()
    {
        float RAmplitude = 0;
        float LAmplitude = 0;

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        int quadIndexR = findQuadrant(RPos);
        int quadIndexL = findQuadrant(LPos);

        if (quadIndexR == quadIndexL && quadIndexR != -1)
        {
            String[] indexQuad = QuadrantsElements[quadIndexR].Split('-');

            float lessDistanceR = 1000;
            float lessDistanceL = 1000;

            for (int i = 0; i < indexQuad.Length - 1; ++i)
            {
                Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuad[i])].x, points[Int32.Parse(indexQuad[i])].y, points[Int32.Parse(indexQuad[i])].z);

                if (Vector3.Distance(pointPos, RPos) < lessDistanceR)
                {
                    RAmplitude = pointsCharges[Int32.Parse(indexQuad[i])];
                    lessDistanceR = Vector3.Distance(pointPos, RPos);
                }

                if (Vector3.Distance(pointPos, LPos) < lessDistanceL)
                {
                    LAmplitude = pointsCharges[Int32.Parse(indexQuad[i])];
                    lessDistanceL = Vector3.Distance(pointPos, LPos);
                }
            }

        }
        else
        {

            if (quadIndexR != -1)
            {
                String[] indexQuadR = QuadrantsElements[quadIndexR].Split('-');

                float lessDistance = 1000f;

                for (int i = 0; i < indexQuadR.Length - 1; ++i)
                {
                    Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuadR[i])].x, points[Int32.Parse(indexQuadR[i])].y, points[Int32.Parse(indexQuadR[i])].z);
                    if (Vector3.Distance(pointPos, RPos) < lessDistance)
                    {
                        RAmplitude = pointsCharges[Int32.Parse(indexQuadR[i])];
                        lessDistance = Vector3.Distance(pointPos, RPos);
                    }
                }
            }

            if (quadIndexL != -1)
            {
                String[] indexQuadL = QuadrantsElements[quadIndexL].Split('-');
                float lessDistance = 1000f;

                for (int i = 0; i < indexQuadL.Length - 1; ++i)
                {
                    Vector3 pointPos = new Vector3(points[Int32.Parse(indexQuadL[i])].x, points[Int32.Parse(indexQuadL[i])].y, points[Int32.Parse(indexQuadL[i])].z);

                    if (Vector3.Distance(pointPos, LPos) < lessDistance)
                    {
                        LAmplitude = pointsCharges[Int32.Parse(indexQuadL[i])];
                        lessDistance = Vector3.Distance(pointPos, LPos);
                    }

                }
            }

        }

        float vibrationR = Mathf.Pow(normalizeCharge(RAmplitude), Gamma);
        vibrationDEBUG.text = vibrationR + "";

        OVRInput.SetControllerVibration(1, vibrationR, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, Mathf.Pow(normalizeCharge(LAmplitude), Gamma), OVRInput.Controller.LTouch);
    }

    public void findHandsVibrationDirectly()
    {
        float RAmplitude = 0;
        float LAmplitude = 0;

        Vector3 RPos = RHand.transform.position;
        Vector3 LPos = LHand.transform.position;

        float vibrationR = Mathf.Pow(normalizeCharge(marchingCubes.ElectromagnetismCharge(RPos)), Gamma);
        float vibrationL = Mathf.Pow(normalizeCharge(marchingCubes.ElectromagnetismCharge(LPos)), Gamma);

        OVRInput.SetControllerVibration(1, vibrationR, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, vibrationL, OVRInput.Controller.LTouch);
    }

    public void resetHaptic()
    {
        OVRInput.SetControllerVibration(1, 0.0f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(1, 0.0f, OVRInput.Controller.LTouch);
    }

    #endregion

    #region Private Methods

    public float GetVector3Value(Vector3 a, int position)
    {
        float value = 0f;

        switch (position)
        {
            case 0:
                value = a.x;
                break;
            case 1:
                value = a.y;
                break;
            case 2:
                value = a.z;
                break;
        }

        return value;
    }

    void resetPlayerPosition()
    {
        characterController.enabled = false;
        player.transform.position = new Vector3(0.0f, player.transform.position.y, -18f);
        player.transform.rotation = Quaternion.identity;
        trackingSpace.rotation = Quaternion.identity;
        characterController.enabled = true;
    }

    void moveToLobby() // TO DO: Change to Survey Scene.
    {
        characterController.enabled = false;
        player.transform.position = new Vector3(-320.0f, player.transform.position.y, -18f);
        player.transform.rotation = Quaternion.identity;
        trackingSpace.rotation = Quaternion.identity;
        characterController.enabled = true;
    }

    void getUserID()
    {
        if (PlayerPrefs.GetInt("playerID") == 0)
        {
            PlayerPrefs.SetInt("playerID", 1);
        }
        else
        {
            PlayerPrefs.SetInt("playerID", PlayerPrefs.GetInt("playerID") + 1);
        }


        playerID = "HMD" + HMDNumber + "-" + PlayerPrefs.GetInt("playerID");

        playerIDLabel.text = "Student ID: " + playerID;
    }

    void cleanPointsLabels()
    {
        if (interestPoints.Length > currentScene)
        {
            GameObject points = interestPoints[currentScene].transform.GetChild(0).gameObject;

            points.GetComponent<InterestPoint>().Reset();

            for (int i = 1; i < interestPoints[currentScene].transform.childCount; i++)
            {
                points = interestPoints[currentScene].transform.GetChild(i).gameObject;
                points.GetComponent<InterestPoint>().Reset();
            }
        }
    }

    void InitIntructions()
    {
        SceneControl.SetActive(false);
        InitialInstruction.SetActive(true);
    }

    void setPhaseLabel()
    {
        Debug.Log("The current phase is: " + currentPhase);
        phaseLabel.text = PhaseNames[currentPhase];
        phaseInstruction.text = instructions[3*currentScene + currentPhase];
        phaseInstruction2.text = instructions[3*currentScene + currentPhase];
    }

    void resetParticlePosition()
    {
        for (int i = 0; i < particleList.Count; ++i)
        {
            particleList[i].transform.position = initialPositions[particleList[i].transform.gameObject.name];
        }

        updateIsosurface();
        trackParticle();
    }

    NativeArray<Vector3> GetParticlePositions()
    {
        NativeArray<Vector3> positions = new NativeArray<Vector3>(particleList.Count, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particleList)
        {
            positions[index] = particle.transform.position;
            index++;
        }

        return positions;
    }

    NativeArray<float> GetChargeValues()
    {
        NativeArray<float> chargesNative = new NativeArray<float>(particleList.Count, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particleList)
        {
            chargesNative[index] = particleList[index].charge;
            index++;
        }

        return chargesNative;
    }

    #endregion

    #region User Stats

    void startScene()
    {
        mainScene = new SceneData();
        mainScene.sceneTime = Time.time;

        for (int i = 0; i < particleList.Count; ++i)
        {
            mainScene.particlePositions.Add(new ParticleData(particleList[i].charge > 0, particleList[i].transform.position));
            initialTimePerParticle[i] = 0.0f;
        }

        initPhaseTime();
        reportData = true;
    }

    void UpdateStats()
    {
        if (reportData)
        {
            dataController.ButtonsPressed(ref mainScene);
        }
    }

    void UIClick()
    {
        mainScene.UIClick += 1;
    }

    void IsosurfaceCalculate()
    {
        if(noActivityReport)
            mainScene.isosurfaceCalculate += 1;
    }

    void calculateInterestPointInteraction()
    {
        if (interestPoints.Length > currentScene)
        {
            for (int i = 0; i < interestPoints[currentScene].transform.childCount; ++i)
            {
                GameObject point = interestPoints[currentScene].transform.GetChild(i).gameObject;
                float interactionTime = point.GetComponent<InterestPoint>().interactionTime;
                mainScene.interestPointDuration.Add(new InterestPointData(point.transform.position, interactionTime));
            }

        }
    }

    void trackParticle()
    {
        if(noActivityReport)
            for (int i = 0; i < mainScene.particlePositions.Count; ++i)
            {
                mainScene.particlePositions[i].addPosition(particleList[i].transform.position);
            }
    }

    void validateParticleGrab(int i)
    {
        if (!particleList[i].GetOVRGrababble().isGrabbed && initialTimePerParticle[i] != 0.0f)
        {
            if (noActivityReport)
                if (particleList[i].charge > 0)
                {
                    mainScene.positiveParticleGrabTime += (Time.time - initialTimePerParticle[i]);
                }
                else
                {
                    mainScene.negativeParticleGrabTime += (Time.time - initialTimePerParticle[i]);
                }

            particleList[i].transform.gameObject.GetComponent<MeshRenderer>().material.color = currentColor;
            GameController.lastPointRelease = Time.time;
            initialTimePerParticle[i] = 0.0f;
        }
    }

    void initPhaseTime()
    {
        currentPhaseTime = Time.time;
    }

    void setPhaseTime()
    {
        mainScene.phaseTime.Add(Time.time - currentPhaseTime);
    }

    void resetParticleTime()
    {
        for (int i = 0; i < particleList.Count; ++i)
        {
            initialTimePerParticle[i] = 0.0f;
        }
    }

    void saveSceneData()
    {
        mainScene.sceneTime = (Time.time - mainScene.sceneTime);
        calculateInterestPointInteraction();

        SceneData scene = new SceneData();

        reportData = false;

        scene.copyScene(mainScene);

        SceneController.controller.SceneInfo(scene);
    }

    void saveJson()
    {
        SceneController.controller.generateUserResponses();
        SceneController.controller.SaveIntoJson(playerID, microphone.filename);
    }

    void reportUser()
    {
        if (reportData)
        {
            dataController.reportUser(ref mainScene, player);
        }
    }

    void initReport()
    {
        InvokeRepeating("reportUser", 2.0f, 2.0f);
    }

    #endregion
}

