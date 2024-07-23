using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RelationPair
{
    public List<Pair> pairs;

    public RelationPair(List<Pair> pairs)
    {
        this.pairs = pairs;
    }


    public List<string> GetRelations(int pair)
    {
        List<string> relations = new List<string>();

        foreach (Relation relation in pairs[pair].relations)
        {
            relations.Add(relation.relation);
        }

        return relations;
    }

    public List<Vector3> GetParticleSettings(int pair)
    {
        List<Vector3> settings = new List<Vector3>();

        foreach (Relation relation in pairs[pair].relations)
        {
            settings.Add(relation.setting);
        }

        return settings;
    }

}

[System.Serializable]
public class Pair
{
    public string name;
    public List<Relation> relations;

    public Pair(string name, List<Relation> relations)
    {
        this.name = name;
        this.relations = relations;
    }
}

[System.Serializable]
public class Relation
{
    public string relation;
    public Vector3 setting;

    public Relation(string relation, Vector3 setting)
    {
        this.relation = relation;
        this.setting = setting;
    }
}
