using System.Collections.Generic;
using System.Linq;
using Raylib_cs; // Для Color, если нужно будет отрисовывать путь для дебага

public class Pathfinder
{
    private class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Node? Parent { get; set; }
        public int GCost { get; set; } // Cost from start
        public int HCost { get; set; } // Heuristic cost to end
        public int FCost => GCost + HCost; // Total cost

        public Node(int x, int y, Node? parent = default, int gCost = 0, int hCost = 0)
        {
            X = x;
            Y = y;
            Parent = parent;
            GCost = gCost;
            HCost = hCost;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Node other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (X, Y).GetHashCode();
        }
    }

    public static List<(int x, int y)>? FindPath(GameMap map, int startX, int startY, int targetX, int targetY)
    {
        Node startNode = new Node(startX, startY);
        Node targetNode = new Node(targetX, targetY);

        List<Node> openSet = new List<Node>(); // Using List as a priority queue (sorted later)
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.Equals(targetNode))
            {
                return ReconstructPath(currentNode);
            }

            foreach (Node neighbor in GetNeighbors(map, currentNode))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.GCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newMovementCostToNeighbor;
                    neighbor.HCost = GetDistance(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private static List<(int x, int y)> ReconstructPath(Node endNode)
    {
        List<(int x, int y)> path = new List<(int x, int y)>();
        Node currentNode = endNode;
        while (currentNode != null)
        {
            path.Add((currentNode.X, currentNode.Y));
            currentNode = currentNode.Parent;
        }
        path.Reverse(); // Path is built backwards, so reverse it
        return path;
    }

    private static int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = System.Math.Abs(nodeA.X - nodeB.X);
        int dstY = System.Math.Abs(nodeA.Y - nodeB.Y);
        // Manhattan distance for grid-based movement
        return dstX + dstY;
    }

    private static IEnumerable<Node> GetNeighbors(GameMap map, Node node)
    {
        List<Node> neighbors = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip current node
                if (x != 0 && y != 0) continue; // Only straight movements (no diagonals)

                int checkX = node.X + x;
                int checkY = node.Y + y;

                if (checkX >= 0 && checkX < GameMap.MapWidth && checkY >= 0 && checkY < GameMap.MapHeight && !map.IsWall(checkX, checkY))
                {
                    neighbors.Add(new Node(checkX, checkY));
                }
            }
        }
        return neighbors;
    }
}