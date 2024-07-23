using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.Jobs;
using Unity.Collections;

public class ParticleLines
{
    public List<Particle> particles;
    public bool showForces;
    public float zOffset = 0.3199998f;
    public Vector2[] simulationLimits = {
        new Vector2(-9.5f, -9.5f),
        new Vector2(-9.5f, 9.5f),
        new Vector2(9.5f, 9.5f),
        new Vector2(9.5f, -9.5f),
    };
    public Vector3 linesLimit = new Vector3(10, 10, 10);
    public GameObject parent;


    private float particleRadius = 0.25f;
    private Vector3[] lookUpTable = {
        new Vector3(0.0f, 5.0f, 0.0f),
        new Vector3(0.0f, 4.0f, 0.0f),
        new Vector3(0.0f, 3.0f, 0.0f),
        new Vector3(0.0f, 2.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(0.0f, -2.0f, 0.0f),
        new Vector3(0.0f, -3.0f, 0.0f),
        new Vector3(0.0f, -4.0f, 0.0f),
        new Vector3(0.0f, -5.0f, 0.0f),
    };


    private float lineDefaultWidth = 0.010f;
    private List<GameObject> lines = new List<GameObject>();
    private List<GameObject> arrows = new List<GameObject>();
    private int FIELD_LINES = 12;
    private static float eps;
    private static float EPSILON;
    private JobHandle handle;
    private GameObject EFLinePrefab;
    private GameObject arrowPrefab;

    // Job adding two floating point values together
    public struct ElectricFieldJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> particles;
        [ReadOnly]
        public NativeArray<float> charges;
        [ReadOnly]
        public float fieldLines;
        [ReadOnly]
        public int index;
        [ReadOnly]
        public float zOffset;
        [ReadOnly]
        public Vector3 linesLimit;

        public NativeArray<ManagedObjectRef<List<Vector3>>> linePoints;

        public void Execute(int i)
        {
            float x = particles[index].x + eps * Mathf.Cos((float)(2 * Math.PI * i / fieldLines));
            float y = particles[index].y + eps * Mathf.Sin((float)(2 * Math.PI * i / fieldLines));

            bool reachedAnotherCharge = false;

            // Check for infinite loop 
            bool infiniteLoop = false;
            int count = 0;
            float[] oldXs = { 0.0f, 0.0f };
            float[] oldYs = { 0.0f, 0.0f };

            List<Vector3> linePositions = new List<Vector3>();

            while (!reachedAnotherCharge && !infiniteLoop
                     && x > -linesLimit.x && x < linesLimit.x && y > -linesLimit.y && y < linesLimit.y)
            {

                // find the field (Ex, Ey, Ez) and field strength E at (x,y.z)
                float[] E = ETotal2D(x, y);
                float n = (float)Mathf.Sqrt(E[0] * E[0] + E[1] * E[1]);

                // if charge is negative the line needs to go backwards
                if (charges[index] > 0)
                {
                    x += E[0] / n * eps;
                    y += E[1] / n * eps;
                }
                else
                {
                    x -= E[0] / n * eps;
                    y -= E[1] / n * eps;
                }

                linePositions.Add(new Vector3(x, y, zOffset));

                // stop in infinite loop
                if (Math.Abs(x - oldXs[0]) < EPSILON && Math.Abs(y - oldYs[0]) < EPSILON)
                {
                    infiniteLoop = true;
                    break;
                }
                int index2 = count++ % 2;
                oldXs[index2] = x;
                oldYs[index2] = y;

                // stop if the line ends in a charge
                for (int j = 0; j < charges.Length; j++)
                {
                    float dx = x - particles[j].x;
                    float dy = y - particles[j].y;

                    if (Math.Sqrt(dx * dx + dy * dy) < eps) reachedAnotherCharge = true;
                }

            }
            linePoints[i] = world.Add(linePositions); //new ManagedObjectRef<string>();
        }

        private float[] ETotal2D(float x, float y)
        {
            float[] Exy = new float[2];

            Exy[0] = 0.0f;
            Exy[1] = 0.0f;

            for (int i = 0; i < charges.Length; i++)
            {
                float[] E = pointCharge2D(charges[i], particles[i], x, y);

                Exy[0] = Exy[0] + E[0];
                Exy[1] = Exy[1] + E[1];
            }

            return Exy;
        }
        private float[] pointCharge2D(float charge, Vector3 position, float x, float y)
        {
            float diffx = (x - position.x);
            float diffy = (y - position.y);

            float distance = (float)Math.Pow((diffx* diffx) + (diffy* diffy), 1.5f);

            float[] chargeOnPoint = new float[2];

            chargeOnPoint[0] = charge * diffx / distance;
            chargeOnPoint[1] = charge * diffy / distance;

            return chargeOnPoint;
        }
    }

