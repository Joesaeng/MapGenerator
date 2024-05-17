using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Node
{
    public Node pNode;

    public Node lNode;
    public Node rNode;

    public Vector2Int bLeft,tRight;

    public bool isDivided;

    public int depth = 0;

    public bool dir; // true : 세로, false : 가로

    public Vector2Int roomBl, roomTr;
    public Vector2Int divideLineT, divideLineB;

    public Node() { }
    public Node(Vector2Int bl, Vector2Int tr)
    {
        bLeft = bl;
        tRight = tr;

        isDivided = false;
    }

    public void SetDir()
    {
        if (tRight.x - bLeft.x > tRight.y - bLeft.y)
            dir = true;
        else if (tRight.x - bLeft.x < tRight.y - bLeft.y)
            dir = false;
        else
        {
            int rand = Random.Range(0,2);
            if (rand == 0)
                dir = true;
            else
                dir = false;
        }
    }

    public bool DivideNode(int ratio, int minSize)
    {
        float divideRatio = ratio * 0.01f;

        if (dir)
        {
            int width = Mathf.RoundToInt((tRight.x - bLeft.x) * divideRatio);
            if (width < minSize || tRight.x - bLeft.x - width < minSize || tRight.y - bLeft.y < minSize)
            {
                return false;
            }
            divideLineT = new Vector2Int(bLeft.x + width, tRight.y);
            divideLineB = new Vector2Int(bLeft.x + width, bLeft.y);
        }
        else
        {
            int height = Mathf.RoundToInt((tRight.y - bLeft.y) * divideRatio);
            if (height < minSize || tRight.y - bLeft.y - height < minSize || tRight.x - bLeft.x < minSize)
            {
                return false;
            }
            divideLineT = new Vector2Int(tRight.x, bLeft.y + height);
            divideLineB = new Vector2Int(bLeft.x, bLeft.y + height);
        }

        lNode = new Node(bLeft, divideLineT);
        rNode = new Node(divideLineB, tRight);
        lNode.pNode = rNode.pNode = this;
        isDivided = true;
        return true;
    }

    public bool CreateRoom(float bleftRatio, float tRightRatio)
    {
        int distanceFrom = 2;
        int minRoomWidthAndHeight = 2;
        if (isDivided == false)
        {
            int blX = Random.Range(bLeft.x + distanceFrom, Mathf.RoundToInt(bLeft.x + ((tRight.x - bLeft.x) * bleftRatio)));
            if (tRight.x - blX < minRoomWidthAndHeight)
                blX = Random.Range(bLeft.x + distanceFrom, Mathf.RoundToInt(bLeft.x + ((tRight.x - bLeft.x) * bleftRatio)));

            int blY = Random.Range(bLeft.y + distanceFrom, Mathf.RoundToInt(bLeft.y + ((tRight.y - bLeft.y) * bleftRatio)));
            if (tRight.y - blY < minRoomWidthAndHeight)
                blY = Random.Range(bLeft.x + distanceFrom, Mathf.RoundToInt(bLeft.y + ((tRight.y - bLeft.y) * bleftRatio)));
            roomBl = new Vector2Int(blX, blY);

            int trX = Random.Range(Mathf.RoundToInt(bLeft.x + (tRight.x - bLeft.x) * tRightRatio),tRight.x - distanceFrom);
            if(trX - blX < minRoomWidthAndHeight)
                trX = Random.Range(Mathf.RoundToInt(bLeft.x + (tRight.x - bLeft.x) * tRightRatio), tRight.x - distanceFrom);

            int trY = Random.Range(Mathf.RoundToInt(bLeft.y + (tRight.y - bLeft.y) * tRightRatio),tRight.y - distanceFrom);
            if(trY - blY < minRoomWidthAndHeight)
                trY = Random.Range(Mathf.RoundToInt(bLeft.y + (tRight.y - bLeft.y) * tRightRatio), tRight.y - distanceFrom);

            roomTr = new Vector2Int(trX, trY);
            return true;
        }
        return false;
    }
}

public class Room
{
    public int roomNumber;

    public Vector2Int bLeft, tRight;
    public Vector2 centerPos;

    // 연결된 방들<방 번호, 거리>
    public Dictionary<int,float> linkedRooms = new();
}

public struct RoomPair
{
    public Room first;
    public Room second;

    public RoomPair(Room f, Room s)
    {
        first = f;
        second = s;
    }
}

public class BSPCellular : MonoBehaviour
{
    // TEMP
    public Tile untouchableRoad;
    public Tile untouchableWall;
    public Tile wall;
    public Tile road;

