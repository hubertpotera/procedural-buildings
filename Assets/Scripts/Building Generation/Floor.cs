using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{
    public class Floor
    {
        private const int _outside = -1;
        private const int _inside = 0;  // or anything greater
        private const int _public = 1;
        private const int _private = 2;

        private int[] _gridFootprint;
        private int[] _gridPrivacy;
        private Vector2Int _gridSize;
        private int floorArea;



        public Floor(HousePlanSO plan, Vector2Int[] outsideCells)
        {
            _gridSize = plan.GridSize;
            _gridFootprint = new int[_gridSize.x * _gridSize.y];

            // Set outside cells to -1
            int outsideArea = 0;
            for (int i = 0; i < outsideCells.Length; i++)
            {
                _gridFootprint[outsideCells[i].y*_gridSize.y + outsideCells[i].x] = -1;
                outsideArea ++;
            }
            floorArea = _gridFootprint.Length - outsideArea;


        }

        private void FindSeeds(int validPlacement, Room[] rooms)
        {
            //calculate percentage area of each room
            //based on that calculate minimum distance from walls (?how exactly?)
            //set weights
            //get final seeds

        }

        private void ExpandRooms(Room[] rooms, int[] roomIndeces, Vector2Int[] seeds)
        {

        }

        private void CheckValidity()
        {

        }
    }
}
