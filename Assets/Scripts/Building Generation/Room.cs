using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    public enum RoomType
    {
        Empty,
        Bedroom,
        Bathroom,
        Hallway,
        LivingRoom,
        DiningRoom,
        Kitchen
    }

    [System.Serializable]
    public class Room
    {
        public RoomType Type;
        public int AreaRatio;
        public List<RoomType> Neighbouring;

        public Room(RoomType type, int areaRatio, List<RoomType> neighbouring)
        {
            Type = type;
            AreaRatio = areaRatio;
            Neighbouring = neighbouring;
            if(AreaRatio <= 0) AreaRatio = 1; 
        }
    }
}