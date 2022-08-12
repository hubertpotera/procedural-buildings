using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    public class BuildingGenerator : MonoBehaviour
    {
        public int Seed;
        public BuildingPlanSO HousePlan;

        private Floor _floor;

        private void Start() {
            Generate();
        } 

        public void Generate()
        {
            if(_floor != null) _floor.DeleteDebug();

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
            _floor = new Floor(Seed, HousePlan, outside);
            Debug.Log("Generation ended. Took " + (Time.realtimeSinceStartup-startTime));
            
            _floor.ShowDebug();
        }
    }
}
