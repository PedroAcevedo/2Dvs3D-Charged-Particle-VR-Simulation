using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System;
using Unity.Collections;

public class MarchingCubes
{
    private MeshFilter meshFilter;       // Mesh for marching cubes visualization
    private List<Particle> particles;
    private int gridSize;
    private Vector3 origin;
    
    int MINX, MAXX, MINY, MAXY, MINZ, MAXZ;     //Boundary values for Marching Cubes
    int nX, nY, nZ;                             //number of cells on each axis for Marching cubes

    Vector4[] points;                           // Vertex on the grid
    float[] pointsCharges;                      // Log of Electric field applied of each point of the grid 
    float[] currentPointsCharge;                // Electric field applied of each point of the grid 
    float[] vibrationMapping;
    float[] vibrationIntervals;

    private float maxCharge = -1.0f;
    private float minCharge = 10000.0f;

    private NativeArray<Vector4> points2;
    private NativeArray<float> currentPointsCharge2;
    private NativeArray<float> pointsCharges2;
    Dictionary<Vector3, int> verticesMap = new Dictionary<Vector3, int>();

    int numTriangles = 0;         //Obtained by Marching Cubes
    float minValueForSingle = 0.35f;

    // Lists for Mesh cosntruction
    List<Vector3> vertices = new List<Vector3>();
    Int32[] triangles;

    private TRIANGLE[] Triangles;
    private Vector3 stepSize;
    private JobHandle handle;

    public MarchingCubes(ref List<Particle> particles, Vector3 origin, int gridSize, int dimension, MeshFilter meshFilter)
    {
        this.particles = particles;
        this.gridSize = gridSize;
        this.meshFilter = meshFilter;
        this.origin = origin;
        this.nX = dimension;
        this.nY = dimension;
        this.nZ = dimension;
    }

    public void Init()
    {
        MINX = -gridSize;
        MAXX = gridSize;
        MINY = -gridSize;
        MAXY = gridSize;
        MINZ = -gridSize;
        MAXZ = gridSize;

        CreateGridParallel();
    }

    // Parallel struture to handle multiple CPU cores for Electric field calculation
    public struct PointInCubeUpdate : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> particles;
        [ReadOnly]
        public NativeArray<float> charges;
        [ReadOnly]
        public Vector3 simulationCenter;
        public NativeArray<Vector4> points;
        public NativeArray<float> currentPointsCharge;
        public NativeArray<float> pointsCharges;

        public void Execute(int index)
        {
            float[] fieldValues = Electromagnetism(points[index]);

            points[index] = new Vector4(points[index].x, points[index].y, points[index].z, fieldValues[0]);
            currentPointsCharge[index] = fieldValues[1];
            pointsCharges[index] = (int)currentPointsCharge[index];

            if (pointsCharges[index] > 100000)
            {
                pointsCharges[index] = 0;
            }

            if (pointsCharges[index] > 500)
            {
                pointsCharges[index] = 500;
            }

            if (pointsCharges[index] < 0.1)
            {
                pointsCharges[index] = 0;
            }

        }

        private  float ElectromagnetismCharge(Vector3 currentPosition)
        {
            Vector3 totalForce = Vector3.zero;

            for (int i = 0; i < particles.Length; i++)
            {
                float electric_magnitude = Utils.ElectricField(particles[i] - simulationCenter, currentPosition, charges[i]);
                totalForce += Utils.GetDirectonalVector(currentPosition, particles[i] - simulationCenter) * electric_magnitude;
            }

            return Mathf.Abs(totalForce.magnitude);
        }

