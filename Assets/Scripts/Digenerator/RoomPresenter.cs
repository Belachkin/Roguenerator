using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomPresenter : MonoBehaviour
{
    [SerializeField] private RoomsContainer[] _roomsContainer;

    //Работа с графами
    private Dictionary<string, RoomsTypes> _roomsNodes = new Dictionary<string, RoomsTypes>();
    private Dictionary<int, List<RoomsTypes>> _segments = new Dictionary<int, List<RoomsTypes>>();
    private List<bool> _isLoop = new List<bool>();
    private List<string> _roomsName = new List<string>();
    private List<string> _transRooms = new List<string>();

    private List<string> _previousRooms = new List<string>();
    private List<RoomsTypes> _previousRoomsTypes = new List<RoomsTypes>();
    private string _baseRoom;
    private bool alreadySegmented;
    int index = 0;

    private RoomsContainer _selectedContainer;

    //Генерация сегментов
    [Header("Rooms")]
    [SerializeField] private List<Room> _rooms = new List<Room>();
    [SerializeField] private float _roomsSaveZone = 1f;
    private List<Transform> _segmentsParents = new List<Transform>();

    private void Awake()
    {
        _selectedContainer = _roomsContainer[Random.Range(0, _roomsContainer.Length)];
        foreach(var room in _selectedContainer.RoomsNodeData)
        {
            _roomsNodes.Add(room.NodeGUID, room.RoomType);
            _roomsName.Add(room.NodeGUID);
        }
        foreach (var translation in _selectedContainer.NodeLinks)
        {
            _transRooms.Add(translation.BaseNodeGuid);
        }

        _baseRoom = _roomsName[0];
        AroundTheGraph(_roomsName[0]);

        SegmentsGeneration();
    }

    private void SegmentsGeneration()
    {
        foreach (var segment in _segments)
        {
            if (_isLoop[segment.Key])
                GenerateLoopSegment(segment);
            else
                GenerateSegment(segment);
        }
    }

    private void AroundTheGraph(string vertexName)
    {
        bool alreadyLoop = false;
        foreach (var translation in _selectedContainer.NodeLinks)
        {
            if(translation.BaseNodeGuid == vertexName)
            {
                if (!alreadyLoop)
                {
                    _previousRooms.Add(translation.BaseNodeGuid);
                    _previousRoomsTypes.Add(_selectedContainer.RoomsNodeData
                        .Find(x => x.NodeGUID == translation.BaseNodeGuid).RoomType);
                }

                if (translation.TargetNodeGuid == _baseRoom)
                {
                    AddSegment(_previousRoomsTypes, true);

                    alreadySegmented = true;
                    _previousRoomsTypes.Clear();
                    _previousRooms.Clear();
                    continue;
                }

                if (_previousRooms.Contains(translation.TargetNodeGuid))
                {
                    _previousRooms.Clear();
                    continue;
                }

                if (!_transRooms.Contains(translation.TargetNodeGuid))
                {
                    _previousRoomsTypes.Add(_selectedContainer.RoomsNodeData
                        .Find(x => x.NodeGUID == translation.TargetNodeGuid).RoomType);

                    AddSegment(_previousRoomsTypes, false);

                    alreadySegmented = true;
                    _previousRoomsTypes.Clear();
                    _previousRooms.Clear();
                    continue;
                }

                if (alreadySegmented)
                {
                    _baseRoom = translation.TargetNodeGuid;
                    alreadySegmented = false;
                }

                AroundTheGraph(translation.TargetNodeGuid);

                alreadyLoop = true;
            }
        }
    }

    private void AddSegment(List<RoomsTypes> roomsTypes,bool isLoop)
    {
        _segments.Add(index, roomsTypes.ToList());
        _isLoop.Add(isLoop);
        index++;
    }

    private void GenerateSegment(KeyValuePair<int, List<RoomsTypes>> segment)
    {
        Transform segmentParent = Instantiate(new GameObject()).transform;
        _segmentsParents.Add(segmentParent);
        
        foreach (var item in segment.Value)
        {
            List<Room> tempRooms = _rooms.Where(x => x.RoomType == item).ToList();


        }
    }

    private void GenerateLoopSegment(KeyValuePair<int, List<RoomsTypes>> segment)
    {
        Vector2[] directions =
        {
            Vector2.down, Vector2.up,
            Vector2.right, Vector2.left,
        };

        List<RoomsTypes> roomsTypes = segment.Value;

        Transform segmentParent = Instantiate(new GameObject()).transform;
        _segmentsParents.Add(segmentParent);

        List<Room> tempRooms = _rooms.Where(x => x.RoomType == roomsTypes[0]).ToList();
        Room choosenRoom = tempRooms[Random.Range(0, tempRooms.Count)];

        Room spawnedRoom = Instantiate(choosenRoom);
        spawnedRoom.transform.parent = segmentParent;

        Vector2 _direction = directions[Random.Range(0, directions.Length)];

        int centerRoom = Mathf.RoundToInt(roomsTypes.Count / 2f);

        Door selectedDoor = spawnedRoom.Exits.First(x => x.direction == _direction);
        selectedDoor.alreadyUsed = true;

        GenerateBranch(spawnedRoom, selectedDoor,
            roomsTypes.GetRange(1, centerRoom - 1), _direction, segmentParent);

        selectedDoor = spawnedRoom.Exits.FirstOrDefault(x => x.direction == _direction && x.alreadyUsed == false);
        if(selectedDoor == null)
        {
            selectedDoor = spawnedRoom.Exits.First(x => (Vector3)x.direction == Quaternion.Euler(0, 0, 90) * _direction);
        }
        selectedDoor.alreadyUsed = true;

        GenerateBranch(spawnedRoom, selectedDoor,
            roomsTypes.GetRange(centerRoom + 1, roomsTypes.Count - centerRoom - 1),
            _direction, segmentParent);
    }

    private void GenerateBranch(Room mainRoom, Door mainDoor, List<RoomsTypes> rooms, Vector2 direction, Transform parent)
    {
        foreach (RoomsTypes room in rooms)
        {
            List<Room> tempRooms = _rooms.Where(x => x.RoomType == room).ToList();
            Room choosenRoom = tempRooms[Random.Range(0, tempRooms.Count)];

            Vector3 offset = (Vector3)mainDoor.direction * (2 * _roomsSaveZone
                + ((mainDoor.direction.x == 0) ? mainRoom.Height/2 + choosenRoom.Height/2 
                : mainRoom.Width / 2 + choosenRoom.Width / 2));

            offset += new Vector3();

            mainRoom = Instantiate(choosenRoom, mainRoom.transform.position
                + offset, Quaternion.identity);
            mainRoom.transform.parent = parent;

            Door connectedDoor = choosenRoom.Exits.First(x => x.direction == -mainDoor.direction);
            connectedDoor.alreadyUsed = true;
            ConnectDoors(mainDoor, connectedDoor);

            mainDoor = mainRoom.Exits.First(x => x.direction == direction);
        }
    }

    private void ConnectDoors(Door mainDoor, Door connectedDoor)
    {
        
    }
}
