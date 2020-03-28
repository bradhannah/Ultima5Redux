using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AStarSharp
{
    public class Node
    {
        // Change this depending on what the desired size is for each element in the grid
        public static int NodeSize = 1;
        public Node Parent;
        public Vector2 Position;
        public Vector2 Center
        {
            get
            {
                return new Vector2(Position.X + NodeSize / 2, Position.Y + NodeSize / 2);
            }
        }
        public float DistanceToTarget;
        public float Cost;
        public float Weight;
        public float F
        {
            get
            {
                if (DistanceToTarget != -1 && Cost != -1)
                    return DistanceToTarget + Cost;
                else
                    return -1;
            }
        }
        public bool Walkable;

        public Node(Vector2 pos, bool walkable, float weight = 1)
        {
            Parent = null;
            Position = pos;
            DistanceToTarget = -1;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
        }
    }

    public class Astar
    {
        List<List<Node>> _grid;
        int GridRows
        {
            get
            {
               return _grid[0].Count;
            }
        }
        int GridCols
        {
            get
            {
                return _grid.Count;
            }
        }

        public Astar(List<List<Node>> grid)
        {
            _grid = grid;
        }

        public Stack<Node> FindPath(Vector2 start, Vector2 end)
        {
            Node start = new Node(new Vector2((int)(Start.X/Node.NodeSize), (int) (Start.Y/Node.NodeSize)), true);
            Node end = new Node(new Vector2((int)(End.X / Node.NodeSize), (int)(End.Y / Node.NodeSize)), true);

            Stack<Node> path = new Stack<Node>();
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            List<Node> adjacencies;
            Node current = start;
           
            // add start node to Open List
            openList.Add(start);

            while(openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position))
            {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                adjacencies = GetAdjacentNodes(current);

 
                foreach(Node n in adjacencies)
                {
                    if (!closedList.Contains(n) && n.Walkable)
                    {
                        if (!openList.Contains(n))
                        {
                            n.Parent = current;
                            n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) + Math.Abs(n.Position.Y - end.Position.Y);
                            n.Cost = n.Weight + n.Parent.Cost;
                            openList.Add(n);
                            openList = openList.OrderBy(node => node.F).ToList<Node>();
                        }
                    }
                }
            }
            
            // construct path, if end was not closed return null
            if(!closedList.Exists(x => x.Position == end.Position))
            {
                return null;
            }

            // if all good, return path
            Node temp = closedList[closedList.IndexOf(current)];
            if (temp == null) return null;
            do
            {
                path.Push(temp);
                temp = temp.Parent;
            } while (temp != start && temp != null) ;
            return path;
        }
		
        private List<Node> GetAdjacentNodes(Node n)
        {
            List<Node> temp = new List<Node>();

            int row = (int)n.Position.Y;
            int col = (int)n.Position.X;

            if(row + 1 < GridRows)
            {
                temp.Add(_grid[col][row + 1]);
            }
            if(row - 1 >= 0)
            {
                temp.Add(_grid[col][row - 1]);
            }
            if(col - 1 >= 0)
            {
                temp.Add(_grid[col - 1][row]);
            }
            if(col + 1 < GridCols)
            {
                temp.Add(_grid[col + 1][row]);
            }

            return temp;
        }
    }
}
