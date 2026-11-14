using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


//迷路を生成するためのクラス
#if UNITY_EDITOR
public class MazeGenerator : MonoBehaviour
{
    [SerializeField] private Transform maze;
    [SerializeField] private List<room> allRooms;
    //部屋を回転配置したときに床の模様が一致するように迷路全体で一括で生成するためにマテリアルを保持
    [SerializeField] private Material floorMaterial;

    [SerializeField] private int mazeSize = 3;
    [SerializeField] private float roomSize = 20f;
    [SerializeField] private float roomHeight = 5f;
    [SerializeField] float replaceProbability = 0.2f;

    //各座標のセルを一度でも探索したかどうかを記録する
    private bool[,] visitedMap;
    //各セルの開口方向を、下位ビットから+Z、+X、-Z、-Xの順で保持する。
    int[,] roomBitMap;

    public void Start()
    {
        visitedMap = new bool[mazeSize, mazeSize];
        roomBitMap = new int[mazeSize, mazeSize];

    }
    public void GenerateMaze()
    {
        visitedMap = new bool[mazeSize, mazeSize];
        roomBitMap = new int[mazeSize, mazeSize];
        makeBitMap(0, 0);
        deployRooms();
        GenerateFloorAndRoof();
    }

    private void makeBitMap(int x, int z)
    {
        visitedMap[x, z] = true;
        int[] dirs = { 0, 1, 2, 3 };
        Shuffle(dirs);

        foreach (int dir in dirs)
        {
            int nx = x, nz = z;

            switch (dir)
            {
                case 0: nz++; break;
                case 1: nx++; break;
                case 2: nz--; break;
                case 3: nx--; break;
            }

            if (!isInRange(nx, nz)) continue;

            if (!visitedMap[nx, nz] || randomBool())
            {
                roomBitMap[x, z] |= (1 << dir);
                roomBitMap[nx, nz] |= (1 << ((dir + 2) % 4));
                makeBitMap(nx, nz);
            }

        }
    }
    
    private void deployRooms()
    {

        List<room> candidateRooms;
        List<int> candidateRoomsDir;

        System.Random rng = new System.Random();
        int rndIdx = 0;
        int roomdir = 0;
        Transform room;

        for (int x = 0; x < mazeSize; x++)
        {
            for(int z = 0; z < mazeSize; z++)
            {
                candidateRooms = new List<room>();
                candidateRoomsDir = new List<int>();
                Debug.Log($"(x, z) = ({x}, {z}){roomBitMap[x, z]}");

                for (int i = 0; i < allRooms.Count; i++)
                {
                    if (allRooms[i] == null)
                    {
                        Debug.LogError($"allRooms[{i}] が null");
                    }

                    roomdir = allRooms[i].GetMatchRotation(roomBitMap[x, z]);
                    if(roomdir != -1)
                    {
                        candidateRooms.Add(allRooms[i]);
                        candidateRoomsDir.Add(roomdir);
                    }
                }

                if (candidateRooms.Count == 0)
                {
                    Debug.LogError($"一致する部屋がない: ({x}, {z})");
                    continue;
                }

                rndIdx = rng.Next(candidateRooms.Count);

                room = ((GameObject)PrefabUtility.InstantiatePrefab(candidateRooms[rndIdx].gameObject)).transform;
                Undo.RegisterCreatedObjectUndo(room.gameObject, "Create Room Object");

                room.parent = maze;
                room.localPosition = new Vector3(x * roomSize, 0, z * roomSize);
                room.localRotation = Quaternion.Euler(0, 90 * candidateRoomsDir[rndIdx], 0);
            }
        }
    }

    private void GenerateFloorAndRoof()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(floor, "Create Floor Object");
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(roof, "Create Roof Object");

        floor.name = "floor";
        roof.name = "roof";

        Transform floorTF = floor.transform;
        Transform roofTF = roof.transform;

        floorTF.parent = maze;
        roofTF.parent = maze;

        floorTF.localPosition = new Vector3(1, 0, 1) * (roomSize * (mazeSize - 1) / 2f);
        roofTF.localPosition = floorTF.localPosition +  Vector3.up * roomHeight;

        Vector3 floorScale = new Vector3(roomSize * mazeSize, 1, roomSize * mazeSize) / 10;
        floorTF.localScale = floorScale;
        roofTF.localScale = floorScale;

        roofTF.rotation = Quaternion.Euler(180, 0, 0);

        Renderer floorRenderer = floor.GetComponent<Renderer>();
        floorRenderer.material = floorMaterial;
    }

    private void Shuffle(int[] ary)
    {
        System.Random rng = new System.Random();
        int n = ary.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            int tmp = ary[k];
            ary[k] = ary[n];
            ary[n] = tmp;
        }
    }

    private bool isInRange(int x, int z) => x >= 0 && x < mazeSize && z >= 0 && z < mazeSize;

    private bool randomBool()
    {
        System.Random rng = new System.Random();
        int n = rng.Next(100);
        return (n < (int)(100 * replaceProbability));

    }
}

#endif