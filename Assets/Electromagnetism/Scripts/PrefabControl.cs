using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class PrefabControl : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            SaveToPrefab();
        }
    }

    public void SaveToPrefab()
    {
        var indentifier = new System.DateTimeOffset(System.DateTime.Now).ToUnixTimeSeconds();

        var meshFilter = this.gameObject.GetComponent<MeshFilter>();
        AssetDatabase.CreateAsset(meshFilter.sharedMesh, "Assets/Resources/SimulationInstances/MeshFilters/meshFilter_" + indentifier + ".asset");
        AssetDatabase.SaveAssets();

        PrefabUtility.SaveAsPrefabAsset(GameObject.Find("3DSimulationBox"), "Assets/Resources/SimulationInstances/particleSetting_" + indentifier + ".prefab");
    }
}
