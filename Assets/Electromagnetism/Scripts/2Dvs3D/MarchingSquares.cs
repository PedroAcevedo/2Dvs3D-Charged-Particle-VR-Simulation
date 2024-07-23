using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.UI;

// Marching Squares implementation adapted from https://github.com/timfornell/P5-MarchinSquares
public class MarchingSquares : MonoBehaviour
{
    public int canvasSize;
    public static float K = 8.98685134e9f;      //Coulomb's law constant
    public int gridSize;
    public GameObject canvas;
    public GameObject squarePrefab;
    public GameObject EFParent;
    public GameObject MSParent;
    public GameObject gridReference;
    public GameObject square;
    public GameObject testPoint;
    public GameObject testPointVR;
    public Material surfaceMaterial;
    public List<Particle> particleList;
    public List<Vector3> particleSetting;
    public Transform interestPoints;
    public float[] charges;
    public float scaled;
    public float toY;

    private PointGrid[,] points;
    private CellGrid[,] cells;
    private List<GameObject> lines;
    private ParticleLines lineController;
    private float squareSize;

    private int m, n;
    private Vector2[] simulationLimits;

    NativeArray<PointGrid> pointsNative;
    private bool executingThread;
    private Vector2 canvasCenter;
    private Vector2 initGrid;
    private float gridSeparation;
    private SimulationRaycaster[] particleInCanvas;
    private OVR2DVRControlUI controlUI;

    // Parallel struture to handle multiple CPU cores for Electric field calculation
    public struct PointInGridUpdate: IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> particles;
        [ReadOnly]
        public NativeArray<float> charges;
        public NativeArray<PointGrid> pointList;

        public void Execute(int index)
        {
            var temp = pointList[index];
            temp.value = Electromagnetism3(temp.currentPosition);

            temp.binaryValue = 0;

            if (temp.value <= 0.5)
            {
                temp.binaryValue = 1;
            }

            pointList[index] = temp;
        }

        public float Electromagnetism3(Vector3 currentPosition)
        {
            float totalForce = 0;

            for (int i = 0; i < particles.Length; ++i)
            {
                float r = Utils.VectorDistance(particles[i], currentPosition);
                totalForce += Mathf.Abs(Utils.ElectricField(r, charges[i] * 1e-9f)) * Utils.blendFunction0(r);
            }

            return totalForce;
        }

        public float ElectricField(Vector3 a, Vector3 b, float charge)
        {
            float distance = new Vector3((b.x - a.x), (b.y - a.y), (b.z - a.z)).magnitude;

            return (K * charge) / (distance * distance);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        squareSize = (float)canvasSize / (float)gridSize;
        m = gridSize;
        n = m;

        canvasCenter = new Vector2(canvas.transform.position.x, canvas.transform.position.y);
        points = new PointGrid[n,m];
        cells = new CellGrid[n,m];
        lines = new List<GameObject>();

        simulationLimits = new Vector2[4];

        float canvasEnd = canvasSize / 2;

        simulationLimits[0] = new Vector2(-canvasEnd, -canvasEnd) + canvasCenter;
        simulationLimits[1] = new Vector2(-canvasEnd, canvasEnd) + canvasCenter;
        simulationLimits[2] = new Vector2(canvasEnd, canvasEnd) + canvasCenter;
        simulationLimits[3] = new Vector2(canvasEnd, -canvasEnd) + canvasCenter;

        lineController = new ParticleLines(ref particleList);
        lineController.zOffset = 0.0f;
        lineController.simulationLimits = simulationLimits;
        lineController.linesLimit = new Vector3(25 + canvasCenter.x, 25 + canvasCenter.y, 0);
        lineController.parent = EFParent;
        lineController.Draw(true);


        pointsNative = new NativeArray<PointGrid>(n*n, Allocator.TempJob);
        particleInCanvas = new SimulationRaycaster[charges.Length];
        controlUI = GetComponent<OVR2DVRControlUI>();

        for (int i = 0; i < particleList.Count; i++)
        {
            particleInCanvas[i] = particleList[i].transform.gameObject.GetComponent<SimulationRaycaster>();
            particleList[i] = new Particle(particleList[i].transform, particleList[i].charge, particleList[i].initialPosition, particleList[i].transform.gameObject, true);
            particleList[i].SetChargeValue(GetVector3Value(particleSetting[GameController.currentRelation], i));
            particleList[i].SetParticleMaterial();
            particleList[i].ResetPosition();
        }

        GeneratedGrid();
        DrawLines();
    }
    
