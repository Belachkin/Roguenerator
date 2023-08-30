using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private RoomsGraphView _targetGraphView;
    private RoomsContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<RoomsNode> Nodes => _targetGraphView.nodes.ToList().Cast<RoomsNode>().ToList();

    public static GraphSaveUtility GetInstance(RoomsGraphView roomsGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = roomsGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        if(!Edges.Any()) return;

        var roomsContainer = ScriptableObject.CreateInstance<RoomsContainer>();

        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();

        for (int i = 0; i < connectedPorts.Length; i++)
        {
            var outputNode = connectedPorts[i].output.node as RoomsNode;
            var inputNode = connectedPorts[i].input.node as RoomsNode;

            roomsContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = connectedPorts[i].output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        foreach(var roomsNode in Nodes/*.Where(node => !node.EntryPoint)*/)
        {
            roomsContainer.RoomsNodeData.Add(new RoomsNodeData
            {
                NodeGUID = roomsNode.GUID,
                NodeName = roomsNode.title,
                RoomType = roomsNode.RoomType,
                Position = roomsNode.GetPosition().position,
            });
        }

        AssetDatabase.CreateAsset(roomsContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<RoomsContainer>(fileName);

        if( _containerCache == null )
        {
            EditorUtility.DisplayDialog("File Not Found", "Проверь файлы, додик", "Ok");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
    }

    private void ConnectNodes()
    {
        for(var i = 0; i < Nodes.Count; i++)
        {
            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();
            for(var j = 0; j < connections.Count; j++)
            {
                var targetNodeGUID = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGUID);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(_containerCache.RoomsNodeData.First(x => x.NodeGUID == targetNodeGUID).Position,
                    _targetGraphView.defaultNodeSize));
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input,
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);

        _targetGraphView.Add(tempEdge);
    }

    private void ClearGraph()
    {/*
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.NodeLinks[0].BaseNodeGuid;
*/
        foreach(var node in Nodes)
        {/*
            if (node.EntryPoint) continue;
*/
            Edges.Where(x => x.input.node == node).ToList()
                .ForEach(edge => _targetGraphView.RemoveElement(edge));

            _targetGraphView.RemoveElement(node);
        }
    }

    private void CreateNodes()
    {
        foreach(var nodeData in _containerCache.RoomsNodeData)
        {
            var tempNode = _targetGraphView.CreateRoomNode(nodeData.NodeName, nodeData.RoomType);
            tempNode.GUID = nodeData.NodeGUID;
            _targetGraphView.AddElement(tempNode);

            var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.NodeGUID).ToList();
            nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.PortName));
        }
    }
}
