using System;
using System.Collections.Generic;
using System.Linq;

namespace Ultima5Redux.External
{
    public class Node
    {
        public readonly float Weight;
        public float Cost;

        public float DistanceToTarget;
        public Node Parent;
        public Point2D Position;

        public float F
        {
            get
            {
                if (Math.Abs(DistanceToTarget - -1) > 0.001f && Math.Abs(Cost - -1) > 0.001f)
                    return DistanceToTarget + Cost;
                return -1;
            }
        }
        // Change this depending on what the desired size is for each element in the grid
        //internal const int NODE_SIZE = 1;

        public bool Walkable { get; set; }

        public Node(Point2D pos, bool walkable, float weight = 1)
        {
            Parent = null;
            Position = pos;
            DistanceToTarget = -1;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
        }
    }

    public class AStar
    {
        private readonly List<List<Node>> _grid;

        private int GridCols => _grid.Count;

        private int GridRows => _grid[0].Count;

        public AStar(List<List<Node>> grid)
        {
            _grid = grid;
        }

        /// <summary>
        ///     Finds the best path based on surrounding tiles around a single end position
        ///     For example, below the 0 represents the end position, while the Xs represent the tiles that will be checked
        ///     XXXXX
        ///     X 0 X
        ///     XXXXX
        ///     This can be used to help find the best path to get in range of an attack
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="nUnitsOut"></param>
        /// <returns>a stack of nodes if a path is found, otherwise null if no path is found</returns>
        public Stack<Node> FindBestPathForSurroundingTiles(Point2D startPosition, Point2D endPosition, int nUnitsOut)
        {
            int nXExtent = _grid.Count - 1;
            int nYExtent = _grid[0].Count - 1;

            List<Point2D> points = endPosition.GetConstrainedSurroundingPoints(nUnitsOut, nXExtent, nYExtent);

            int nBestPathNodes = 0xFFFF;
            Stack<Node> bestPath = null;
            foreach (Point2D point in points)
            {
                Stack<Node> nodes = FindPath(startPosition, point);
                if (nodes?.Count < nBestPathNodes)
                {
                    nBestPathNodes = nodes.Count;
                    bestPath = nodes;
                }
            }

            return nBestPathNodes == 0xFFFF ? null : bestPath;
        }


        public Stack<Node> FindPath(Point2D startPosition, Point2D endPosition)
        {
            if (!_grid[endPosition.X][endPosition.Y].Walkable)
            {
                // if you pass in a non-walkable tile then we don't waste our time calculating
                return null;
            }

            Node start =
                new Node(startPosition, true);
            Node end = new Node(endPosition, true);

            Stack<Node> path = new Stack<Node>();
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            Node current = start;

            // add start node to Open List
            openList.Add(start);

            while (openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position))
            {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                IEnumerable<Node> adjacencies = GetAdjacentNodes(current);

                foreach (Node n in adjacencies)
                {
                    if (closedList.Contains(n) || !n.Walkable) continue;

                    if (openList.Contains(n)) continue;

                    n.Parent = current;
                    n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) +
                                         Math.Abs(n.Position.Y - end.Position.Y);
                    n.Cost = n.Weight + n.Parent.Cost;
                    openList.Add(n);
                    openList = openList.OrderBy(node => node.F).ToList();
                }
            }

            // construct path, if end was not closed return null
            if (!closedList.Exists(x => x.Position == end.Position)) return null;

            // if all good, return path
            Node temp = closedList[closedList.IndexOf(current)];
            if (temp == null) return null;
            do
            {
                path.Push(temp);
                temp = temp.Parent;
            } while (temp != start && temp != null);

            return path;
        }

        private IEnumerable<Node> GetAdjacentNodes(Node n)
        {
            List<Node> temp = new List<Node>();

            int row = n.Position.Y;
            int col = n.Position.X;

            if (row + 1 < GridRows) temp.Add(_grid[col][row + 1]);
            if (row - 1 >= 0) temp.Add(_grid[col][row - 1]);
            if (col - 1 >= 0) temp.Add(_grid[col - 1][row]);
            if (col + 1 < GridCols) temp.Add(_grid[col + 1][row]);

            return temp;
        }

        public Node GetNode(Point2D position)
        {
            if (position.X >= _grid.Count || position.Y >= _grid[position.X].Count)
                throw new Ultima5ReduxException("Tried to get a node with position=" + position +
                                                " but didn't exist in the astar grid");
            return _grid[position.X][position.Y];
        }

        public bool GetWalkable(Point2D position) => GetNode(position).Walkable;

        public string GetWalkableDebug()
        {
            string debugOut = "";
            for (int i = 0; i < GridCols; i++)
            {
                for (int j = 0; j < GridRows; j++)
                {
                    debugOut += _grid[i][j].Walkable ? "O" : "X";
                }

                debugOut += "\n";
            }

            return debugOut;
        }

        public void SetWalkable(Point2D position, bool bWalkable)
        {
            GetNode(position).Walkable = bWalkable;
        }
    }
}