    [SerializeField]
    private Tilemap roadTilemap;
    [SerializeField]
    private Tilemap wallTilemap;
    //

    [SerializeField]
    private RuleTile wallRuleTile;

    [Range(0, 100)]
    public int randomFillPercent;

    int[,] map;
    // 0,1:Wall,2:Road,3:Room,4:unTWall

    public int width, height;
    [Range(0f,0.5f)]
    public float bLeftRatio;
    [Range(0.5f,1f)]
    public float tRightRatio;

    List<Node> nodes;
    List<Node> leafNodes;

    List<RoomColliderObj> roomObjs;
    List<Room> rooms;
    List<int> roomParent;

    List<KeyValuePair<float,RoomPair>> edges;
    List<RoomPair> mstRooms;

    int roomNumber = 0;

    public int maxDepth;
    public int minRoomSize;

    public void GenerateMap()
    {
        Init();

        Vector2Int bl = new Vector2Int(0,0);
        Vector2Int tr = new Vector2Int(width,height);
        Node root = new Node(bl,tr);

        nodes.Add(root);
        MakeNodes(ref root, 0);
        MakeRooms();

        // 충돌검사를 진행하기 위해 코루틴으로 실행
        StartCoroutine(CoMakeMST());
    }

    private void Init()
    {
        map = new int[width, height];

        if (nodes == null)
            nodes = new List<Node>();
        nodes.Clear();

        if (leafNodes == null)
            leafNodes = new List<Node>();
        leafNodes.Clear();

        if (roomObjs == null)
            roomObjs = new List<RoomColliderObj>();
        foreach (RoomColliderObj room in roomObjs)
            Destroy(room.gameObject);
        roomObjs.Clear();

        if (rooms == null)
            rooms = new();
        rooms.Clear();

        if (roomParent == null)
            roomParent = new();
        roomParent.Clear();

        if (edges == null)
            edges = new();
        edges.Clear();

        if (mstRooms == null)
            mstRooms = new();
        mstRooms.Clear();

        roomNumber = 0;


    }

    #region Union_Find
    int FindRoot(int n)
    {
        if (roomParent[n] == n)
            return n;
        else
            return roomParent[n] = FindRoot(roomParent[n]);
    }

    bool Union(int x, int y)
    {
        x = FindRoot(x);
        y = FindRoot(y);
        if (x == y)
            return true;
        return false;
    }

    void Merge(int x, int y)
    {
        x = FindRoot(x);
        y = FindRoot(y);
        if (x == y)
            return;
        roomParent[y] = x;
    }
    #endregion

    IEnumerator CoMakeMST() // 만들어진 방의 인접 그래프를 통하여 최소스패닝트리 생성
    {
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < roomObjs.Count; ++i)
        {
            roomObjs[i].isCheck = true;
            foreach (RoomColliderObj neighbor in roomObjs[i].neighborRooms)
            {
                if (neighbor.isCheck)
                    continue;
                float weight = Vector2.Distance(rooms[i].centerPos, rooms[neighbor.RoomNumber].centerPos);

                rooms[i].linkedRooms.Add(neighbor.RoomNumber, weight);

                // 각 방들을 잇는 간선 생성
                edges.Add(new KeyValuePair<float, RoomPair>(weight, new RoomPair(rooms[i], rooms[neighbor.RoomNumber])));
            }
        }

        // 간선 오름차순 정렬
        edges.Sort((x, y) => x.Key.CompareTo(y.Key));

        // 최소 스패닝 트리 생성
        for (int i = 0; i < edges.Count; ++i)
        {
            Room x = edges[i].Value.first;
            Room y = edges[i].Value.second;
            if (Union(x.roomNumber, y.roomNumber))
                continue;
            Merge(x.roomNumber, y.roomNumber);
            mstRooms.Add(new RoomPair(x, y));
        }
        MakeRoad();

        FillRoom();

        for (int i = 0; i < 4; i++)
        {
            SmoothMap();
        }

        BrushTileMap();

