using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomsContainer : ScriptableObject
{
    public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
    public List<RoomsNodeData> RoomsNodeData = new List<RoomsNodeData>();
}