    // Creates a 2D grid for the resolution of the contours
    void GeneratedGrid()
    {
        float adaptSize = squareSize * scaled;
        float offset = (canvasSize / 2) * scaled;

        initGrid = new Vector2((8 * adaptSize) - offset, (8 * adaptSize) - offset) + canvasCenter;
        gridSeparation = 8 * adaptSize;

        for (int i = 0; i < n; i++)
        {
            for(int j = 0; j < m; j++)
            {
                Vector2 position = new Vector2((i * adaptSize) - offset, (j * adaptSize) - offset) + canvasCenter;
                points[i, j] = new PointGrid(position, 0, 0);
                pointsNative[j * n + i] = new PointGrid(position, 0, 0);
            }
        }
    }

    void BuildMap()
    {
        for (int x = 0; x < n - 1; x++)
        {
            for (int y = 0; y < m - 1; y++)
            {
                var index = 0;
                // Represent the corners by a 4-bit number, top left is MSB and bottom left is LSB
                index |= pointsNative[y*n + x].binaryValue << 3;
                index |= pointsNative[y * n + (x+1)].binaryValue << 2;
                index |= pointsNative[(y+1) * n + (x+1)].binaryValue << 1;
                index |= pointsNative[(y + 1) * n + x].binaryValue << 0;

                cells[x, y] = new CellGrid(index);
            }
        }
    }
    
    // Calculate the new lines visualization on each of the cells
    public void DrawLines()
    {
        foreach (GameObject line in lines)
            GameObject.Destroy(line);

        CalculateLines();
        for (int x = 0; x < n-1; x++)
        {
            for (int y = 0; y < m-1; y++)
            {
                if (cells[x, y].lines.Count > 0)
                    DrawLine(cells[x, y].GetAllLines(canvasSize, scaled, canvasCenter));
            }
        }
    }

    // Display the line on the scene
    public void DrawLine(List<Vector2> pointList)
    {
        GameObject go = new GameObject($"LineRenderer_particle_1");
        go.transform.parent = MSParent.transform;
        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.material = surfaceMaterial;
        goLineRenderer.startWidth = 0.10f;
        goLineRenderer.endWidth = 0.10f;
        goLineRenderer.sortingOrder = 1;

        goLineRenderer.positionCount = pointList.Count;

        Vector3[] points = new Vector3[pointList.Count];

        for(int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector3(pointList[i].x, pointList[i].y, 0.0f);
        }

        goLineRenderer.SetPositions(points);
        lines.Add(go);
    }

