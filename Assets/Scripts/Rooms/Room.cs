using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    public RoomsTypes RoomType;
    public float Width;
    public float Height;
    public float OffsetY = 0;
    public float OffsetX = 0;
    public Door[] Exits;
}
