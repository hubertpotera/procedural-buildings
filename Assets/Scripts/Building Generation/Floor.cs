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

        private int[] _grid;
        private Vector2Int _gridSize;
        private int floorArea;



        public Floor(HousePlanSO plan, Vector2Int[] outsideCells)
        {
            _gridSize = plan.GridSize;
            _grid = new int[_gridSize.x * _gridSize.y];

            // Set outside cells to -1
            int outsideArea = 0;
            for (int i = 0; i < outsideCells.Length; i++)
            {
                _grid[PosToIdx(outsideCells[i])] = -1;
                outsideArea ++;
            }
            floorArea = _grid.Length - outsideArea;

            // Divide to private and public area
            Room privateArea = new Room(RoomType.Empty, plan.PrivateAreaRatio);
            Room publicArea = new Room(RoomType.Empty, plan.PublicAreaRatio);
            FindSeeds(0, new Room[]{privateArea, publicArea});
        }

        private Vector2Int[] FindSeeds(int validPlacement, Room[] rooms)
        {
            //calculate percentage area of each room
            //based on that calculate minimum distance from walls (?how exactly?)
            //set weights
            //get final seeds
            Vector2Int[] seeds = new Vector2Int[rooms.Length];

            int availableArea = 0;
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] == validPlacement) availableArea ++;
            }
            int totalRatio = 0;
            for (int i = 0; i < rooms.Length; i++)
            {
                totalRatio += rooms[i].AreaRatio;
            }

            for (int roomIdx = 0; roomIdx < rooms.Length; roomIdx++)
            {
                float areaPercentage = (float)rooms[roomIdx].AreaRatio / (float)totalRatio;
                float areaToTake = areaPercentage * availableArea;
                int wallDist = 1 + (int)(0.2f*Mathf.Sqrt(areaToTake));
                float[] gridWeight = new float[_grid.Length];

                for (int cellIdx = 0; cellIdx < gridWeight.Length; cellIdx++)
                {
                    if(_grid[cellIdx] != validPlacement) continue;
                    bool valid = true;
                    for (int y = -wallDist; y <= wallDist; y++)
                    {
                        for (int x = -wallDist; x <= wallDist; x++)
                        {
                            if(!valid) continue;
                            Vector2Int offset = new Vector2Int(x,y);
                            Vector2Int cellCheck = offset + IdxToPos(cellIdx);
                            if(cellCheck.x < 0 || cellCheck.y < 0 || cellCheck.x >= _gridSize.x || cellCheck.y >= _gridSize.y) 
                            {
                                valid = false;
                                continue;
                            }
                            if(_grid[PosToIdx(cellCheck)] != validPlacement)
                            {
                                valid = false;
                            }
                        }
                    }
                    if(!valid) continue;

                    gridWeight[cellIdx] = 1f;

                    Vector2Int pos = IdxToPos(cellIdx);
                    GameObject.CreatePrimitive(PrimitiveType.Cube).transform.position = new Vector3(pos.x, roomIdx, pos.y);
                }
            }

            return seeds;
        }

        private void ExpandRooms(Room[] rooms, int[] roomIndeces, Vector2Int[] seeds)
        {

        }

        private void CheckValidity()
        {

        }

        private int PosToIdx(Vector2Int pos)
        {
            return pos.y*_gridSize.x + pos.x;
        }

        private Vector2Int IdxToPos(int idx)
        {
            int y = idx / _gridSize.x;
            int x = idx - y*_gridSize.x;
            return new Vector2Int(x, y);
        }

        public void ShowDebug()
        {
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] == -1) continue;

                Vector2 pos = IdxToPos(i);

                Transform tr = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                tr.position = new Vector3(pos.x,0f,pos.y);
                tr.rotation = Quaternion.Euler(90f,0f,0f);
            }
        }
    }
}
