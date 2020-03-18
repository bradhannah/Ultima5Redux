using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ultima5Redux
{
    public class Moongates
    {
        private DataChunk XPos { get; }
        private DataChunk YPos { get; }
        private DataChunk BuriedFlags { get; }
        private DataChunk ZPos { get; }
        
        private const int TOTAL_MOONSTONES = 8;
        
        private readonly List<Point3D> moongatePositions = new List<Point3D>(TOTAL_MOONSTONES);
        private readonly List<bool> moongatesBuried = new List<bool>(TOTAL_MOONSTONES);
        private readonly Dictionary<Point3D, bool> moongateBuriedAtPositionDictionary = new Dictionary<Point3D, bool>(TOTAL_MOONSTONES);
        
        public Point3D GetMoongatePosition(int nMoongateIndex)
        {
            return moongatePositions[nMoongateIndex];
        }

        public bool IsMoonstoneBuried(int nMoonstoneIndex)
        {
            return moongatesBuried[nMoonstoneIndex];
        }

        public bool IsMoonstoneBuried(Point3D position)
        {
            if (!moongateBuriedAtPositionDictionary.ContainsKey(position)) return false;
            return moongateBuriedAtPositionDictionary[position];
        }
        
        public Moongates(DataChunk XPos, DataChunk YPos, DataChunk BuriedFlags, DataChunk ZPos)
        {
            this.XPos = XPos;
            this.YPos = YPos;
            this.BuriedFlags = BuriedFlags;
            this.ZPos = ZPos;
            List<byte> xpos = XPos.GetAsByteList();
            List<byte> ypos = YPos.GetAsByteList();
            List<byte> zpos = ZPos.GetAsByteList();
            List<byte> buried = BuriedFlags.GetAsByteList();
            Debug.Assert(xpos.Count == TOTAL_MOONSTONES && ypos.Count == TOTAL_MOONSTONES && zpos.Count == TOTAL_MOONSTONES && buried.Count == TOTAL_MOONSTONES);
            
            for (int i = 0; i < TOTAL_MOONSTONES; i++)
            {
                moongatePositions.Add(new Point3D(xpos[i], ypos[i], zpos[i]));
                moongatesBuried.Add(buried[i] == 0);
                moongateBuriedAtPositionDictionary.Add(moongatePositions[i],moongatesBuried[i]);
            }
        }
    }
}