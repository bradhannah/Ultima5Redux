using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ultima5Redux.External
{
    public class Node
    {
        // Change this depending on what the desired size is for each element in the grid
        internal static readonly int NODE_SIZE = 1;
        public Node Parent;
        public Vector2 Position;
        
        public Vector2 Center => new Vector2(Position.X + NODE_SIZE / 2, Position.Y + NODE_SIZE / 2);
        
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

        public bool Walkable { get; }

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

    public class AStar
    {
        private readonly List<List<Node>> _grid;
        private int GridRows => _grid[0].Count;

        private int GridCols => _grid.Count;

        public AStar(List<List<Node>> grid)
        {
            _grid = grid;
        }

        public Stack<Node> FindPath(Vector2 startVector, Vector2 endVector)
        {
            Node start = new Node(new Vector2((int)(startVector.X/Node.NODE_SIZE), (int) (startVector.Y/Node.NODE_SIZE)), true);
            Node end = new Node(new Vector2((int)(endVector.X / Node.NODE_SIZE), (int)(endVector.Y / Node.NODE_SIZE)), true);

            Stack<Node> path = new Stack<Node>();
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            Node current = start;
           
            // add start node to Open List
            openList.Add(start);

            while(openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position))
            {
                current = openList[0];
                openList.Remove(current);
                closedList.Add(current);
                IEnumerable<Node> adjacencies = GetAdjacentNodes(current);

 
                foreach(Node n in adjacencies)
                {
                    if (closedList.Contains(n) || !n.Walkable) continue;
                    
                    if (openList.Contains(n)) continue;
                    
                    n.Parent = current;
                    n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) + Math.Abs(n.Position.Y - end.Position.Y);
                    n.Cost = n.Weight + n.Parent.Cost;
                    openList.Add(n);
                    openList = openList.OrderBy(node => node.F).ToList<Node>();
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
