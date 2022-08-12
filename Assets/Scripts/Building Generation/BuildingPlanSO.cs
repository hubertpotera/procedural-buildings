using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    [CreateAssetMenu(fileName = "HousePlan", menuName = "ScriptableObjects/HousePlan")]
    public class BuildingPlanSO : ScriptableObject
    {
        public Vector2Int GridSize;

        public int PrivateAreaRatio;
        public int PublicAreaRatio;

        public Room[] Rooms;
    }
}