        private float[] Electromagnetism(Vector3 currentPosition)
        {
            float[] totalForce = new float[2];
            Vector3 sumForce = Vector3.zero;

            totalForce[0] = 0.0f;
            totalForce[1] = 0.0f;

            for (int i = 0; i < particles.Length; ++i)
            {
                Vector3 position = particles[i] - simulationCenter;
                float r = Utils.VectorDistance(position, currentPosition);
                float electricfield = Utils.ElectricField(r, charges[i]);

                totalForce[0] += Mathf.Abs(electricfield) * Utils.blendFunction0(r);
                sumForce += Utils.GetDirectonalVector(currentPosition, position) * electricfield;
            }

            totalForce[1] = Mathf.Abs(sumForce.magnitude);

            return totalForce;
        }
    }

    public void RunMarchingCubes()
    {
        Triangles = ExecuteParallel();
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        List<Vector3> verticesList = new List<Vector3>(vertices);
        ClearMeshData();

        for (int i = 0; i < numTriangles; ++i)
        {
            triangles[(i * 3)] = Triangles[i].index[0];
            triangles[(i * 3) + 1] = Triangles[i].index[1];
            triangles[(i * 3) + 2] = Triangles[i].index[2];
        }

        meshFilter.mesh = new()
        {
            vertices = verticesList.ToArray(),
            triangles = triangles,
        };

        meshFilter.mesh.RecalculateNormals();
    }

    void ClearMeshData()
    {
        vertices.Clear();
        verticesMap.Clear();
        triangles = new Int32[numTriangles * 3];
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

    void CreateGrid()
    {
        points = new Vector4[(nX + 1) * (nY + 1) * (nZ + 1)];
        pointsCharges = new float[(nX + 1) * (nY + 1) * (nZ + 1)];
        currentPointsCharge = new float[(nX + 1) * (nY + 1) * (nZ + 1)];

        points2 = new NativeArray<Vector4>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        pointsCharges2 = new NativeArray<float>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        currentPointsCharge2 = new NativeArray<float>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        stepSize = new Vector3((float)(2 * gridSize) / (float)nX, (float)(2 * gridSize) / (float)nY, (float)(2 * gridSize) / (float)nZ);

        int YtimesZ = (nY + 1) * (nZ + 1);    //for extra speed
        for (int i = 0; i < nX + 1; ++i)
        {
            int ni = i * YtimesZ;                       //for speed
            float vertX = MINX + i * stepSize.x;
            for (int j = 0; j < nY + 1; ++j)
            {
                int nj = j * (nZ + 1);             //for speed
                float vertY = MINY + j * stepSize.y;
                for (int k = 0; k < nZ + 1; ++k)
                {
                    Vector4 vert = new Vector4(vertX, vertY, MINZ + k * stepSize.z, 0);

                    int ind = ni + nj + k;

                    points[ind] = vert;
                    points2[ind] = vert;

                    // Instantiate(boxPrefab, new Vector3(vert.x, vert.y, vert.z), Quaternion.identity);

                    if (vert.z > -1 && vert.z < 1 && i == 0 && j == 0)
                    {
                        Debug.Log("Cutting plane at z -> " + vert.z);
                    }

                    currentPointsCharge[ind] = Electromagnetism(new Vector3(vert.x, vert.y, vert.z));
                    currentPointsCharge2[ind] = currentPointsCharge[ind];
                    pointsCharges[ind] = (float)Math.Log10(ElectromagnetismCharge(new Vector3(vert.x, vert.y, vert.z)));
                    pointsCharges2[ind] = pointsCharges[ind];
                }
            }
        }
    }

    void CreateGridParallel()
    {
        points2 = new NativeArray<Vector4>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        pointsCharges2 = new NativeArray<float>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        currentPointsCharge2 = new NativeArray<float>((nX + 1) * (nY + 1) * (nZ + 1), Allocator.TempJob);
        stepSize = new Vector3((float)(2 * gridSize) / (float)nX, (float)(2 * gridSize) / (float)nY, (float)(2 * gridSize) / (float)nZ);

        int YtimesZ = (nY + 1) * (nZ + 1);
        for (int i = 0; i < nX + 1; ++i)
        {
            int ni = i * YtimesZ;
            float vertX = MINX + i * stepSize.x;
            for (int j = 0; j < nY + 1; ++j)
            {
                int nj = j * (nZ + 1);
                float vertY = MINY + j * stepSize.y;
                for (int k = 0; k < nZ + 1; ++k)
                {
                    Vector4 vert = new Vector4(vertX, vertY, MINZ + k * stepSize.z, 0);

                    int ind = ni + nj + k;

                    points2[ind] = vert;

                    currentPointsCharge2[ind] = Electromagnetism(new Vector3(vert.x, vert.y, vert.z));
                    pointsCharges2[ind] = (float)Mathf.Log10(ElectromagnetismCharge(new Vector3(vert.x, vert.y, vert.z)));
                }
            }
        }
    }

    TRIANGLE[] Execute()
    {

        for (int i = 0; i < (nX + 1) * (nY + 1) * (nZ + 1); ++i)
        {
            points[i].w = Electromagnetism(new Vector3(points[i].x, points[i].y, points[i].z));/*(step 3)*/
            currentPointsCharge[i] = ElectromagnetismCharge(new Vector3(points[i].x, points[i].y, points[i].z));
            pointsCharges[i] = (int)currentPointsCharge[i];

            if (pointsCharges[i] > 100000)
            {
                pointsCharges[i] = 0;
            }

            if (pointsCharges[i] > 500)
            {
                pointsCharges[i] = 500;
            }

            if (pointsCharges[i] < 0.1)
            {
                pointsCharges[i] = 0;
            }
        }

        maxCharge = Mathf.Max(pointsCharges);
        minCharge = Mathf.Min(pointsCharges);

        if (maxCharge > 100)
        {
            vibrationIntervals = Utils.linspace(minCharge, 100, 10);
        }

        TRIANGLE[] triangles = new TRIANGLE[3 * nX * nY * nZ];    //this should be enough space, if not change 4 to 5
        numTriangles = 0; int YtimeZ = (nY + 1) * (nZ + 1);

        for (int i = 0; i < nX; ++i)
        {
            for (int j = 0; j < nY; ++j)
            {
                for (int k = 0; k < nZ; ++k)   //z axis
                {
                    //initialize vertices
                    Vector4[] verts = new Vector4[8];
                    int ind = i * YtimeZ + j * (nZ + 1) + k;

                    /*(step 3)*/
                    verts[0] = points[ind];
                    verts[1] = points[ind + YtimeZ];
                    verts[2] = points[ind + YtimeZ + 1];
                    verts[3] = points[ind + 1];
                    verts[4] = points[ind + (nZ + 1)];
                    verts[5] = points[ind + YtimeZ + (nZ + 1)];
                    verts[6] = points[ind + YtimeZ + (nZ + 1) + 1];
                    verts[7] = points[ind + (nZ + 1) + 1];

                    //get the index
                    int cubeIndex = 0;
                    for (int n = 0; n < 8; n++)
                        /*(step 4)*/
                        if (verts[n].w <= minValueForSingle) cubeIndex |= (1 << n);

                    //check if its completely inside or outside
                    /*(step 5)*/
                    if (cubeIndex == 0 || cubeIndex == 255)
                        continue;

                    //get intersection vertices on edges and save into the array    
                    Vector3[] intVerts = new Vector3[12];
                    /*(step 6)*/
                    if ((Utils.edgeTable[cubeIndex] & 1) > 0) intVerts[0] = Utils.intersection(verts[0], verts[1], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 2) > 0) intVerts[1] = Utils.intersection(verts[1], verts[2], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 4) > 0) intVerts[2] = Utils.intersection(verts[2], verts[3], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 8) > 0) intVerts[3] = Utils.intersection(verts[3], verts[0], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 16) > 0) intVerts[4] = Utils.intersection(verts[4], verts[5], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 32) > 0) intVerts[5] = Utils.intersection(verts[5], verts[6], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 64) > 0) intVerts[6] = Utils.intersection(verts[6], verts[7], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 128) > 0) intVerts[7] = Utils.intersection(verts[7], verts[4], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 256) > 0) intVerts[8] = Utils.intersection(verts[0], verts[4], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 512) > 0) intVerts[9] = Utils.intersection(verts[1], verts[5], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 1024) > 0) intVerts[10] = Utils.intersection(verts[2], verts[6], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 2048) > 0) intVerts[11] = Utils.intersection(verts[3], verts[7], minValueForSingle);

                    //now build the triangles using triTable
                    for (int n = 0; Utils.triTable[cubeIndex, n] != -1; n += 3)
                    {
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n + 1]]);
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n]]);
                        vertices.Add(intVerts[Utils.triTable[cubeIndex, n + 2]]);

                        triangles[numTriangles] = new TRIANGLE(new Vector3[] { intVerts[Utils.triTable[cubeIndex, n + 2]], intVerts[Utils.triTable[cubeIndex, n + 1]], intVerts[Utils.triTable[cubeIndex, n]] }, new int[] { vertices.Count - 1, vertices.Count - 3, vertices.Count - 2 }, new Vector3(0, 0, 0));
                        numTriangles++;
                    }
                }
            }
        }

        return triangles;
    }


    TRIANGLE[] ExecuteParallel()
    {
        PointInCubeUpdate jobData = new PointInCubeUpdate();
        jobData.particles = GetParticlePositions();
        jobData.charges = GetChargeValues();
        jobData.simulationCenter = origin;
        jobData.points = points2;
        jobData.currentPointsCharge = currentPointsCharge2;
        jobData.pointsCharges = pointsCharges2;

        // Schedule the job with one Execute per index in the results array and only 1 item per processing batch
        handle = jobData.Schedule(points2.Length, 32);

        handle.Complete();

        //maxCharge = Mathf.Max(pointsCharges2.ToArray());
        //minCharge = Mathf.Min(pointsCharges2.ToArray());

        Debug.Log("The min value: " + minCharge + " and the Max value: " + maxCharge);

        if (maxCharge > 100)
        {
            vibrationIntervals = Utils.linspace(minCharge, 100, 10);
        }

        TRIANGLE[] triangles = new TRIANGLE[3 * nX * nY * nZ];    //this should be enough space, if not change 4 to 5
        numTriangles = 0; 
        int YtimeZ = (nY + 1) * (nZ + 1);

        for (int i = 0; i < nX; ++i)
        {
            for (int j = 0; j < nY; ++j)
            {
                for (int k = 0; k < nZ; ++k)   //z axis
                {
                    //initialize vertices
                    Vector4[] verts = new Vector4[8];
                    int ind = i * YtimeZ + j * (nZ + 1) + k;
                    /*(step 3)*/

                    verts[0] = points2[ind];
                    verts[1] = points2[ind + YtimeZ];
                    verts[2] = points2[ind + YtimeZ + 1];
                    verts[3] = points2[ind + 1];
                    verts[4] = points2[ind + (nZ + 1)];
                    verts[5] = points2[ind + YtimeZ + (nZ + 1)];
                    verts[6] = points2[ind + YtimeZ + (nZ + 1) + 1];
                    verts[7] = points2[ind + (nZ + 1) + 1];

                    //get the index
                    int cubeIndex = 0;
                    for (int n = 0; n < 8; n++)
                        /*(step 4)*/
                        if (verts[n].w <= minValueForSingle) cubeIndex |= (1 << n);

                    //check if its completely inside or outside
                    /*(step 5)*/
                    if (cubeIndex == 0 || cubeIndex == 255)
                        continue;

                    //get intersection vertices on edges and save into the array    
                    Vector3[] intVerts = new Vector3[12];
                    /*(step 6)*/
                    if ((Utils.edgeTable[cubeIndex] & 1) > 0) intVerts[0] = Utils.intersection(verts[0], verts[1], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 2) > 0) intVerts[1] = Utils.intersection(verts[1], verts[2], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 4) > 0) intVerts[2] = Utils.intersection(verts[2], verts[3], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 8) > 0) intVerts[3] = Utils.intersection(verts[3], verts[0], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 16) > 0) intVerts[4] = Utils.intersection(verts[4], verts[5], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 32) > 0) intVerts[5] = Utils.intersection(verts[5], verts[6], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 64) > 0) intVerts[6] = Utils.intersection(verts[6], verts[7], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 128) > 0) intVerts[7] = Utils.intersection(verts[7], verts[4], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 256) > 0) intVerts[8] = Utils.intersection(verts[0], verts[4], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 512) > 0) intVerts[9] = Utils.intersection(verts[1], verts[5], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 1024) > 0) intVerts[10] = Utils.intersection(verts[2], verts[6], minValueForSingle);
                    if ((Utils.edgeTable[cubeIndex] & 2048) > 0) intVerts[11] = Utils.intersection(verts[3], verts[7], minValueForSingle);

                    //now build the triangles using triTable
                    for (int n = 0; Utils.triTable[cubeIndex, n] != -1; n += 3)
                    {
                        Vector3 v0 = intVerts[Utils.triTable[cubeIndex, n + 1]];
                        Vector3 v1 = intVerts[Utils.triTable[cubeIndex, n]];
                        Vector3 v2 = intVerts[Utils.triTable[cubeIndex, n + 2]];

                        vertices.Add(v0);
                        vertices.Add(v1);
                        vertices.Add(v2);

                        int[] indexes = new int[3];

                        for (int h = 0; h < 3; h++)
                        {
                            int currentIndex = vertices.Count - (1 + (2 - h));

                            if (verticesMap.TryGetValue(vertices[currentIndex], out int index))
                            {
                                indexes[h] = index;
                            }
                            else
                            {
                                verticesMap.Add(vertices[currentIndex], currentIndex);
                                indexes[h] = currentIndex;
                            }
                        }

                        triangles[numTriangles] = new TRIANGLE(new Vector3[] { v2, v0, v1 }, indexes, new Vector3(0, 0, 0));
                        numTriangles++;
                    }
                }
            }

        }

        return triangles;
    }

    public float ElectromagnetismCharge(Vector3 currentPosition)
    {
        Vector3 totalForce = Vector3.zero;

        for (int i = 0; i < particles.Count; i++)
        {
            float electric_magnitude = Utils.ElectricField(particles[i].transform.position - origin, currentPosition, particles[i].charge);
            totalForce += Utils.GetDirectonalVector(currentPosition, particles[i].transform.position - origin) * electric_magnitude;
        }

        return Mathf.Abs(totalForce.magnitude);
    }

    private float Electromagnetism(Vector3 currentPosition)
    {
        float totalForce = 0;

        for (int i = 0; i < particles.Count; ++i)
        {
            float r = Utils.VectorDistance(particles[i].transform.position - origin, currentPosition);
            totalForce += Mathf.Abs(Utils.ElectricField(r, particles[i].charge)) * Utils.blendFunction0(r);
        }

        return totalForce;
    }

    NativeArray<Vector3> GetParticlePositions()
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

    NativeArray<float> GetChargeValues()
    {
        NativeArray<float> chargesNative = new NativeArray<float>(particles.Count, Allocator.TempJob);

        int index = 0;

        foreach (Particle particle in particles)
        {
            chargesNative[index] = particle.charge;
            index++;
        }

        return chargesNative;
    }
}

public struct TRIANGLE
{
    public Vector3[] points;
    public int[] index;
    public float charge;

    public TRIANGLE(Vector3[] pointsT, int[] index, Vector3 normalT)
    {
        this.points = pointsT;
        this.index = index;
        this.charge = 0;
    }
}