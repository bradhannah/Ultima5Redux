using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace Ultima5Redux
{
    [DataContract]  
    public class TileReference
    {
        [DataMember]
        public int Index;
        [DataMember]
        public string Name;
        [DataMember]
        public string Description;
        [DataMember]
        public bool IsWalking_Passable;
        [DataMember]
        public bool IsBoat_Passable;
        [DataMember]
        public bool IsSkiff_Passable;
        [DataMember]
        public bool IsCarpet_Passable;
        [DataMember]
        public bool IsPartOfAnimation;
        [DataMember]
        public int AnimationIndex;
        [DataMember]
        public bool IsUpright;
        [DataMember]
        public int FlatTileSubstitionIndex;
        [DataMember]
        public string FlatTileSubstitionName;
        [DataMember]
        public bool IsEnemy;
        [DataMember]
        public bool IsNPC;
        [DataMember]
        public bool IsBuilding;


     
    }
}
