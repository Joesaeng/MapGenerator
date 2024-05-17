using System;
using System.Collections.Generic;
using UnityEngine;

public static class PathFinder
{

    public static bool AStarSearch(Vector2Int startPoint, Vector2Int endPoint, int[,] map)
    {
        // 상하좌우대각선
        int[] dx = {-1, 0, 1, -1, 1, -1,  0,  1};
        int[] dy = { 1, 1, 1,  0, 0, -1, -1, -1 };

        bool[,] closed = new bool[map.GetLength(0), map.GetLength(1)];
        Vector2Int[,] parent = new Vector2Int[map.GetLength(0), map.GetLength(1)];

        int[,] open = new int[map.GetLength(0),map.GetLength(1)];

        PriorityQueue<PQNode> pq = new PriorityQueue<PQNode>();

        open[startPoint.x,startPoint.y] = 10;

        parent[startPoint.x,startPoint.y] = new Vector2Int(startPoint.x, startPoint.y);

        pq.Push(new PQNode(open[startPoint.x,startPoint.y],0,startPoint.x,startPoint.y));

        while (pq.Count > 0)
        {
            PQNode node = pq.Pop();

            if (closed[node.x,node.y])
            {
                continue;
            }

            if (node.x == endPoint.x && node.y == endPoint.y)
                return true;

            for(int i = 0; i < dx.Length; ++i)
            {
                int nextX = node.x + dx[i];
                int nextY = node.y + dy[i];

                if (nextX < 0 || nextY < 0 || nextX >= map.GetLength(0) || nextY >= map.GetLength(1))
                    continue;

                if (map[nextX, nextY] == 1)
                    continue;

                if (closed[nextX, nextY])
                    continue;

                if (open[nextX, nextY] == 10)
                    continue;

                open[nextX, nextY] = 10;

                pq.Push(new PQNode(10, 1, nextX, nextY));

                parent[nextX,nextY] = new Vector2Int(nextX, nextY);
            }
        }

        // openSet이 비어있고 도착 지점을 찾지 못한 경우 경로가 존재하지 않음
        return false;
    }

    class PQNode : IComparable<PQNode>
    {
        public int f;
        public int g;
        public int x;
        public int y;

        public PQNode() { }
        public PQNode(int f, int g, int x, int y)
        {
            this.f = f;
            this.g = g;
            this.x = x;
            this.y = y;
        }

        public int CompareTo(PQNode other)
        {
            if (f == other.f)
                return g;
            return f < other.f ? 1 : -1;
        }
    }
}
