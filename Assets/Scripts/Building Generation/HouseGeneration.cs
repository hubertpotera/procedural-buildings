using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    public class HouseGeneration : MonoBehaviour
    {
        public int Seed;
        public HousePlanSO HousePlan;

        private void Start() {
            Generate();
        } 

        public void Generate()
        {
            Vector2Int[] outside = new Vector2Int[15];
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    outside[y*5 + x] = new Vector2Int(x,y);
                }
            }
            Debug.Log("Generation start");
            float startTime = Time.realtimeSinceStartup;
            Floor floor = new Floor(Seed, HousePlan, outside);
            Debug.Log("Generation ended. Took " + (Time.realtimeSinceStartup-startTime));
            
            floor.ShowDebug();
        }
    }
}
