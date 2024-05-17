using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum Direction
{
    Vertical,
    Horizontal
}

public class TreeNode
{
    // 노드가 차지하는 2차원 내에서의 사각형 크기
    // 좌하단, 우상단의 좌표를 통해 사각형을 그린다.
    public Vector2Int bottomLeft, topRight;

    public TreeNode parentNode;
    public TreeNode leftNode, rightNode;

    // 모든 노드는 한번만 나뉘어질 수 있다.
    public bool isDivided;

    public int depth;

    public Vector3Int roomBL, roomTR;

    private Direction dir;
    public Direction Dir { get { return dir; } }

    public TreeNode() { }

    public TreeNode(Vector2Int bottomLeft, Vector2Int topRight)
    {
        this.bottomLeft = bottomLeft;
        this.topRight = topRight;
    }

    public void SetDirection()
    {
        if (topRight.x - bottomLeft.x > topRight.y - bottomLeft.y)
            dir = Direction.Vertical;
        else if (topRight.x - bottomLeft.x < topRight.y - bottomLeft.y)
            dir = Direction.Horizontal;

        else
        {
            int tempDirction = Random.Range(0, 2);
            if (tempDirction == 1)
                dir = Direction.Vertical;
            else
                dir = Direction.Horizontal;
        }
    }

    public bool DivideNode(int ratio, int minSize)
    {
        float temp;
        Vector2Int divideLine1, divideLine2;
        switch (Dir)
        {
            case Direction.Vertical:
            {
                temp = topRight.x - bottomLeft.x;
                temp = temp * ratio / 100;
                int width = Mathf.RoundToInt(temp);
                if (width < minSize || topRight.x - bottomLeft.x - width < minSize || topRight.y - bottomLeft.y < 3)
                {
                    return false;
                }
                divideLine1 = new Vector2Int(bottomLeft.x + width, topRight.y);
                divideLine2 = new Vector2Int(bottomLeft.x + width, bottomLeft.y);
            }
            break;
            default:
            {
                temp = topRight.y - bottomLeft.y;
                temp = temp * ratio / 100;
                int height = Mathf.RoundToInt(temp);
                if (height < minSize || topRight.y - bottomLeft.y - height < minSize || topRight.x - bottomLeft.x < 3)
                    return false;
                divideLine1 = new Vector2Int(topRight.x, bottomLeft.y + height);
                divideLine2 = new Vector2Int(bottomLeft.x, bottomLeft.y + height);
            }
            break;
        }
        leftNode = new TreeNode(bottomLeft, divideLine1);
        rightNode = new TreeNode(divideLine2, topRight);
        leftNode.parentNode = rightNode.parentNode = this;
        isDivided = true;
        return true;
    }

    public void CreateRoom()
    {
        int distanceFrom = 2;
        if (isDivided == false)
        {
            roomBL = new Vector3Int(bottomLeft.x + distanceFrom, bottomLeft.y + distanceFrom, 0);
            roomTR = new Vector3Int(topRight.x - distanceFrom, topRight.y - distanceFrom, 0);
        }
    }
}

public class Door
{
    public Vector2Int node1;
    public Vector2Int node2;
    public bool isHorizontal;

    public Door(Vector2Int node1, Vector2Int node2)
    {
        this.node1 = node1;
        this.node2 = node2;
    }
}



public class MapGenerator : MonoBehaviour
{
    private List<TreeNode> treeList;
    private List<TreeNode> roomList;

    private List<Door> doorList;

    [SerializeField]
    private int width, height;

    [SerializeField]
    private int roomMinSize;
    [SerializeField]
    private int maxDepth;
    private int minDepth = 0;

    [SerializeField]
    private int[,] map;

    [SerializeField]
    private RuleTile wall;
    [SerializeField]
    private Tile road;
    [SerializeField]
    private Tilemap roadTilemap;
    [SerializeField]
    private Tilemap wallTilemap;

    CellularAutomataMap cellularAutomataMap;

    void Start()
    {
        treeList = new();
        roomList = new();
        doorList = new();

        cellularAutomataMap = GameObject.FindWithTag("Cellular").GetComponent<CellularAutomataMap>();
    }

    public void GenerateMap()
    {
        map = new int[width, height];

        Clear();

        TreeNode root = new TreeNode(new Vector2Int(0,0),new Vector2Int(width,height));
        treeList.Add(root);
        ToMakeTree(ref root, minDepth);
        ToMakeRoom();
        ConnectRoom();
        // ExtendLine();

        // BuildWall();
        CreateTileMap();
    }

