using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneRelation
{
    public List<SceneInfo> scenes;

    public SceneRelation(List<SceneInfo> scenes)
    {
        this.scenes = scenes;
    }

	public string[] GetSampleRelation(int scene, int size)
    {
		return GetRandomArray(scenes[scene].relations.ToArray(), size);

	}
	/// <summary> Returns an array of random unique elements from the specified array. </summary>
	public static T[] GetRandomArray<T>(T[] array, int size)
	{
		List<T> list = new List<T>();
		T element;
		int tries = 0;
		int maxTries = array.Length;

		while (tries < maxTries && list.Count < size)
		{
			element = array[UnityEngine.Random.Range(0, array.Length)];

			if (!list.Contains(element))
			{
				list.Add(element);
			}
			else
			{
				tries++;
			}
		}

		if (list.Count > 0)
		{
			return list.ToArray();
		}
		else
		{
			return null;
		}
	}
}

[System.Serializable]
public class SceneInfo
{
    public string name;
    public List<string> relations;

    public SceneInfo(string name, List<string> relations)
    {
        this.name = name;
        this.relations = relations;
    }
}
