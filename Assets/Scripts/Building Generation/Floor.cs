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

        public Floor(int seed, BuildingPlanSO plan, Vector2Int[] outsideCells)
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
            int[] gridCopy = new int[_gridSize.x * _gridSize.y];
            System.Array.Copy(_grid, gridCopy, _grid.Length);
            bool validDivisionFound = false;
            while (!validDivisionFound)
            {
                RoomWInfo privateArea = new RoomWInfo(new Room(RoomType.Empty, plan.PrivateAreaRatio), _private);
                RoomWInfo publicArea = new RoomWInfo(new Room(RoomType.Empty, plan.PublicAreaRatio), _public);
                Vector2Int[] areaSeeds = FindRoomSeeds(_inside, new RoomWInfo[]{privateArea, publicArea});
                ExpandRooms(_inside, new RoomWInfo[]{privateArea, publicArea}, areaSeeds);
                // Check if the whole grid was filled
                validDivisionFound = true;
                for (int i = 0; i < _grid.Length; i++)
                {
                    if(_grid[i] == _inside) 
                    {
                        validDivisionFound = false;
                        System.Array.Copy(gridCopy, _grid, _grid.Length);
                        break;
                    }
                }
            }
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
                            Vector2Int cell = roomSeeds[i] + offset;
                            if(cell.x < 0 || cell.x >= _gridSize.x || cell.y < 0 || cell.y >= _gridSize.y) continue;
                            totalWeights -= gridWeight[PosToIdx(cell)];
                            gridWeight[PosToIdx(cell)] = 0;
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
                List<List<int>> expansions = FindExpansions(validPlacement, roomsToExpand[idx]);
                bool canGrow = GrowRect(roomsToExpand[idx], expansions);
                if(!canGrow) 
                {
                    roomsToExpand.RemoveAt(idx);
                }
            }
            roomsToExpand.AddRange(rooms);
            // Grow with allowed L shapes
            while (roomsToExpand.Count != 0)
            {
                int idx = PickRoomWeighted(roomsToExpand);
                List<List<int>> expansions = FindExpansions(validPlacement, roomsToExpand[idx]);
                bool canGrow = GrowLShape(roomsToExpand[idx], expansions, validPlacement);
                if(!canGrow) 
                {
                    roomsToExpand.RemoveAt(idx);
                }
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

        private List<List<int>> FindExpansions(int validPlacement, RoomWInfo room)
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

            return candidateExpansions;
        }

        private bool GrowRect(RoomWInfo room, List<List<int>> candidateExpansions)
        {
            candidateExpansions = FilterRectExpansions(candidateExpansions, room.Idx);
            if(candidateExpansions.Count == 0) return false;

            // Pick biggest viable expansion
            List<int> bestExpansionsIdxs = new List<int>();
            int biggestArea = -1;
            for (int i = 0; i < candidateExpansions.Count; i++)
            {
                if(candidateExpansions[i].Count == biggestArea)
                {
                    bestExpansionsIdxs.Add(i);
                }
                else if(candidateExpansions[i].Count > biggestArea && 
                    candidateExpansions[i].Count < room.MaxArea-room.CurrentArea)
                {
                    bestExpansionsIdxs.Clear();
                    bestExpansionsIdxs.Add(i);
                    biggestArea = candidateExpansions[i].Count;
                }
            }

            if(bestExpansionsIdxs.Count == 0) return false;

            int bestExpansionIndex = bestExpansionsIdxs[_prng.Next( bestExpansionsIdxs.Count-1)];
            // Claim
            for (int i = 0; i < candidateExpansions[bestExpansionIndex].Count; i++)
            {
                _grid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
                room.CurrentArea ++;
            }
            return true;
        }

        private List<List<int>> FilterRectExpansions(List<List<int>> expansions, int roomIdx)
        {
            if(expansions.Count == 0) return expansions;

            int minX = int.MaxValue; int maxX = int.MinValue;
            int minY = int.MaxValue; int maxY = int.MinValue;
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] != roomIdx) continue;
                Vector2Int pos = IdxToPos(i);
                if(pos.x < minX) minX = pos.x;
                if(pos.x > maxX) maxX = pos.x;
                if(pos.y < minY) minY = pos.y;
                if(pos.y > maxY) maxY = pos.y;
            }

            List<List<int>> filteredExpansions = new List<List<int>>();
            for (int i = 0; i < expansions.Count; i++)
            {
                if(expansions[i].Count == 1)
                {
                    // Single cell
                    Vector2Int pos = IdxToPos(expansions[i][0]);
                    if((pos.x == maxX && pos.x == minX) || (pos.y == minY && pos.y == maxY))
                    {
                        filteredExpansions.Add(expansions[i]);
                    }
                }   
                else if(expansions[i][0]+1 == expansions[i][1])
                {
                    // Horizontal wall
                    int firstX = IdxToPos(expansions[i][0]).x;
                    int lastX = IdxToPos(expansions[i][expansions[i].Count-1]).x;
                    if(firstX == minX && lastX == maxX)
                    {
                        filteredExpansions.Add(expansions[i]);
                    }
                }
                else
                {
                    // Vertical wall
                    int firstY = IdxToPos(expansions[i][0]).y;
                    int lastY = IdxToPos(expansions[i][expansions[i].Count-1]).y;
                    if(firstY == minY && lastY == maxY)
                    {
                        filteredExpansions.Add(expansions[i]);
                    }
                }
            }
            return filteredExpansions;
        }

        private bool GrowLShape(RoomWInfo room, List<List<int>> candidateExpansions, int validPlacement)
        {
            candidateExpansions = FilterLExpansions(candidateExpansions, room, validPlacement);

            if(candidateExpansions.Count == 0) return false;

            // Pick biggest expansion
            List<int> bestExpansionsIdxs = new List<int>();
            int biggestArea = -1;
            for (int i = 0; i < candidateExpansions.Count; i++)
            {
                if(candidateExpansions[i].Count == biggestArea)
                {
                    bestExpansionsIdxs.Add(i);
                }
                else if(candidateExpansions[i].Count > biggestArea)
                {
                    bestExpansionsIdxs.Clear();
                    bestExpansionsIdxs.Add(i);
                    biggestArea = candidateExpansions[i].Count;
                }
            }

            int bestExpansionIndex = bestExpansionsIdxs[_prng.Next( bestExpansionsIdxs.Count-1)];
            // Claim
            for (int i = 0; i < candidateExpansions[bestExpansionIndex].Count; i++)
            {
                _grid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
                room.CurrentArea ++;
            }
            return true;
        }

        private List<List<int>> FilterLExpansions(List<List<int>> expansions, RoomWInfo room, int validPlacement)
        {
            if(expansions.Count == 0) return expansions;
            if(!room.DidLExpansion)
            {
                List<List<int>> rectExpansions = FilterRectExpansions(expansions, room.Idx);
                if(rectExpansions.Count == 0)
                {
                    room.DidLExpansion = true;
                    // Decide on the L expansion (pick the biggest)
                    List<int> bestExpansionsIdxs = new List<int>();
                    int biggestArea = -1;
                    for (int i = 0; i < expansions.Count; i++)
                    {
                        if(expansions[i].Count == biggestArea)
                        {
                            bestExpansionsIdxs.Add(i);
                        }
                        else if(expansions[i].Count > biggestArea)
                        {
                            bestExpansionsIdxs.Clear();
                            bestExpansionsIdxs.Add(i);
                            biggestArea = expansions[i].Count;
                        }
                    }
                    int bestExpansionIndex = bestExpansionsIdxs[_prng.Next( bestExpansionsIdxs.Count-1)];
                    List<int> chosenExpansion = expansions[bestExpansionIndex];

                    // Figure out the directions of the expansion
                    int[] dirs = new int[] {-1, 1, -_gridSize.x, _gridSize.x};
                    if(chosenExpansion.Count == 1)
                    {
                        for (int i = 0; i < dirs.Length; i++)
                        {
                            int check = chosenExpansion[0] + dirs[i];
                            if(check >= _grid.Length || check < 0) continue;
                            if(_grid[check] == room.Idx)
                            {
                                room.LExpansionDif = -dirs[i];
                            }
                        }
                    }
                    else
                    {
                        if(chosenExpansion[1] - chosenExpansion[0] == 1)
                        {
                            // horizontal wall
                            if(_grid[chosenExpansion[0] + _gridSize.x] == room.Idx)
                            {
                                room.LExpansionDif = -_gridSize.x;
                            }
                            else 
                            {
                                room.LExpansionDif = _gridSize.x;
                            }
                        }
                        else
                        {
                            // vertical wall
                            if(_grid[chosenExpansion[0] + 1] == room.Idx)
                            {
                                room.LExpansionDif = -1;
                            }
                            else 
                            {
                                room.LExpansionDif = 1;
                            }
                        }
                    }

                    room.LastLExpansion = chosenExpansion;
                    return new List<List<int>> {chosenExpansion};
                }
                else
                {
                    return rectExpansions;
                }
            }
            else
            {
                // If last expansion was on the edge, the next one is not viable as it will wrap around
                Vector2Int pos1 = IdxToPos(room.LastLExpansion[0]);
                if(pos1.x == 0 || pos1.x == _gridSize.x-1 || pos1.y == 0 || pos1.y == _gridSize.y-1) 
                {
                    return new List<List<int>>();
                }

                // Check if last L expansion is still viable
                List<int> newExpansion = new List<int>();
                for (int i = 0; i < room.LastLExpansion.Count; i++)
                {
                    newExpansion.Add(room.LastLExpansion[i] + room.LExpansionDif);
                }

                bool valid = true;
                for (int i = 0; i < newExpansion.Count; i++)
                {
                    if(_grid[newExpansion[i]] != validPlacement) 
                    {
                        valid = false;
                        break;
                    }
                }

                if(valid)
                {
                    room.LastLExpansion = newExpansion;
                    return new List<List<int>> {newExpansion};
                }
                else return new List<List<int>>();
            }
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

        private Transform _debugObject;
        public void ShowDebug()
        {
            _debugObject = new GameObject("view").transform;
            for (int i = 0; i < _grid.Length; i++)
            {
                if(_grid[i] == -1) continue;

                Vector2 pos = IdxToPos(i);

                Transform tr = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                tr.position = new Vector3(pos.x,0f,pos.y);
                tr.rotation = Quaternion.Euler(90f,0f,0f);
                tr.parent = _debugObject;
                
                tr = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                tr.position = new Vector3(pos.x,_grid[i],pos.y);
                tr.parent = _debugObject;
            }
        }

        public void DeleteDebug() 
        {
            Object.Destroy(_debugObject.gameObject);
        }

        private class RoomWInfo : Room
        {
            public int Idx;
            public int MaxArea;
            public int CurrentArea;

            public bool DidLExpansion;
            public List<int> LastLExpansion;
            public int LExpansionDif;

            public RoomWInfo(Room baseRoom, int idx) : base(baseRoom.Type, baseRoom.AreaRatio)
            {
                Idx = idx;
            }
        }
    }
}
