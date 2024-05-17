using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CellularAutomataMap : MonoBehaviour
{
    // 맵 너비 설정
    public int width, height;

    // 셀룰러오토마타 기법에 적용되는 인트값
    [Range(0,100)]
    public int randomFillPercent;

    // 맵 스무딩 횟수
    public int smoothCount;

    // 벽이 아닌 타일
    [SerializeField]
    private Tile[] road;
    // 벽 룰 타일
    [SerializeField]
    private RuleTile wallRuleTile;
    // 이동가능한 타일 맵
    [SerializeField]
    private Tilemap roadTilemap;
    // 벽 타일 맵
    [SerializeField]
    private Tilemap wallTilemap;

    // 시작, 목표 지점
    Vector2Int startPoint;
    Vector2Int destPoint;

    GameObject startObj;
    GameObject destObj;

    List<GameObject> enemySpawnAreas = new List<GameObject>();

    int[,] map;

    public void GenerateMap()
    {
        Generate();
    }

    void Generate()
    {
        Init();

        RandomFillMap();

        for (int i = 0; i < smoothCount; ++i)
        {
            SmoothMap();
        }
        BrushTileMap();

        // int serachPointCount = 5;
        // bool isSearch = false;
        // for (int i = 0; i < serachPointCount; ++i)
        // {
        //     isSearch = GenerateRandomPoint();
        //     if (isSearch)
        //     {
        //         GenerateArea();
        //         StartCoroutine(CoCheckGeneratedArea());
        //         break;
        //     }
        // }
        // if (!isSearch)
        //     Generate();
    }

    public void GenerateWithBSP(int width, int height, int[,] map)
    {
        this.width = width;
        this.height = height;

        this.map = map;
        RandomFillMapForBSP();

        for (int i = 0; i < smoothCount; ++i)
        {
            SmoothMap();
        }
        BrushTileMap();
    }

    void Init()
    {
        map = new int[width, height];
        Destroy(startObj);
        Destroy(destObj);
        foreach (var area in enemySpawnAreas)
            Destroy(area.gameObject);
        enemySpawnAreas.Clear();
        roadTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    void RandomFillMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 가장자리 벽 채우기
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                    map[x, y] = Random.Range(0, 101) <= randomFillPercent ? 1 : 0;
            }
        }
    }

    public void RandomFillMapForBSP()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 가장자리 벽 채우기
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else if (map[x, y] != 2 && map[x,y] != 3)
                    map[x, y] = Random.Range(0, 101) <= randomFillPercent ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);

                if (map[x, y] == 2 || map[x, y] == 3)
                    continue;

                if (neighborWallTiles > 4)
                    map[x, y] = 1;
                else if (neighborWallTiles < 4)
                    map[x, y] = 0;
            }
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    if (neighborX != gridX || neighborY != gridY)
                        wallCount += map[neighborX, neighborY];
                }
                else
                    wallCount++;
            }
        }

        return wallCount;
    }

    void BrushTileMap()
    {
        for (int x = -3; x < width + 3; ++x)
        {
            wallTilemap.SetTile(new Vector3Int(x, -3, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(x, -2, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(x, -1, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(x, height, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(x, height + 1, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(x, height + 2, 0), wallRuleTile);
        }
        for (int y = -3; y < height + 3; ++y)
        {
            wallTilemap.SetTile(new Vector3Int(-3, y, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(-2, y, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(-1, y, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(width, y, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(width + 1, y, 0), wallRuleTile);
            wallTilemap.SetTile(new Vector3Int(width + 2, y, 0), wallRuleTile);
        }

        for (int x = 0; x < width; ++x)
            for (int y = 0; y < height; ++y)
            {
                if (map[x, y] == 1)
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wallRuleTile);

                roadTilemap.SetTile(new Vector3Int(x, y, 0), road[Random.Range(0, road.Length)]);
            }
    }

    bool GenerateRandomPoint(int findCount = 10)
    {
        // 맵의 좌하단, 우상단 지역에서 두 곳의 비어있는 지역 찾기

        int lx = 0;
        int ly = 0;
        int rx = 0;
        int ry = 0;


        bool lFind = false;
        bool rFind = false;
        for (int i = 0; i < findCount; ++i)
        {
            lx = Random.Range(0, Mathf.CeilToInt(width * 0.25f));
            ly = Random.Range(0, Mathf.CeilToInt(height * 0.25f));
            lFind = FindSurround(lx, ly);
            if (lFind)
                break;
        }
        for (int i = 0; i < findCount; ++i)
        {
            rx = Random.Range(Mathf.CeilToInt(width * 0.75f), width);
            ry = Random.Range(Mathf.CeilToInt(height * 0.75f), height);
            rFind = FindSurround(rx, ry);
            if (rFind)
                break;
        }

        if (lFind && rFind)
        {
            if (Random.Range(0, 2) == 0)
            {
                startPoint = new Vector2Int(lx, ly);
                destPoint = new Vector2Int(rx, ry);
            }
            else
            {
                startPoint = new Vector2Int(rx, ry);
                destPoint = new Vector2Int(lx, ly);
            }

            if (PathFinder.AStarSearch(startPoint, destPoint, map))
            {
                startObj = (GameObject)Instantiate(Resources.Load("Point"), new Vector3(startPoint.x + 0.5f, startPoint.y + 0.5f, 0), Quaternion.identity);
                destObj = (GameObject)Instantiate(Resources.Load("Point"), new Vector3(destPoint.x + 0.5f, destPoint.y + 0.5f, 0), Quaternion.identity);
                return true;
            }
        }
        return false;

    }

    bool FindSurround(int gridX, int gridY)
    {
        // 위치 기준 8방향에 벽이 있는지 확인

        // 상하좌우대각선
        int[] dx = {-1, 0, 1, -1, 0, 1, -1,  0,  1 };
        int[] dy = { 1, 1, 1,  0, 0, 0, -1, -1, -1 };

        for (int i = 0; i < dx.Length; ++i)
        {
            int x = gridX + dx[i];
            int y = gridY + dy[i];
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            if (map[x, y] == 1)
                return false;
        }
        return true;
    }

    void GenerateArea(int areaCount = 50)
    {
        // 맵의 width, height 중 작은 값의 1/10 값으로 에어리어 사이즈 설정
        int areaSize = Mathf.CeilToInt(Mathf.Min(width, height) * 0.05f);

        for (int i = 0; i < areaCount; ++i)
        {
            float x = Random.Range(width * 0.1f, width * 0.9f);
            float y = Random.Range(height * 0.1f, height * 0.9f);
            GameObject obj = Instantiate((GameObject)Resources.Load("EnemySpawnArea"),new Vector3(x,y,0),Quaternion.identity);
            obj.transform.localScale *= areaSize;
            enemySpawnAreas.Add(obj);
        }
    }

    IEnumerator CoCheckGeneratedArea()
    {
        yield return new WaitForSeconds(1f);
        foreach (GameObject obj in enemySpawnAreas)
        {
            obj.GetComponent<SpawnArea>().Check = true;
        }
        yield return new WaitForSeconds(0.5f);
        foreach (GameObject obj in enemySpawnAreas)
        {
            obj.GetComponent<SpawnArea>().Check = false;
        }
    }

    public void RemoveArea(GameObject obj)
    {
        Destroy(obj.gameObject);
        enemySpawnAreas.Remove(obj);
    }
}
