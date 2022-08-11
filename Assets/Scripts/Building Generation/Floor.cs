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
            _rooms = new RoomWInfo[plan.Rooms.Length];

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
            ExpandRooms(_inside, new RoomWInfo[]{privateArea, publicArea}, areaSeeds);
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
                        // TEMP --------------------------
                        GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = 
                            new Vector3(IdxToPos(i).x, 3f, IdxToPos(i).y);
                        // -------------------------------
                        break;
                    }
                }
            }

            return roomSeeds;
        }

        private void ExpandRooms(int validPlacement, RoomWInfo[] rooms, Vector2Int[] roomSeeds)
        {
            int availableArea = 0;
            int totalAreaRatio = 0;
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
                rooms[i].MaxArea = (int)(0.5f*availableArea * rooms[i].AreaRatio/totalAreaRatio);
                rooms[i].CurrentArea = 1;
                // Claim first cells
                _grid[PosToIdx(roomSeeds[i])] = rooms[i].Idx;
            }

            List<RoomWInfo> roomsToExpand = new List<RoomWInfo>();
            roomsToExpand.AddRange(rooms);
            // Grow in rectangles
            while (roomsToExpand.Count != 0)
            {
                int idx = PickRoomWeighted(roomsToExpand);
                //bool canGrow = GrowRect(validPlacement, roomsToExpand[idx]);
                bool canGrow = GrowRoom(validPlacement, roomsToExpand[idx]);
                if(!canGrow) 
                {
                    roomsToExpand.RemoveAt(idx);
                }
            }
            // roomsToExpand.AddRange(rooms);
            // Grow with allowed L shapes
            // while (roomsToExpand.Count != 0)
            // {
            //     int idx = PickRoomWeighted(roomsToExpand);
            //     bool canGrow = GrowRect(validPlacement, roomsToExpand[idx]);
            //     if(!canGrow) 
            //     {
            //         roomsToExpand.RemoveAt(idx);
            //     }
            // }
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

        private bool GrowRect(int validPlacement, RoomWInfo room)
        {
            List<List<int>> candidateExpansions = new List<List<int>>();

            for (int cellIdx = 0; cellIdx < _grid.Length; cellIdx++)
            {
                if(_grid[cellIdx] != room.Idx) continue;

                Vector2Int cellPos = IdxToPos(cellIdx);
                if(cellPos.y == 0 || _grid[PosToIdx(cellPos+Vector2Int.down)] != room.Idx)
                {
                    // Wall continues up
                    if(cellPos.x > 0 && _grid[PosToIdx(cellPos+Vector2Int.left)] == validPlacement)
                    {
                        // Outside is on the left
                        List<int> expansion = CalculateRectExpansion(validPlacement, room.Idx, cellPos, Vector2Int.up, 
                            Vector2Int.left, out bool valid);
                        if(valid) candidateExpansions.Add(expansion);
                    }
                    if(cellPos.x < _gridSize.x-1 && _grid[PosToIdx(cellPos+Vector2Int.right)] == validPlacement)
                    {
                        // Outside is on the right
                        List<int> expansion = CalculateRectExpansion(validPlacement, room.Idx, cellPos, Vector2Int.up, 
                            Vector2Int.right, out bool valid);
                        if(valid) candidateExpansions.Add(expansion);
                    }
                }
                if(cellPos.x == 0 || _grid[PosToIdx(cellPos+Vector2Int.left)] != room.Idx)
                {
                    // Wall continues right
                    if(cellPos.y > 0 && _grid[PosToIdx(cellPos+Vector2Int.down)] == validPlacement)
                    {
                        // Outside is down
                        List<int> expansion = CalculateRectExpansion(validPlacement, room.Idx, cellPos, Vector2Int.right, 
                            Vector2Int.down, out bool valid);
                        if(valid) candidateExpansions.Add(expansion);
                    }
                    if(cellPos.y < _gridSize.y-1 && _grid[PosToIdx(cellPos+Vector2Int.up)] == validPlacement)
                    {
                        // Outside is up
                        List<int> expansion = CalculateRectExpansion(validPlacement, room.Idx, cellPos, Vector2Int.right, 
                            Vector2Int.up, out bool valid);
                        if(valid) candidateExpansions.Add(expansion);
                    }
                }
            }

            // Pick biggest viable expansion
            List<int> bestExpansions = new List<int>();
            int biggestArea = -1;
            for (int i = 0; i < candidateExpansions.Count; i++)
            {
                if(candidateExpansions[i].Count == biggestArea)
                {
                    bestExpansions.Add(i);
                }
                else if(candidateExpansions[i].Count > biggestArea && 
                    candidateExpansions[i].Count < room.MaxArea-room.CurrentArea)
                {
                    bestExpansions.Clear();
                    bestExpansions.Add(i);
                    biggestArea = candidateExpansions[i].Count;
                }
            }

            if(bestExpansions.Count == 0) return false;

            int bestExpansionIndex = bestExpansions[_prng.Next( bestExpansions.Count-1)];
            // Claim
            for (int i = 0; i < candidateExpansions[bestExpansionIndex].Count; i++)
            {
                _grid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
                room.CurrentArea ++;
            }
            return true;
        }

        private List<int> CalculateRectExpansion(int validPlacement, int roomIdx, Vector2Int origin, 
            Vector2Int dir, Vector2Int outside, out bool validRect)
        {
            validRect = true;
            List<int> expansionCells = new List<int>();

            while (true)
            {
                if(origin.x == _gridSize.x || origin.y == _gridSize.y) break;

                if(_grid[PosToIdx(origin)] != roomIdx)
                {
                    break;
                }
                if(_grid[PosToIdx(origin+outside)] != validPlacement)
                {
                    validRect = false;
                    break;
                }
                expansionCells.Add(PosToIdx(origin+outside));
                origin += dir;
            }

            return expansionCells;
        }

        private bool GrowLShape(int validPlacement, RoomWInfo room)
        {
            for (int cellIdx = 0; cellIdx < _grid.Length; cellIdx++)
            {
                if(_grid[cellIdx] != room.Idx) continue;

                Vector2Int cellPos = IdxToPos(cellIdx);
                    
            }
            return false;
        }

        private bool GrowRoom(int validPlacement, RoomWInfo room)
        {
            List<List<int>> candidateExpansions = new List<List<int>>();
            List<int> outsideCells = new List<int>();
            Vector2Int[] dirs = new Vector2Int[] {Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up};

            // Find outside cells
            for (int cellIdx = 0; cellIdx < _grid.Length; cellIdx++)
            {
                if(_grid[cellIdx] != room.Idx) continue;
                Vector2Int cellPos = IdxToPos(cellIdx);
                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector2Int checking = cellPos + dirs[i];
                    if(checking.x < 0 || checking.y < 0 || checking.x >= _gridSize.x || checking.y >= _gridSize.y) continue;
                    if(_grid[PosToIdx(checking)] == validPlacement) outsideCells.Add(PosToIdx(checking));
                }
            }

            // Connect outside cells into expansions
            Debug.Log(outsideCells.Count);
            int repeat = 0;
            while (outsideCells.Count > 0)
            {
                List<int> expansion = new List<int>();
                int currentCell = outsideCells[0];
                int dif = -1;
                if(outsideCells.Contains(currentCell + 1))
                {
                    dif = 1;
                }
                else if(outsideCells.Contains(currentCell + _gridSize.x))
                {
                    dif = _gridSize.x;
                }
                else 
                {
                    expansion.Add(currentCell);
                    outsideCells.Remove(currentCell);
                }

                while (currentCell<_grid.Length && outsideCells.Contains(currentCell))
                {
                    expansion.Add(currentCell);
                    outsideCells.Remove(currentCell);
                    currentCell += dif;
                    
                    repeat++;
                    if(repeat > 1000)
                    {
                        Debug.Log("loop");
                        break;
                    }
                }

                candidateExpansions.Add(expansion);

                repeat++;
                if(repeat > 1000)
                {
                    Debug.Log("loop");
                    break;
                }
            }
            Debug.Log(candidateExpansions.Count);

            // Pick biggest viable expansion
            List<int> bestExpansions = new List<int>();
            int biggestArea = -1;
            for (int i = 0; i < candidateExpansions.Count; i++)
            {
                if(candidateExpansions[i].Count == biggestArea)
                {
                    bestExpansions.Add(i);
                }
                else if(candidateExpansions[i].Count > biggestArea && 
                    candidateExpansions[i].Count < room.MaxArea-room.CurrentArea)
                {
                    bestExpansions.Clear();
                    bestExpansions.Add(i);
                    biggestArea = candidateExpansions[i].Count;
                }
            }

            if(bestExpansions.Count == 0) return false;

            int bestExpansionIndex = bestExpansions[_prng.Next( bestExpansions.Count-1)];
            // Claim
            for (int i = 0; i < candidateExpansions[bestExpansionIndex].Count; i++)
            {
                _grid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
                room.CurrentArea ++;
            }
            return true;
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
                
                tr = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                tr.position = new Vector3(pos.x,_grid[i],pos.y);
            }
        }

        private class RoomWInfo : Room
        {
            public int Idx;
            public int MaxArea;
            public int CurrentArea;

            public RoomWInfo(Room baseRoom, int idx) : base(baseRoom.Type, baseRoom.AreaRatio)
            {
                Idx = idx;
            }

            
        }
    }
}