    void Clear()
    {
        treeList.Clear();
        roomList.Clear();
        doorList.Clear();
        roadTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    void ToMakeTree(ref TreeNode node, int depth)
    {
        node.depth = depth;
        if (depth >= maxDepth)
            return;

        int dividedRatio = Random.Range(30,71);
        depth++;
        node.SetDirection();
        if (node.DivideNode(dividedRatio, roomMinSize))
        {
            ToMakeTree(ref node.leftNode, depth);
            ToMakeTree(ref node.rightNode, depth);
            treeList.Add(node.leftNode);
            treeList.Add(node.rightNode);
        }
    }

    void ToMakeRoom()
    {
        for (int x = 0; x < treeList.Count; ++x)
        {
            TreeNode node = treeList[x];
            node.CreateRoom();

            if (node.isDivided == false)
            {
                for (int ry = node.roomBL.y; ry <= node.roomTR.y; ry++)
                    for (int rx = node.roomBL.x; rx <= node.roomTR.x; rx++)
                    {
                        if (rx == node.roomBL.x || rx == node.roomTR.x || ry == node.roomBL.y || ry == node.roomTR.y)
                            map[ry, rx] = 1;
                        else
                            map[ry, rx] = 3;
                    }
                roomList.Add(node);
            }
        }
    }

    void ConnectRoom()
    {
        for (int x = 0; x < treeList.Count; ++x)
        {
            for (int y = 0; y < treeList.Count; ++y)
            {
                TreeNode nodeX = treeList[x];
                TreeNode nodeY = treeList[y];

                if (nodeX != nodeY && nodeX.parentNode == nodeY.parentNode)
                {
                    switch (nodeX.parentNode.Dir)
                    {
                        case Direction.Vertical:
                        {
                            int temp = (nodeX.topRight.y + nodeX.bottomLeft.y) / 2;
                            Door door = new Door(new Vector2Int(nodeX.parentNode.leftNode.topRight.x - 2, temp),
                                new Vector2Int(nodeY.parentNode.rightNode.bottomLeft.x + 2, temp));
                            door.isHorizontal = true;
                            doorList.Add(door);
                            MarkDoorOnMap(door);
                            break;
                        }
                        default:
                        {
                            int temp = (nodeX.parentNode.leftNode.topRight.x + nodeX.parentNode.leftNode.bottomLeft.x) / 2;
                            Door line = new Door(new Vector2Int(temp, nodeX.parentNode.leftNode.topRight.y - 2),
                                new Vector2Int(temp, nodeY.parentNode.rightNode.bottomLeft.y + 2));
                            line.isHorizontal = false;
                            doorList.Add(line);
                            MarkDoorOnMap(line);
                            break;
                        }
                    }
                }
            }
        }
    }

    void MarkDoorOnMap(Door line)
    {
        if (line.node1.x == line.node2.x)
            for (int y = line.node1.y; y <= line.node2.y; y++)
            { map[y, line.node1.x] = 2; }
        else
            for (int x = line.node1.x; x <= line.node2.x; x++)
            { map[line.node1.y, x] = 2; }
    }

    void ExtendLine()
    {
        for (int x = 0; x < doorList.Count; ++x)
        {
            Door door = doorList[x];
            if (door.isHorizontal)
            {
                while (true)
                {
                    int lx = door.node1.x;
                    int ly = door.node1.y;
                    if (map[ly, lx - 1] == 0 || map[ly, lx - 1] == 1)
                    {
                        if (map[ly + 1, lx] == 2 || map[ly - 1, lx] == 2 || map[ly + 1, lx] == 3 || map[ly - 1, lx] == 3)
                            break;
                        map[ly, lx - 1] = 2;
                        door.node1.x = lx - 1;
                    }
                    else
                        break;
                }
                while (true)
                {
                    int lx = door.node2.x;
                    int ly = door.node2.y;
                    if (map[ly, lx + 1] == 0 || map[ly, lx + 1] == 1)
                    {
                        if (map[ly + 1, lx] == 2 || map[ly - 1, lx] == 2 || map[ly + 1, lx] == 3 || map[ly - 1, lx] == 3)
                            break;
                        map[ly, lx + 1] = 2;
                        door.node2.x = lx + 1;
                    }
                    else
                        break;
                }
            }
            else
            {
                while (true)
                {
                    int lx = door.node2.x;
                    int ly = door.node2.y;
                    if (map[ly + 1, lx] == 0 || map[ly + 1, lx] == 1)
                    {
                        if (map[ly, lx + 1] == 2 || map[ly, lx - 1] == 2 || map[ly, lx + 1] == 3 || map[ly, lx - 1] == 3)
                            break;
                        map[ly + 1, lx] = 2;
                        door.node2.y = ly + 1;
                    }
                    else
                        break;
                }
                while (true)
                {
                    int lx = door.node1.x;
                    int ly = door.node1.y;
                    if (map[ly - 1, lx] == 0 || map[ly - 1, lx] == 1)
                    {
                        if (map[ly, lx + 1] == 2 || map[ly, lx - 1] == 2 || map[ly, lx + 1] == 3 || map[ly, lx - 1] == 3)
                            break;
                        map[ly - 1, lx] = 2;
                        door.node1.y = ly - 1;
                    }
                    else
                        break;
                }
            }
        }
    }

    void BuildWall()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (map[x, y] == 2)
                {
                    for (int xx = -1; xx <= 1; xx++)
                        for (int yy = -1; yy <= 1; yy++)
                            if (map[x + xx, y + yy] == 0)
                                map[x + xx, y + yy] = 1;
                }

            }
        }
    }

    void CreateTileMap()
    {
        for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                if (map[y, x] == 1)
                    wallTilemap.SetTile(new Vector3Int(y, x, 0), wall);
                else if (map[y, x] == 2)
                    roadTilemap.SetTile(new Vector3Int(y, x, 0), road);
                else if (map[y, x] == 3)
                    roadTilemap.SetTile(new Vector3Int(y, x, 0), road);

            }
    }
}
