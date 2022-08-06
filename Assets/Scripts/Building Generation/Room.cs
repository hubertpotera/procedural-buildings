using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    public enum RoomType
    {
        Empty,
        Bedroom,
        Bathroom
    }

    [System.Serializable]
    public class Room
    {
        public RoomType Type;
        public int AreaRatio;

        public Room(RoomType type, int areaRatio)
        {
            Type = type;
            AreaRatio = areaRatio;
        }
    }
}