        foreach (RoomColliderObj room in roomObjs)
            Destroy(room.gameObject);
        roomObjs.Clear();
    }
    void MakeRoad()
    {
        foreach (RoomPair roomPair in mstRooms)
        {
            // 최소 스패닝 트리로 연결된 각 간선과 정점들의 각도를 확인
            Room left = roomPair.first;
            Room right = roomPair.second;
            Vector2 offset = right.centerPos - left.centerPos;
            float deg = Mathf.Atan2(offset.y,offset.x) * Mathf.Rad2Deg;
            if (deg < 0)
                deg += 360;
            
            // 각도에 따른 복도 생성
            if (deg > 315 && deg <= 360 ||deg >= 0 && deg <= 45) // 315 ~ 45 : 우측
            {
                Vector2Int sPos = new Vector2Int(left.tRight.x, Mathf.RoundToInt(left.centerPos.y));
                Vector2Int ePos = new Vector2Int(right.bLeft.x, Mathf.RoundToInt(right.centerPos.y));

                int centerX = (right.bLeft.x + left.tRight.x)/2;
                for (int i = sPos.x; i < centerX; ++i)
                    map[i, sPos.y] = 2;
                for (int i = centerX; i < ePos.x; ++i)
                    map[i, ePos.y] = 2;
                if (sPos.y < ePos.y)
                    for (int i = sPos.y; i <= ePos.y; ++i)
                        map[centerX, i] = 2;
                else
                    for (int i = ePos.y; i <= sPos.y; ++i)
                        map[centerX, i] = 2;
            }
            else if (deg > 45 && deg <= 135) // 45 ~ 135 : 상측
            {
                Vector2Int sPos = new Vector2Int(Mathf.RoundToInt(left.centerPos.x),left.tRight.y);
                Vector2Int ePos = new Vector2Int(Mathf.RoundToInt(right.centerPos.x),right.bLeft.y);

                int centerY = (right.bLeft.y + left.tRight.y)/2;
                for (int i = sPos.y; i < centerY; ++i)
                    map[sPos.x, i] = 2;
                for (int i = centerY; i < ePos.y; ++i)
                    map[ePos.x, i] = 2;
                if (sPos.x < ePos.x)
                    for (int i = sPos.x; i <= ePos.x; ++i)
                        map[i, centerY] = 2;
                else
                    for (int i = ePos.x; i <= sPos.x; ++i)
                        map[i, centerY] = 2;

            }
            else if (deg > 135 && deg <= 225) // 135 ~ 225 : 좌측
            {
                Room temp = left;
                left = right;
                right = temp;
                Vector2Int sPos = new Vector2Int(left.tRight.x, Mathf.RoundToInt(left.centerPos.y));
                Vector2Int ePos = new Vector2Int(right.bLeft.x, Mathf.RoundToInt(right.centerPos.y));

                int centerX = (right.bLeft.x + left.tRight.x)/2;
                for (int i = sPos.x; i < centerX; ++i)
                    map[i, sPos.y] = 2;
                for (int i = centerX; i < ePos.x; ++i)
                    map[i, ePos.y] = 2;
                if (sPos.y < ePos.y)
                    for (int i = sPos.y; i <= ePos.y; ++i)
                        map[centerX, i] = 2;
                else
                    for (int i = ePos.y; i <= sPos.y; ++i)
                        map[centerX, i] = 2;
            }
            else                                 // 225 ~ 315 : 하측
            {
                Room temp = left;
                left = right;
                right = temp;
                Vector2Int sPos = new Vector2Int(Mathf.RoundToInt(left.centerPos.x),left.tRight.y);
                Vector2Int ePos = new Vector2Int(Mathf.RoundToInt(right.centerPos.x),right.bLeft.y);

                int centerY = (right.bLeft.y + left.tRight.y)/2;
                for (int i = sPos.y; i < centerY; ++i)
                    map[sPos.x, i] = 2;
                for (int i = centerY; i < ePos.y; ++i)
                    map[ePos.x, i] = 2;
                if (sPos.x < ePos.x)
                    for (int i = sPos.x; i <= ePos.x; ++i)
                        map[i, centerY] = 2;
                else
                    for (int i = ePos.x; i <= sPos.x; ++i)
                        map[i, centerY] = 2;
            }
        }
    }

    void BrushTileMap()
    {
        roadTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        // 외벽 쌓기
        for (int x = -3; x < width + 3; ++x)
        {
            wallTilemap.SetTile(new Vector3Int(x, -3, 0), wall);
            wallTilemap.SetTile(new Vector3Int(x, -2, 0), wall);
            wallTilemap.SetTile(new Vector3Int(x, -1, 0), wall);
            wallTilemap.SetTile(new Vector3Int(x, height, 0), wall);
            wallTilemap.SetTile(new Vector3Int(x, height + 1, 0), wall);
            wallTilemap.SetTile(new Vector3Int(x, height + 2, 0), wall);
        }
        for (int y = -3; y < height + 3; ++y)
        {
            wallTilemap.SetTile(new Vector3Int(-3, y, 0), wall);
            wallTilemap.SetTile(new Vector3Int(-2, y, 0), wall);
            wallTilemap.SetTile(new Vector3Int(-1, y, 0), wall);
            wallTilemap.SetTile(new Vector3Int(width, y, 0), wall);
            wallTilemap.SetTile(new Vector3Int(width + 1, y, 0), wall);
            wallTilemap.SetTile(new Vector3Int(width + 2, y, 0), wall);
        }

        for (int x = 0; x < width; ++x)
            for (int y = 0; y < height; ++y)
            {
                if (map[x, y] == 1 || map[x, y] == 4)
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wall);

                roadTilemap.SetTile(new Vector3Int(x, y, 0), road);
            }
    }

    void MakeNodes(ref Node node, int depth)
    {
        node.depth = depth;
        if (depth >= maxDepth)
            return;

        int dividedRatio = Random.Range(40,61);

        depth++;
        node.SetDir();

        if (node.DivideNode(dividedRatio, minRoomSize))
        {
            MakeNodes(ref node.lNode, depth);
            MakeNodes(ref node.rNode, depth);
            nodes.Add(node.lNode);
            nodes.Add(node.rNode);
        }
    }

    void MakeRooms()
    {
        foreach (Node node in nodes)
        {
            if (node.CreateRoom(bLeftRatio,tRightRatio))
            {
                for (int x = node.roomBl.x; x <= node.roomTr.x; ++x)
                    for (int y = node.roomBl.y; y <= node.roomTr.y; ++y)
                    {
                        map[x, y] = 2; // 변화하지 않을 땅으로 설정
                    }
                leafNodes.Add(node);

                // ==============================================
                // 인접 방 확인 프로세스
                // 방과 동일한 크기의 충돌체 생성
                Vector2 pos = GetCenter(node.bLeft,node.tRight);
                float width = node.tRight.x - node.bLeft.x;
                float height = node.tRight.y - node.bLeft.y;

                GameObject obj = new GameObject();
                obj.transform.position = pos;

                // Collider 충돌 이벤트가 들어있는 클래스 할당
                RoomColliderObj bspR = obj.AddComponent<RoomColliderObj>();
                bspR.RoomNumber = roomNumber++;

                // 충돌처리를 위한 Rigidbody와 Collider를 생성
                Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                BoxCollider2D col =  obj.AddComponent<BoxCollider2D>();
                col.size = new Vector2(width, height);
                col.isTrigger = true;
                roomObjs.Add(bspR);

                // 생성된 방들의 실제 크기(땅의 크기)를 갖는 Room 클래스 생성 및 rooms에 추가
                Room room = new Room();
                room.bLeft = node.roomBl;
                room.tRight = node.roomTr;
                room.centerPos = GetCenter(room.bLeft, room.tRight);
                room.roomNumber = bspR.RoomNumber;
                rooms.Add(room);

                roomParent.Add(room.roomNumber);
            }
        }
    }

    void FillRoom()
    {
        foreach (Node node in leafNodes)
        {
            RandomFillMap(node.bLeft, node.tRight);
        }
    }

    void RandomFillMap(Vector2Int bl, Vector2Int tr)
    {
        for (int x = bl.x; x < tr.x; x++)
        {
            for (int y = bl.y; y < tr.y; y++)
            {
                if (map[x, y] != 4 && map[x, y] != 2)
                {
                    if (x == bl.x || y == bl.y || x == tr.x - 1 || y == tr.y - 1)
                        map[x, y] = 4;
                    else
                        map[x, y] = Random.Range(0, 101) <= randomFillPercent ? 1 : 3;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] != 4 && map[x, y] != 2)
                    Cellular(map[x, y], x, y);
            }
        }
    }

    void Cellular(int mapValue, int x, int y)
    {
        int count = 0;
        // 상하좌우대각선
        int[] dx = {-1, 0, 1, -1, 1, -1,  0,  1 };
        int[] dy = { 1, 1, 1,  0, 0, -1, -1, -1 };
        for (int i = 0; i < dx.Length; ++i)
        {
            if (x + dx[i] < 0 || x + dx[i] >= width || y + dy[i] < 0 || y + dy[i] >= height)
                continue;
            if (map[x + dx[i], y + dy[i]] == 3 || map[x + dx[i], y + dy[i]] == 2)
                count++;
        }
        switch (mapValue)
        {
            case 3:
                if (count < 4)
                    map[x, y] = 1;
                break;
            case 1:
                if (count >= 5)
                    map[x, y] = 3;
                break;
        }
    }

    Vector2 GetCenter(Vector2Int bl, Vector2Int tr)
    {
        // 방의 가로폭과 세로폭 계산
        float width = tr.x - bl.x;
        float height = tr.y - bl.y;

        // 방의 중심 좌표 계산
        float centerX = bl.x + width / 2;
        float centerY = bl.y + height / 2;

        return new Vector2(centerX, centerY);
    }
}
