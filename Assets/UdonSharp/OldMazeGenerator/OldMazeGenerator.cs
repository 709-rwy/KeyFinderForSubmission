using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//現在は使用していない古い迷路を生成するためのプログラム
//アルゴリズムは基本的にMazeGeneratorと同じである
//MazeGeneratorから先に確認することを推奨する
#if UNITY_EDITOR
public class OldMazeGenerator : MonoBehaviour
{

    [SerializeField] float roomSize = 10f;
    [SerializeField] float wallThickness = 0.1f;
    [SerializeField] float wallHeight = 5f;
    [SerializeField] int mazeSize = 10;
    [SerializeField] float replaceProbability = 0.2f;

    [SerializeField] Transform startPoint;

    //現在の迷路の生成は部屋のプレハブを生成して迷路を生成するのにたいして　
    //こちらはその場で壁を生成しながら生成する
    //その違いにより部屋を区切るinnerWallと迷路全体を囲うように配置されるouterWallが存在して、それぞれを個別に管理している
    Transform innerWalls;
    [SerializeField] GameObject innerWall;

    Transform outerWalls;
    [SerializeField] GameObject outerWall;

    //doorという名前だが、正確には人が通れる開口部を持つ壁を表す。
    Transform doors;
    [SerializeField] GameObject door;

    Transform roomCenters;

    bool[,] visitedMap;
    //部屋が存在しないため、壁を縦向き/横向きのもので分けて、それぞれについて生成済みかを管理している
    bool[,] XAxisDirectionWallMap;
    bool[,] ZAxisDirectionWallMap;

    public void GenerateMaze()
    {
        GameObject innerWalls = new GameObject("innerWalls");
        innerWalls.transform.position = startPoint.position;
        innerWalls.transform.parent = startPoint;
        this.innerWalls = innerWalls.transform;

        GameObject outerWalls = new GameObject("innerWalls");
        outerWalls.transform.position = startPoint.position;
        outerWalls.transform.parent = startPoint;
        this.outerWalls = outerWalls.transform;

        GameObject doors = new GameObject("doors");
        doors.transform.position = startPoint.position;
        doors.transform.parent = startPoint;
        this.doors = doors.transform;

        GameObject roomCenters = new GameObject("roomCenters");
        roomCenters.transform.position = startPoint.position;
        roomCenters.transform.parent = startPoint;
        this.roomCenters = roomCenters.transform;

        visitedMap = new bool[mazeSize, mazeSize];
        XAxisDirectionWallMap = new bool[mazeSize + 1, mazeSize];
        ZAxisDirectionWallMap = new bool[mazeSize, mazeSize + 1];

        createFloorAndRoof();
        GenerateMazeWalls(0, 0);
    }

    private void GenerateMazeWalls(int x, int z)
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

            bool dirIsZAxis = (dir % 2 == 0);

            if (isInRange(nx, nz) && !visitedMap[nx, nz])
            {
                Instantiate(door, wallPos(x, nx, z, nz), wallRot(dir), doors);
                createRoomCenter(nx, nz);

                if (dirIsZAxis)
                {
                    ZAxisDirectionWallMap[x, (z + nz + 1) / 2] = true;
                }
                else
                {
                    XAxisDirectionWallMap[(x + nx + 1) / 2, z] = true;
                }
                GenerateMazeWalls(nx, nz);
            }
            else
            {
                if (!(dirIsZAxis && ZAxisDirectionWallMap[x, (z + nz + 1) / 2]) && !(!dirIsZAxis && XAxisDirectionWallMap[(x + nx + 1) / 2, z]))
                {
                    if (isInRange(nx, nz))
                    {
                        bool replaceWall = randomBool();
                        Instantiate(replaceWall ? door : innerWall, wallPos(x, nx, z, nz), wallRot(dir), replaceWall ? doors : innerWalls);
                    }
                    else
                    {
                        Instantiate(outerWall, wallPos(x, nx, z, nz), wallRot(dir), outerWalls);
                    }

                    if (dirIsZAxis)
                    {
                        ZAxisDirectionWallMap[x, (z + nz + 1) / 2] = true;
                    }
                    else
                    {
                        XAxisDirectionWallMap[(x + nx + 1) / 2, z] = true;
                    }
                }
            }
        }
    }

    private void createFloorAndRoof()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Plane);

        floor.name = "floor";
        roof.name = "roof";

        Vector3 floorSize = new Vector3(1, 1, 1) * ((mazeSize * roomSize) + ((mazeSize + 1) * wallThickness)) / 10;
        floor.transform.localScale = floorSize;
        roof.transform.localScale = floorSize;

        Vector3 floorPosition = (new Vector3(1, 0, 1) * (roomSize + wallThickness) * (mazeSize - 1) / 2) + startPoint.position;
        floor.transform.position = floorPosition;
        roof.transform.position = floorPosition + new Vector3(0, wallHeight, 0);

        floor.transform.parent = startPoint;
        roof.transform.parent = startPoint;
    }

    private bool isInRange(int x, int z) => x >= 0 && x < mazeSize && z >= 0 && z < mazeSize;



    private Vector3 wallPos(int x, int nx, int z, int nz)
    {
        return new Vector3(
            (roomSize + wallThickness) * (x + nx) / 2f,
            wallHeight / 2f,
            (roomSize + wallThickness) * (z + nz) / 2f
         )
            + startPoint.position;
    }

    private Quaternion wallRot(int dir)
    {
        return Quaternion.Euler(0, 90 * dir, 0);
    }

    private void createRoomCenter(int x, int z)
    {
        GameObject roomCenter = new GameObject($"roomCenters{x},{z}");
        roomCenter.transform.position = roomPos(x, z);
        roomCenter.transform.SetParent(roomCenters);
    }

    private Vector3 roomPos(int x, int z)
    {
        return (new Vector3(x, 0, z) * (roomSize + wallThickness)) + startPoint.position;
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

    private bool randomBool()
    {
        System.Random rng = new System.Random();
        int n = rng.Next(100);
        return (n < (int)(100 * replaceProbability));

    }
}
#endif
