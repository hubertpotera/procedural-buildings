using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    [CreateAssetMenu(fileName = "HousePlan", menuName = "ScriptableObjects/HousePlan")]
    public class HousePlanSO : ScriptableObject
    {
        public Vector2Int GridSize;

        public float PrivateAreaRatio;
        public float PublicAreaRatio;

        public Room[] Rooms;
    }
}
