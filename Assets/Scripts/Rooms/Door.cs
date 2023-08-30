using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Door
{
    public Transform Transform;
    public Vector2 direction;
    public bool alreadyUsed = false;
}