    // Linear interpolation on each of the cells. Access to the look up table for line composition. 
    public void CalculateLines()
    {

        float adaptSize = squareSize * scaled;

        BuildMap();

        for (int x = 0; x < n - 1; x++)
        {
            for (int y = 0; y < m - 1; y++)
            {
                int index = cells[x, y].index;
                Vector2 topLeft = points[x, y].currentPosition;
                Vector2 bottomRight = new Vector2(topLeft.x + adaptSize, topLeft.y + adaptSize);

                var leftEdgeValueFactor = GetInterpolatedValue(points[x, y].value, points[x, y + 1].value);
                var rightEdgeValueFactor = GetInterpolatedValue(points[x + 1, y].value, points[x + 1, y + 1].value);
                var topEdgeValueFactor = GetInterpolatedValue(points[x, y].value, points[x + 1, y].value);
                var bottomEdgeValueFactor = GetInterpolatedValue(points[x, y + 1].value, points[x + 1, y + 1].value);
                var leftEdgePoint = new Vector2(topLeft.x, topLeft.y + adaptSize * leftEdgeValueFactor);
                var rightEdgePoint = new Vector2(bottomRight.x, topLeft.y + adaptSize * rightEdgeValueFactor);
                var topEdgePoint = new Vector2(topLeft.x + adaptSize * topEdgeValueFactor, topLeft.y);
                var bottomEdgePoint = new Vector2(topLeft.x + adaptSize * bottomEdgeValueFactor, bottomRight.y);

                //Look up table condition
                if (index == 0)
                {
                    // Empty cell
                }
                else if (index == 1)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(bottomEdgePoint.x, bottomEdgePoint.y))); ;
                }
                else if (index == 2)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(bottomEdgePoint.x, bottomEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 3)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 4)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(topEdgePoint.x, topEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 5)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(topEdgePoint.x, topEdgePoint.y))); ;
                    cells[x, y].lines.Add(new Line(new Vector2(bottomEdgePoint.x, bottomEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 6)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(topEdgePoint.x, topEdgePoint.y), new Vector2(bottomEdgePoint.x, bottomEdgePoint.y))); ;
                }
                else if (index == 7)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(topEdgePoint.x, topEdgePoint.y))); ;
                }
                else if (index == 8)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(topEdgePoint.x, topEdgePoint.y))); ;
                }
                else if (index == 9)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(topEdgePoint.x, topEdgePoint.y), new Vector2(bottomEdgePoint.x, bottomEdgePoint.y))); ;
                }
                else if (index == 10)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(bottomEdgePoint.x, bottomEdgePoint.y))); ;
                    cells[x, y].lines.Add(new Line(new Vector2(topEdgePoint.x, topEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 11)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(topEdgePoint.x, topEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 12)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 13)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(bottomEdgePoint.x, bottomEdgePoint.y), new Vector2(rightEdgePoint.x, rightEdgePoint.y))); ;
                }
                else if (index == 14)
                {
                    cells[x, y].lines.Add(new Line(new Vector2(leftEdgePoint.x, leftEdgePoint.y), new Vector2(bottomEdgePoint.x, bottomEdgePoint.y))); ;
                }
                else if (index == 15)
                {
                    // Empty cell
                }
            }
        }
    }

    private JobHandle handle;

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < particleList.Count; ++i)
        {
            if (particleList[i].transform.hasChanged && !executingThread)
            {
                //DrawLines();
                //lineController.Draw(true);

                PointInGridUpdate jobData = new PointInGridUpdate();
                jobData.pointList = pointsNative;
                jobData.particles = GetParticlePositions();
                jobData.charges = GetChargeValues();

                executingThread = true;
                // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
                handle = jobData.Schedule(pointsNative.Length, 32);

            }
            particleList[i].transform.hasChanged = false;
        }
    }

    private void LateUpdate()
    {
        if (executingThread)
        {
            handle.Complete();

            lineController.Draw(true);
            DrawLines();

            executingThread = false;
        }
    }

    public NativeArray<Vector3> GetParticlePositions()
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

    public NativeArray<float> GetChargeValues()
    {
        NativeArray<float> chargesNative = new NativeArray<float>(charges.Length, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particleList)
        {
            chargesNative[index] = charges[index];
            index++;
        }

        return chargesNative;
    }
    public float GetInterpolatedValue(float lowValue, float highValue)
    {
        float valueRange = 2; // diff can have values in [-1, 1]
        float desiredValueRange = 1; // Size of desired range [0, 1]
        float diff = highValue - lowValue;

        // This is valid as long ass the diff values are centered around 0
        float mappedValue = (diff + desiredValueRange) / (valueRange / desiredValueRange);

        return mappedValue;
    }

    public float Electromagnetism3(Vector3 currentPosition)
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < particleList.Count; i++)
        {
            float electric_magnitude = Utils.ElectricField(particleList[i].transform.position, currentPosition, particleList[i].charge);
            totalForce += Utils.GetDirectonalVector(currentPosition, particleList[i].transform.position) * electric_magnitude;
        }

        return Mathf.Abs(totalForce.magnitude);
    }

    public float ElectricField(Vector3 a, Vector3 b, float charge)
    {
        float distance = new Vector3((b.x - a.x), (b.y - a.y), (b.z - a.z)).magnitude;

        return (K * charge) / (distance * distance);
    }

    public string getPointValue(Vector3 point)
    {
        return (int)Electromagnetism3(point) + "";
    }

    public void ResetParticlesPositons()
    {
        for (int i = 0; i < particleList.Count; i++)
        {
            particleList[i].SetChargeValue(GetVector3Value(particleSetting[GameController.currentRelation], i));
            particleList[i].SetParticleMaterial();
            particleList[i].ResetPosition();
        }
    }

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
}

public struct PointGrid
{
    public Vector3 currentPosition;
    public int binaryValue;
    public float value;

    public PointGrid(Vector3 currentPosition, int binaryValue, float value)
    {
        this.currentPosition = currentPosition;
        this.binaryValue = binaryValue;
        this.value = value;
    }
}

struct CellGrid
{
    public int index;
    public List<Line> lines;

    public CellGrid(int index)
    {
        this.index = index;
        lines = new List<Line>();
    }

    public List<Vector2> GetAllLines(int canvasSize, float scale, Vector2 center)
    {
        float offset = (canvasSize / 2) * scale;
        List<Vector2> allLines = new List<Vector2>();

        for(int i = 0; i < lines.Count; i++)
        {
            allLines.Add(new Vector2(lines[i].start.x , lines[i].start.y));
            allLines.Add(new Vector2(lines[i].end.x, lines[i].end.y));
        }

        return allLines;
    }
}

struct Line
{
    public Vector2 start;
    public Vector2 end;

    public Line(Vector2 start, Vector2 end)
    {
        this.start = start;
        this.end = end;
    }
}
 