    public ParticleLines(ref List<Particle> particles)
    {
        this.showForces = true;
        this.particles = particles;
        eps = particleRadius / 2;
        EPSILON = particleRadius / (2 * 1.0E1f);
       
        //Array.Sort(charges, particles); WHY SORT?

        world = new ManagedObjectLines();

        electricFieldLinesArray = new NativeArray<ManagedObjectRef<List<Vector3>>>(FIELD_LINES, Allocator.TempJob);

        for (int i = 0; i < FIELD_LINES; i++)
        {
            electricFieldLinesArray[i] = world.Add(new List<Vector3>());
        }

        arrowPrefab = (Resources.Load("Prefabs/arrow")) as GameObject;
        EFLinePrefab = (Resources.Load("Prefabs/Line")) as GameObject;
    }

    public void Draw(bool mode)
    {
        CleanLines();

        for (int i = 0; i < particles.Count; i++)
        {
            if (mode)
            {
                if (isInsideSimulationBox2D(particles[i].transform.position))
                {
                    //DrawElectricLinesParticles2D(i);
                    DrawElectricLinesParticles2DParallel(i);
                }
            }
            else
            {
                drawElectricLinesParticles3D(i);
            }
        }
    }

    public void CleanLines()
    {
        if (lines.Count > 0)
        {
            foreach (GameObject line in lines)
                GameObject.Destroy(line);
            foreach (GameObject arrow in arrows)
                GameObject.Destroy(arrow);
        }
    }

    public void AddToParent(GameObject child)
    {
        if (parent != null)
        {
            child.transform.parent = parent.transform;
        }
    }

