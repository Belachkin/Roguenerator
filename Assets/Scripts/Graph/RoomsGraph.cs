using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class RoomsGraph : EditorWindow
{
    private RoomsGraphView _graphView;
    private string _fileName = "New Generation";

    [MenuItem("Graph/Rooms Graph")]
    public static void OpenRoomsGraphWindow()
    {
        var window = GetWindow<RoomsGraph>();
        window.titleContent = new GUIContent("Rooms Graph");
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolBar();
    }

    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);
    }

    private void ConstructGraphView()
    {
        _graphView = new RoomsGraphView
        {
            name = "Rooms Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolBar()
    {
        var toolBar = new Toolbar();

        var fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(_fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        toolBar.Add(fileNameTextField);

        toolBar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data"});
        toolBar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data"});

        var nodeCreateButton = new Button(() =>
        {
            _graphView.CreateNode("Room Node");
        });
        nodeCreateButton.text = "Create Node";
        toolBar.Add(nodeCreateButton);

        rootVisualElement.Add(toolBar);
    }

    private void RequestDataOperation(bool save)
    {
        if(string.IsNullOrEmpty(_fileName))
        {
            EditorUtility.DisplayDialog("Invalid file name!", "Please enter a valid file name", "Ok");
        }

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);
        if (save)
            saveUtility.SaveGraph(_fileName);
        else
            saveUtility.LoadGraph(_fileName);
    }
}

public enum RoomsTypes
{
    Entrance = 0,
    Normal = 1,
    Hub = 2,
    Reward = 3,
    BossEntrance = 4,
    Boss = 5,
    Exit = 6,
    Shop = 7,
}