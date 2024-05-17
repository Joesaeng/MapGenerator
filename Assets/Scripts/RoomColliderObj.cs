using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomColliderObj : MonoBehaviour
{
    public int roomNumber;
    public int RoomNumber { get { return roomNumber; } set { roomNumber = value; } }
    public bool isCheck = false;
    public List<RoomColliderObj> neighborRooms = new List<RoomColliderObj>();

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out RoomColliderObj room))
        {
            if (neighborRooms.Contains(room) == false)
            {
                neighborRooms.Add(room);
            }
        }

    }
}
