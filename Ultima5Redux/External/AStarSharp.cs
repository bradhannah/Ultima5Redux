using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ultima5Redux.External
{
    public class Node
    {
        public readonly Point2D Position;
        internal float Weight { get; }
        public float Cost { get; internal set; }

        public float DistanceToTarget { get; internal set; }

        public float F
        {
            get
            {
                if (Math.Abs(DistanceToTarget - -1) > 0.001f && Math.Abs(Cost - -1) > 0.001f)
                    return DistanceToTarget + Cost;
                return -1;
            }
        }

        public Node Parent { get; internal set; }
        // Change this depending on what the desired size is for each element in the grid

        public bool Walkable { get; set; }

        public Node(in Point2D pos, bool walkable, float weight = 1)
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

        public AStar(List<List<Node>> grid) => _grid = grid;

        private IEnumerable<Node> GetAdjacentNodes(Node n)
        {
            List<Node> temp = new();

            int row = n.Position.Y;
            int col = n.Position.X;

            if (row + 1 < GridRows) temp.Add(_grid[col][row + 1]);
            if (row - 1 >= 0) temp.Add(_grid[col][row - 1]);
            if (col - 1 >= 0) temp.Add(_grid[col - 1][row]);
            if (col + 1 < GridCols) temp.Add(_grid[col + 1][row]);

            return temp;
        }

        public Stack<Node> FindPath(in Point2D startPosition, in Point2D endPosition)
        {
            if (!_grid[endPosition.X][endPosition.Y].Walkable)
            {
                // if you pass in a non-walkable tile then we don't waste our time calculating
                return null;
            }

            Node start = new(startPosition, true);
            Node end = new(endPosition, true);

            Stack<Node> path = new();
            SortedDictionary<float, Node> openList = new();
            List<Node> closedList = new();
            Node current = start;

            // add start node to Open List
            openList.Add(start.F, start);

            // while there are nodes left in the openList 
            // AND the closedList does NOT contain an ending position
            while (openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position))
            {
                // get the "BEST" open list position to test
                current = openList.First().Value;
                // remove the "BEST" open list position
                openList.Remove(current.F);

                closedList.Add(current);
                IEnumerable<Node> adjacencies = GetAdjacentNodes(current);

                // go through each of the adjacent tiles and see if they are better
                foreach (Node n in adjacencies)
                {
                    // if the tile is NOT walkable or already in the closedList which means it cannot be used
                    // to reach your final destination
                    if (closedList.Contains(n) || !n.Walkable) continue;

                    // if the openList contains the value then it's already been checked and doesn't need to
                    // be checked again
                    if (openList.ContainsValue(n)) continue;

                    // let this node know that I am it's parent (previous space)
                    n.Parent = current;
                    n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) +
                                         Math.Abs(n.Position.Y - end.Position.Y);
                    n.Cost = n.Weight + n.Parent.Cost;

                    // we like it and add it to our list, since it is a SortedList, it will automatically organize
                    // it for us
                    while (openList.ContainsKey(n.F))
                    {
                        n.DistanceToTarget += 0.00001f;
                    }

                    openList.Add(n.F, n);
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

        public Node GetNode(Point2D position)
        {
            if (position.X >= _grid.Count || position.Y >= _grid[position.X].Count)
                throw new Ultima5ReduxException("Tried to get a node with position=" + position +
                                                " but didn't exist in the aStar grid");
            return _grid[position.X][position.Y];
        }

        public bool GetWalkable(Point2D position) => GetNode(position).Walkable;

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
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
    }
}