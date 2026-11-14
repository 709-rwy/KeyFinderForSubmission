
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

//迷路の基本単位となる四角い部屋の壁を生成するためのプログラム
#if UNITY_EDITOR
public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private Transform roomCenter;
    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject door;


    [SerializeField] private float roomSize = 20f;
    [SerializeField] private float roomHeight = 5f;
    [SerializeField] private float wallThickness = 0.1f;
    [SerializeField] private int doorBit;

    private Transform parent;


    public void GenerateRoom() {
        parent = new GameObject("structures").transform;
        parent.parent = roomCenter;

        GenerateWalls();

        room room = roomCenter.gameObject.AddComponent<room>();
        room.setDoorBit(doorBit);
    }



    private void GenerateWalls()
    {
        float wallDistance = (roomSize - wallThickness) / 2;
        float wallHeight = roomHeight / 2;
        int doorBit = this.doorBit;
        bool replaceDoor;
        Transform wallTransform;

        Quaternion[] wallRots = { 
            Quaternion.identity,
            Quaternion.Euler(0, 90, 0),
            Quaternion.Euler(0, 180, 0),
            Quaternion.Euler(0, 270, 0)
        };

        foreach (Quaternion wallRot in wallRots)
        {
            replaceDoor = ((doorBit & 1) != 0);
            wallTransform = Instantiate(replaceDoor ? door : wall).transform;
            wallTransform.parent = parent;
            wallTransform.localPosition = (wallHeight * Vector3.up) + (wallDistance * (wallRot * Vector3.forward));
            wallTransform.localRotation = wallRot;
            doorBit >>= 1;
        }
    }
}
#endif
