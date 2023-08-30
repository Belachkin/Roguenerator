using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomsGraphView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(300, 200);

    public RoomsGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(GenerateEntryPointNode());
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach(port =>
        {
            if(startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });

        return compatiblePorts;
    }

    private Port GeneratePort(RoomsNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    private RoomsNode GenerateEntryPointNode()
    {
        var node = new RoomsNode
        {
            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            RoomType = RoomsTypes.Entrance,
            EntryPoint = true
        };

        var generatedPort = GeneratePort(node, Direction.Output);
        generatedPort.portName = "Next";
        node.outputContainer.Add(generatedPort);

        var enumField = new EnumField(RoomsTypes.Entrance);
        enumField.RegisterValueChangedCallback(evt =>
        {
            node.RoomType = (RoomsTypes)evt.newValue;
        });

        node.mainContainer.Add(enumField);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(100, 200, 100, 150));
        return node;
    }

    public void CreateNode(string name)
    {
        AddElement(CreateRoomNode(name));
    }

    public RoomsNode CreateRoomNode(string nodeName, RoomsTypes roomType = RoomsTypes.Normal)
    {
        var roomNode = new RoomsNode { 
            title = nodeName,
            RoomType = roomType,
            GUID = Guid.NewGuid().ToString(),
        };

        var inputPort = GeneratePort(roomNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        roomNode.inputContainer.Add(inputPort);

        var button = new Button(() => { AddChoicePort(roomNode); });
        button.text = "Ways";
        roomNode.titleContainer.Add(button);

        var enumField = new EnumField(roomType);
        enumField.RegisterValueChangedCallback(evt =>
        {
            roomNode.RoomType = (RoomsTypes)evt.newValue;
        });

        roomNode.mainContainer.Add(enumField);

        roomNode.RefreshExpandedState();
        roomNode.RefreshPorts();
        roomNode.SetPosition(new Rect(Vector2.zero, defaultNodeSize));

        return roomNode;

    }

    public void AddChoicePort(RoomsNode roomNode, string overridingPortName = "")
    {
        var generatedPort = GeneratePort(roomNode, Direction.Output);

        //var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        //generatedPort.contentContainer.Remove(oldLabel);

        var outputPortCount = roomNode.outputContainer.Query("connector").ToList().Count;

        var choicePortName = string.IsNullOrEmpty(overridingPortName) ? $"Way" : overridingPortName;

        /*var textField = new TextField
        {
            name = string.Empty,
            value = choicePortName
        };
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);*/

        //generatedPort.contentContainer.Add(new Label(" "));
        //generatedPort.contentContainer.Add(textField);

        var deleteButton = new Button(() => RemovePort(roomNode, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);

        generatedPort.portName = choicePortName;
        roomNode.outputContainer.Add(generatedPort);
        roomNode.RefreshPorts();
        roomNode.RefreshExpandedState();
    }

    private void RemovePort(RoomsNode roomNode, Port generatedPort)
    {
        var target = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);

        if(!target.Any()) { return; }

        var edge = target.First();
        edge.input.Disconnect(edge);
        RemoveElement(target.First());

        roomNode.outputContainer.Remove(generatedPort);
        roomNode.RefreshPorts();
        roomNode.RefreshExpandedState();
    }
}
