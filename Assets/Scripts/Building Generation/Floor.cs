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
        private System.Random _prng;
        private RoomWInfo[] _rooms;

        public Floor(int seed, HousePlanSO plan, Vector2Int[] outsideCells)
        {
            _gridSize = plan.GridSize;
            _grid = new int[_gridSize.x * _gridSize.y];
            _prng = new System.Random(seed);
            _rooms = new RoomWInfo[_rooms.Length];

            for (int i = 0; i < _rooms.Length; i++)
            {
                _rooms[i] = new RoomWInfo(plan.Rooms[i], 2+i); // 0,1,2 are reserved
            }

            // Set outside cells to -1
            int outsideArea = 0;
            for (int i = 0; i < outsideCells.Length; i++)
            {
                _grid[PosToIdx(outsideCells[i])] = -1;
                outsideArea ++;
            }

            // Divide to private and public area
            RoomWInfo privateArea = new RoomWInfo(new Room(RoomType.Empty, plan.PrivateAreaRatio), _private);
            RoomWInfo publicArea = new RoomWInfo(new Room(RoomType.Empty, plan.PublicAreaRatio), _public);
            Vector2Int[] areaSeeds = FindRoomSeeds(_inside, new RoomWInfo[]{privateArea, publicArea});
        }

        private Vector2Int[] FindRoomSeeds(int validPlacement, RoomWInfo[] rooms)
        {
            Vector2Int[] roomSeeds = new Vector2Int[rooms.Length];

            int availableArea = 0;
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] == validPlacement) availableArea ++;
            }
            int totalAreaRatio = 0;
            for (int i = 0; i < rooms.Length; i++)
            {
                totalAreaRatio += rooms[i].AreaRatio;
            }

            for (int roomIdx = 0; roomIdx < rooms.Length; roomIdx++)
            {
                //TODO: adjecency 
                float areaPercentage = (float)rooms[roomIdx].AreaRatio / (float)totalAreaRatio;
                float areaToTake = areaPercentage * availableArea;
                int wallDist = (int)(0.2f*Mathf.Sqrt(areaToTake));

                int[] gridWeight = new int[_grid.Length];
                int totalWeights = 0;

                // Fill grid with weights
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
                    gridWeight[cellIdx] = 1;
                    totalWeights += 1;
                }
                for (int i = 0; i < roomIdx; i++)
                { 
                    // Set weight of cells around previous seeds to 0
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            Vector2Int offset = new Vector2Int(x,y);
                            gridWeight[PosToIdx(roomSeeds[i] + offset)] = 0;
                        }
                    }
                }

                // Pick cell for room seed
                int n = _prng.Next(0, totalWeights);
                int gone = 0;
                for (int i = 0; i < gridWeight.Length; i++)
                {
                    gone += gridWeight[i];
                    if(n < gone)
                    {
                        roomSeeds[roomIdx] = IdxToPos(i);
                        rooms[roomIdx].Seed = roomSeeds[roomIdx];
                        break;
                    }
                }
            }

            return roomSeeds;
        }

        private void ExpandRooms(int validPlacement, RoomWInfo[] rooms, int[] roomIndeces, Vector2Int[] roomSeeds)
        {
            int availableArea = 0;
            int totalAreaRatio = 0;
            int[] maxAreas = new int[rooms.Length];
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] == validPlacement) availableArea ++;
            }
            for (int i = 0; i < rooms.Length; i++)
            {
                totalAreaRatio += rooms[i].AreaRatio;
            }
            for (int i = 0; i < rooms.Length; i++)
            {
                // Calculate max areas
                maxAreas[i] = (int)(0.5f*availableArea * rooms[i].AreaRatio/totalAreaRatio);
                // Claim first cells
                _grid[PosToIdx(roomSeeds[i])] = roomIndeces[i];
            }

            List<RoomWInfo> roomsToExpand = new List<RoomWInfo>();
            roomsToExpand.AddRange(rooms);
            // Grow in rectangles
            while (roomsToExpand.Count != 0)
            {
                int idx = PickRoomWeighted(roomsToExpand);
            }
        }

        private int PickRoomWeighted(List<RoomWInfo> available)
            {
                int totalRatios = 0;
                for (int i = 0; i < available.Count; i++)
                {
                    totalRatios += available[i].AreaRatio;
                }
                int n = _prng.Next(0, totalRatios);
                int gone = 0;
                for (int i = 0; i < available.Count; i++)
                {
                    gone += available[i].AreaRatio;
                    if(n < gone)
                    {
                        return i;
                    }
                }
                return available.Count-1;
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

        private class RoomWInfo : Room
        {
            public int Idx;
            public Vector2Int Seed;

            public RoomWInfo(Room baseRoom, int idx) : base(baseRoom.Type, baseRoom.AreaRatio)
            {
                Idx = idx;
            }

            public bool GrowRect(int validPlacement)
            {
                List<int> candidateExpantions = new List<int>();

                

                return false;
            }
        }
    }
}