    public void AddArrow(Vector3 position, Vector3 nextPosition)
    {
        GameObject arrow = GameObject.Instantiate(arrowPrefab, position, Quaternion.identity) as GameObject;

        arrow.transform.LookAt(nextPosition);
        arrows.Add(arrow);

        AddToParent(arrow);

        Vector3 diff = position - nextPosition;
        diff.Normalize();

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 180);
        arrow.transform.localScale -= new Vector3(0.0f, 0.00781458f, 0.0f);
    }

    private void drawElectricLinesParticles3D(int index)
    {
        for (int i = 0; i < FIELD_LINES; i++)
        {

            float x = particles[index].transform.position.x + eps * Mathf.Cos((float)(2 * Math.PI * i / FIELD_LINES));
            float y = particles[index].transform.position.y + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));
            float z = particles[index].transform.position.z + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));

            bool reachedAnotherCharge = false;

            // Check for infinite loop 
            bool infiniteLoop = false;
            int count = 0;
            float[] oldXs = { 0.0f, 0.0f };
            float[] oldYs = { 0.0f, 0.0f };
            float[] oldZs = { 0.0f, 0.0f };

            List<Vector3> lineField = new List<Vector3>();
            while (!reachedAnotherCharge && !infiniteLoop
                     && x > -linesLimit.x && x < linesLimit.x && y > -linesLimit.y && y < linesLimit.y
                     && z > -linesLimit.z && z < linesLimit.z)
            {

                // find the field (Ex, Ey, Ez) and field strength E at (x,y.z)
                float[] E = ETotal(x, y, z);
                float n = (float)Mathf.Sqrt(E[0] * E[0] + E[1] * E[1] + E[2] * E[2]);

                // if charge is negative the line needs to go backwards
                if (particles[index].charge > 0)
                {
                    x += E[0] / n * eps;
                    y += E[1] / n * eps;
                    z += E[2] / n * eps;
                }
                else
                {
                    x -= E[0] / n * eps;
                    y -= E[1] / n * eps;
                    z -= E[2] / n * eps;
                }

                lineField.Add(new Vector3(x, y, z));

                // stop in infinite loop
                if (Math.Abs(x - oldXs[0]) < EPSILON && Math.Abs(y - oldYs[0]) < EPSILON && Math.Abs(z - oldZs[0]) < EPSILON)
                {
                    infiniteLoop = true;
                }
                int index2 = count++ % 2;
                oldXs[index2] = x;
                oldYs[index2] = y;
                oldZs[index2] = z;


                // stop if the line ends in a charge
                for (int j = 0; j < particles.Count; j++)
                {
                    float dx = x - particles[j].transform.position.x;
                    float dy = y - particles[j].transform.position.y;
                    float dz = z - particles[j].transform.position.z;

                    if (Math.Sqrt(dx * dx + dy * dy + dz * dz) < eps) reachedAnotherCharge = true;
                }

            }
            if (index < particles.Count)
                AddNewLineRendererList(new Color(1.0f, 0.0f, 0.0f), lineField, particles[index].charge);

        }


    }

    private void DrawElectricLinesParticles2D(int index)
    {
        for (int i = 0; i < FIELD_LINES; i++)
        {
            float x = particles[index].transform.position.x + eps * Mathf.Cos((float)(2 * Math.PI * i / FIELD_LINES));
            float y = particles[index].transform.position.y + eps * Mathf.Sin((float)(2 * Math.PI * i / FIELD_LINES));

            bool reachedAnotherCharge = false;

            // Check for infinite loop 
            bool infiniteLoop = false;
            int count = 0;
            float[] oldXs = { 0.0f, 0.0f };
            float[] oldYs = { 0.0f, 0.0f };

            List<Vector3> lineField = new List<Vector3>();
            while (!reachedAnotherCharge && !infiniteLoop
                     && x > -linesLimit.x && x < linesLimit.x && y > -linesLimit.y && y < linesLimit.y)
            {

                // find the field (Ex, Ey, Ez) and field strength E at (x,y.z)
                float[] E = ETotal2D(x, y);
                float n = (float)Mathf.Sqrt(E[0] * E[0] + E[1] * E[1]);

                // if charge is negative the line needs to go backwards
                if (particles[index].charge > 0)
                {
                    x += E[0] / n * eps;
                    y += E[1] / n * eps;
                }
                else
                {
                    x -= E[0] / n * eps;
                    y -= E[1] / n * eps;
                }

                lineField.Add(new Vector3(x, y, zOffset));

                // stop in infinite loop
                if (Math.Abs(x - oldXs[0]) < EPSILON && Math.Abs(y - oldYs[0]) < EPSILON)
                {
                    infiniteLoop = true;
                    break;
                }
                int index2 = count++ % 2;
                oldXs[index2] = x;
                oldYs[index2] = y;

                // stop if the line ends in a charge
                for (int j = 0; j < particles.Count; j++)
                {
                    float dx = x - particles[j].transform.position.x;
                    float dy = y - particles[j].transform.position.y;

                    if (Math.Sqrt(dx * dx + dy * dy) < eps) reachedAnotherCharge = true;
                }

            }

            if (index < particles.Count)
                AddNewLineRendererList(new Color(1.0f, 0.0f, 0.0f), lineField, particles[index].charge);

        }

    }

    NativeArray<ManagedObjectRef<List<Vector3>>> electricFieldLinesArray;

    public static ManagedObjectLines world;

    private void DrawElectricLinesParticles2DParallel(int index)
    {
        ElectricFieldJob jobData = new ElectricFieldJob();
        jobData.particles = GetParticlePositions();
        jobData.charges = GetChargeValues();
        jobData.fieldLines = FIELD_LINES;
        jobData.index = index;
        jobData.zOffset = zOffset;
        jobData.linesLimit = linesLimit;
        jobData.linePoints = electricFieldLinesArray;

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        handle = jobData.Schedule(FIELD_LINES, 64);

        handle.Complete();

        foreach (ManagedObjectRef<List<Vector3>> line in electricFieldLinesArray)
        {
            AddNewLineRendererList(new Color(1.0f, 0.0f, 0.0f), world.Get(line), particles[index].charge);
        }

        // Free the memory allocated by the arrays
        // electricFieldLinesArray.Dispose();
    }

    private void AddNewLineRendererList(Color color, List<Vector3> pointList, float charge)
    {
        GameObject go = GameObject.Instantiate(EFLinePrefab, Vector3.zero, Quaternion.identity) as GameObject;
        LineRenderer goLineRenderer = go.GetComponent<LineRenderer>();
        AddToParent(go);
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;
        goLineRenderer.useWorldSpace = false;

        goLineRenderer.positionCount = pointList.Count;
        goLineRenderer.SetPositions(pointList.ToArray());

        if (this.showForces && pointList.Count > 4)
        {
            int segments = 4;

            if (pointList.Count < 100 && pointList.Count > 20)
            {
                segments = (int)Mathf.Lerp(10, 4, (pointList.Count / 100.0f));
            }


            int arrowStartPosition = pointList.Count / 8;
            int arrowEndPosition = pointList.Count - arrowStartPosition;

            int numberArrow = arrowEndPosition / segments;


            if (charge > 0)
            {
                for (int i = arrowStartPosition; i < arrowEndPosition; i += numberArrow)
                {
                    if (i < (pointList.Count - 1))
                        AddArrow(pointList[i], pointList[i + 1]);
                }
            }
            else
            {
                for (int i = arrowEndPosition; i > arrowStartPosition; i -= numberArrow)
                {
                    if (i > 0 && i < (pointList.Count))
                        AddArrow(pointList[i], pointList[i - 1]);
                }
            }

        }

        lines.Add(go);
    }

    private bool inParticle(Vector3 position, bool distances)
    {
        if (distances)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                if (Vector2.Distance(new Vector2(particles[i].transform.position.x, particles[i].transform.position.y), new Vector2(position.x, position.y)) < 0.25)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private float[] pointCharge(float charge, Vector3 position, float x, float y, float z)
    {
        float diffx = (x - position.x);
        float diffy = (y - position.y);
        float diffz = (z - position.z);

        float distance = (float)Math.Pow((diffx * diffx) + (diffy * diffy) + (diffz* diffz), 1.5f);

        float[] chargeOnPoint = new float[3];

        chargeOnPoint[0] = charge * diffx / distance;
        chargeOnPoint[1] = charge * diffy / distance;
        chargeOnPoint[2] = charge * diffz / distance;

        return chargeOnPoint;
    }

    private float[] pointCharge2D(float charge, Vector3 position, float x, float y)
    {
        float diffx = (x - position.x);
        float diffy = (y - position.y);

        float distance = (float)Math.Pow((diffx * diffx) + (diffy * diffy), 1.5f);

        float[] chargeOnPoint = new float[2];

        chargeOnPoint[0] = charge * diffx / distance;
        chargeOnPoint[1] = charge * diffy / distance;

        return chargeOnPoint;
    }

    private float[] ETotal(float x, float y, float z)
    {
        float[] Exy = new float[3];

        Exy[0] = 0.0f;
        Exy[1] = 0.0f;
        Exy[2] = 0.0f;

        for (int i = 0; i < particles.Count; i++)
        {
            //if (xp > -linesLimit.x && xp < linesLimit.x && yp > -linesLimit.y && yp < linesLimit.y
            //         && zp > -linesLimit.z && zp < linesLimit.z)
            //{
                float[] E = pointCharge(particles[i].charge, particles[i].transform.position, x, y, z);

                Exy[0] = Exy[0] + E[0];
                Exy[1] = Exy[1] + E[1];
                Exy[2] = Exy[2] + E[2];
            //}
        }

        return Exy;
    }

    private float[] ETotal2D(float x, float y)
    {
        float[] Exy = new float[2];

        Exy[0] = 0.0f;
        Exy[1] = 0.0f;

        for (int i = 0; i < particles.Count; i++)
        {
            float[] E = pointCharge2D(particles[i].charge, particles[i].transform.position, x, y);

            Exy[0] = Exy[0] + E[0];
            Exy[1] = Exy[1] + E[1];
        }

        return Exy;
    }

    private bool isInsideSimulationBox2D(Vector3 position)
    {
        Vector2 particlePos = new Vector2(position.x, position.y);

        float ABAM = Vector2.Dot(joinVector(simulationLimits[0], simulationLimits[1]), joinVector(simulationLimits[0], particlePos));
        float ABAB = Vector2.Dot(joinVector(simulationLimits[0], simulationLimits[1]), joinVector(simulationLimits[0], simulationLimits[1]));
        float BCBM = Vector2.Dot(joinVector(simulationLimits[1], simulationLimits[2]), joinVector(simulationLimits[1], particlePos));
        float BCBC = Vector2.Dot(joinVector(simulationLimits[1], simulationLimits[2]), joinVector(simulationLimits[1], simulationLimits[2]));

        return 0 <= ABAM && ABAM <= ABAB && 0 <= BCBM && BCBM <= BCBC;
    }

    private Vector2 joinVector(Vector2 p1, Vector2 p2)
    {
        return new Vector2(p2.x - p1.x, p2.y - p1.y);
    }

    public NativeArray<Vector3> GetParticlePositions()
    {
        NativeArray<Vector3> positions = new NativeArray<Vector3>(particles.Count, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particles)
        {
            positions[index] = particle.transform.position;
            index++;
        }

        return positions;
    }

    public NativeArray<float> GetChargeValues()
    {
        NativeArray<float> chargesNative = new NativeArray<float>(particles.Count, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particles)
        {
            chargesNative[index] = particles[index].charge;
            index++;
        }

        return chargesNative;
    }

    private List<Vector3> GetLinesPointList(string line)
    {
        string[] points = line.Split("@");

        List<Vector3> pointList = new List<Vector3>();

        foreach (string point in points)
        {
            if(point.Length > 0)
                pointList.Add(GetVecto3FromString(point));
        }

        return pointList;
    }

    private Vector3 GetVecto3FromString(string point)
    {
        string[] values = point.Replace("(", "").Replace(")", "").Split(",");

        float x = float.Parse(values[0]);
        float y = float.Parse(values[1]);
        float z = float.Parse(values[2]);

        return new Vector3(x, y, z);
    }
}

public struct ElectricFieldLines
{
    public ManagedObjectRef<List<Vector3>> lines;

    public ElectricFieldLines(ManagedObjectRef<List<Vector3>> lines)
    {
        this.lines = lines;
    }

}