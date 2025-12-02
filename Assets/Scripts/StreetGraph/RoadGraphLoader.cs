using UnityEngine;

public class RoadGraphLoader : MonoBehaviour
{
    public TextAsset roadJson;

    public RoadGraphData RawData { get; private set; }
    public RoadGraph Graph { get; private set; }

    public void LoadGraphData()
    {
        if (roadJson == null)
        {
            Debug.LogError("roadJson not assigned");
            return;
        }

        RawData = JsonUtility.FromJson<RoadGraphData>(roadJson.text);
        Graph = new RoadGraph(RawData);

        Debug.Log($"Loaded {Graph.Nodes.Count} nodes, {RawData.edges.Length} edges.");
    }
}
