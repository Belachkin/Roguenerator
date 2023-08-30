using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

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
    [SerializeField] private GameObject _doorPref;
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
        Vector2[] directions =
        {
            Vector2.down, Vector2.up,
            Vector2.right, Vector2.left,
        };

        Transform segmentParent = Instantiate(new GameObject()).transform;
        _segmentsParents.Add(segmentParent);

        Vector2 direction = directions[Random.Range(0, directions.Length)];
        Room mainRoom = null;
        Door mainDoor = null;

        foreach (var item in segment.Value)
        {
            List<Room> tempRooms = _rooms.Where(x => x.RoomType == item).ToList();
            Room choosenRoom = tempRooms[Random.Range(0, tempRooms.Count)];
            Room spawnedRoom = Instantiate(choosenRoom);
            spawnedRoom.transform.parent = segmentParent;

            Door door = spawnedRoom.Exits.First(x => x.direction == direction);

            if (mainRoom == null)
            {
                mainRoom = spawnedRoom;
                mainDoor = door;
                continue;
            }

            Door connectedDoor = spawnedRoom.Exits.First(y => y.direction == -direction);
            
            Vector3 offset = (Vector3)door.direction * (2 * _roomsSaveZone
                + ((door.direction.x == 0) ? mainRoom.Height / 2 + spawnedRoom.Height / 2
                : mainRoom.Width / 2 + spawnedRoom.Width / 2));

            Vector3 diff = door.Transform.localPosition - connectedDoor.Transform.localPosition;
            offset += (Vector3)new Vector2(Mathf.Abs(diff.x * door.direction.y), Mathf.Abs(diff.y * door.direction.x));

            spawnedRoom.transform.position = mainRoom.transform.position + offset;

            if(segment.Value.IndexOf(item) == segment.Value.Count - 1)
            {
                break;
            }

            ConnectDoors(mainDoor, connectedDoor);

            mainRoom = spawnedRoom;
            mainDoor = door;
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

        Vector2 direction = directions[Random.Range(0, directions.Length)];

        int centerRoom = Mathf.RoundToInt(roomsTypes.Count / 2f);

        Door selectedDoor = spawnedRoom.Exits.First(x => x.direction == direction);
        selectedDoor.alreadyUsed = true;

        Room firstRoomToMerge = GenerateBranch(spawnedRoom, selectedDoor,
            roomsTypes.GetRange(1, centerRoom - 1), direction, segmentParent);

        selectedDoor = spawnedRoom.Exits.FirstOrDefault(x => x.direction == direction && x.alreadyUsed == false);
        if(selectedDoor == null)
        {
            selectedDoor = spawnedRoom.Exits.First(x => (Vector3)x.direction == Quaternion.Euler(0, 0, 90) * direction);
        }
        selectedDoor.alreadyUsed = true;

        Room secondRoomToMerge = GenerateBranch(spawnedRoom, selectedDoor,
            roomsTypes.GetRange(centerRoom + 1, roomsTypes.Count - centerRoom - 1),
            direction, segmentParent);

        MergeRooms(firstRoomToMerge, secondRoomToMerge, roomsTypes[centerRoom], direction, segmentParent);
    }

    private void MergeRooms(Room firstRoomToMerge, Room secondRoomToMerge, RoomsTypes centerRoom, Vector2 direction, Transform segmentParent)
    {
        List<Room> tempRooms = _rooms.Where(x => x.RoomType == centerRoom).ToList();
        Room choosenRoom = tempRooms[Random.Range(0, tempRooms.Count)];

        Room newRoom = Instantiate(choosenRoom, segmentParent);

        Door firstConnectedDoor = newRoom.Exits.First(x => (Vector3)x.direction == Quaternion.Euler(0, 0, -90) * direction);
        firstConnectedDoor.alreadyUsed = true;
        Door secondConnectedDoor = newRoom.Exits.First(x => x.direction == -direction);
        secondConnectedDoor.alreadyUsed = true;

        Door firstMergableDoor = firstRoomToMerge.Exits.FirstOrDefault(x => (Vector3)x.direction == Quaternion.Euler(0, 0, 90) * direction && x.alreadyUsed == false);
        if(firstMergableDoor == null)
        {
            firstMergableDoor = firstRoomToMerge.Exits.First(x => x.direction == direction);
        }
        Door secondMergableDoor = secondRoomToMerge.Exits.FirstOrDefault(x => x.direction == direction && x.alreadyUsed == false);
        if (secondMergableDoor == null)
        {
            secondMergableDoor = secondRoomToMerge.Exits.First(x => (Vector3)x.direction == Quaternion.Euler(0, 0, 90) * direction);
        }

        Vector3 offset = (Vector3)firstMergableDoor.direction * (2 * _roomsSaveZone
                + ((firstMergableDoor.direction.x == 0) ? firstRoomToMerge.Height / 2 + newRoom.Height / 2
                : firstRoomToMerge.Width / 2 + newRoom.Width / 2));

        Vector3 diff = firstMergableDoor.Transform.localPosition - firstConnectedDoor.Transform.localPosition;
        offset += (Vector3)new Vector2(Mathf.Abs(diff.x * firstMergableDoor.direction.y), Mathf.Abs(diff.y * firstMergableDoor.direction.x));

        newRoom.transform.position = firstRoomToMerge.transform.position + offset;

        ConnectDoors(firstConnectedDoor, firstMergableDoor);
        ConnectDoors(secondConnectedDoor, secondMergableDoor);
    }

    private Room GenerateBranch(Room mainRoom, Door mainDoor, List<RoomsTypes> rooms, Vector2 direction, Transform parent)
    {
        foreach (RoomsTypes room in rooms)
        {
            List<Room> tempRooms = _rooms.Where(x => x.RoomType == room).ToList();
            Room choosenRoom = Instantiate(tempRooms[Random.Range(0, tempRooms.Count)], parent);

            Door connectedDoor = choosenRoom.Exits.First(x => x.direction == -mainDoor.direction);
            connectedDoor.alreadyUsed = true;

            Vector3 offset = (Vector3)mainDoor.direction * (2 * _roomsSaveZone
                + ((mainDoor.direction.x == 0) ? mainRoom.Height/2f + choosenRoom.Height/2f
                : mainRoom.Width / 2f + choosenRoom.Width / 2f));

            Vector3 diff = mainDoor.Transform.localPosition - connectedDoor.Transform.localPosition;
            offset = offset + (Vector3) new Vector2(Mathf.Abs( diff.x * mainDoor.direction.y), Mathf.Abs(diff.y * mainDoor.direction.x));

            choosenRoom.transform.position = mainRoom.transform.position + offset;
            mainRoom = choosenRoom;

            ConnectDoors(mainDoor, connectedDoor);

            mainDoor = mainRoom.Exits.First(x => x.direction == direction);
        }
        return mainRoom;
    }

    private void ConnectDoors(Door mainDoor, Door connectedDoor)
    {
        Instantiate(_doorPref, mainDoor.Transform);
        Instantiate(_doorPref, connectedDoor.Transform);
    }
}
