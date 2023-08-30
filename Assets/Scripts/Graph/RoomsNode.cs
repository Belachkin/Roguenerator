using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RoomsNode : Node
{
    public string GUID;

    public RoomsTypes RoomType;

    public bool EntryPoint = false;
}
