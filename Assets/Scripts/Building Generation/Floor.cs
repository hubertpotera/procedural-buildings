using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace BuildingGeneration
{
    public class Floor
    {
        public const int OutsideIdx = -1;
        public const int InsideIdx = 0;  // or anything greater
        public const int PublicIdx = 1;
        public const int PrivateIdx = 2;

        public readonly Vector2Int GridSize;
        public readonly int[] FloorGrid;
        public readonly RoomWInfo[] PrivateRooms;
        public readonly RoomWInfo[] PublicRooms;

        private System.Random _prng;

        public Floor(int seed, BuildingPlanSO plan, Vector2Int[] outsideCells)
        {
            GridSize = plan.GridSize;
            FloorGrid = new int[GridSize.x * GridSize.y];
            _prng = new System.Random(seed);
            PrivateRooms = new RoomWInfo[plan.PrivateRooms.Length];
            PublicRooms = new RoomWInfo[plan.PublicRooms.Length];

            for (int i = 0; i < PrivateRooms.Length; i++)
            {
                PrivateRooms[i] = new RoomWInfo(plan.PrivateRooms[i], 3+i); // 0,1,2 are reserved
            }
            for (int i = 0; i < PublicRooms.Length; i++)
            {
                PublicRooms[i] = new RoomWInfo(plan.PublicRooms[i], 3+PrivateRooms.Length+i);
            }

            // Set outside cells
            int outsideArea = 0;
            for (int i = 0; i < outsideCells.Length; i++)
            {
                FloorGrid[PosToIdx(outsideCells[i])] = OutsideIdx;
                outsideArea ++;
            }

            // Divide to private and public area
            int[] gridCopy = new int[GridSize.x * GridSize.y];
            System.Array.Copy(FloorGrid, gridCopy, FloorGrid.Length);
            bool validDivisionFound = false;
            int repeats = 0;
            while (!validDivisionFound)
            {
                repeats ++;
                RoomWInfo privateArea = new RoomWInfo(new Room(RoomType.Empty, plan.PrivateAreaRatio, new List<RoomType>()), PrivateIdx);
                RoomWInfo publicArea = new RoomWInfo(new Room(RoomType.Empty, plan.PublicAreaRatio, new List<RoomType>()), PublicIdx);
                RoomWInfo[] areas = new RoomWInfo[]{privateArea, publicArea};
                Vector2Int[] areaSeeds = FindRoomSeeds(InsideIdx, areas);
                ExpandRooms(InsideIdx, areas, areaSeeds);
                // Check if the whole grid was filled
                // validDivisionFound = true;
                // for (int i = 0; i < _grid.Length; i++)
                // {
                //     if(_grid[i] == _inside) 
                //     {
                //         validDivisionFound = false;
                //         System.Array.Copy(gridCopy, _grid, _grid.Length);
                //         break;
                //     }
                // }
                validDivisionFound = CheckValidity(areas, InsideIdx);
                if(!validDivisionFound)
                {
                    System.Array.Copy(gridCopy, FloorGrid, FloorGrid.Length);
                }
                if(repeats > 100) 
                {
                    Debug.Log("loop");
                    break;
                }
            }
            // Fill with rooms
            // First, private area
            System.Array.Copy(FloorGrid, gridCopy, FloorGrid.Length);
            validDivisionFound = false;
            repeats = 0;
            while (!validDivisionFound)
            {
                repeats ++;
                Vector2Int[] privateSeeds = FindRoomSeeds(PrivateIdx, PrivateRooms);
                ExpandRooms(PrivateIdx, PrivateRooms, privateSeeds);
                validDivisionFound = CheckValidity(PrivateRooms, PrivateIdx);
                if(!validDivisionFound)
                {
                    System.Array.Copy(gridCopy, FloorGrid, FloorGrid.Length);
                    for (int i = 0; i < PrivateRooms.Length; i++)
                    {
                        PrivateRooms[i].DidLExpansion = false;
                    }
                }
                if(repeats > 100) 
                {
                    Debug.Log("loop");
                    break;
                }
            }
            // Second, public area
            System.Array.Copy(FloorGrid, gridCopy, FloorGrid.Length);
            validDivisionFound = false;
            repeats = 0;
            while (!validDivisionFound)
            {
                repeats ++;
                Vector2Int[] publicSeeds = FindRoomSeeds(PublicIdx, PublicRooms);
                ExpandRooms(PublicIdx, PublicRooms, publicSeeds);
                validDivisionFound = CheckValidity(PublicRooms, PublicIdx);
                if(!validDivisionFound)
                {
                    System.Array.Copy(gridCopy, FloorGrid, FloorGrid.Length);
                    for (int i = 0; i < PublicRooms.Length; i++)
                    {
                        PublicRooms[i].DidLExpansion = false;
                    }
                }
                if(repeats > 100) 
                {
                    Debug.Log("loop");
                    break;
                }
            }
            
        }

        private Vector2Int[] FindRoomSeeds(int validPlacement, RoomWInfo[] rooms)
        {
            Vector2Int[] roomSeeds = new Vector2Int[rooms.Length];

            int availableArea = 0;
            for (int i = 0; i < FloorGrid.Length; i++)
            {
                if(FloorGrid[i] == validPlacement) availableArea ++;
            }
            int totalAreaRatio = 0;
            for (int i = 0; i < rooms.Length; i++)
            {
                totalAreaRatio += rooms[i].AreaRatio;
            }

            for (int roomIdx = 0; roomIdx < rooms.Length; roomIdx++)
            {
                float areaPercentage = (float)rooms[roomIdx].AreaRatio / (float)totalAreaRatio;
                float areaToTake = areaPercentage * availableArea;
                int wallDist = (int)(0.2f*Mathf.Sqrt(areaToTake));

                int[] gridWeight = new int[FloorGrid.Length];
                int totalWeights = 0;

                // Fill grid with weights
                for (int cellIdx = 0; cellIdx < gridWeight.Length; cellIdx++)
                {
                    if(FloorGrid[cellIdx] != validPlacement) continue;
                    bool valid = true;
                    for (int y = -wallDist; y <= wallDist; y++)
                    {
                        for (int x = -wallDist; x <= wallDist; x++)
                        {
                            if(!valid) continue;
                            Vector2Int offset = new Vector2Int(x,y);
                            Vector2Int cellCheck = offset + IdxToPos(cellIdx);
                            if(cellCheck.x < 0 || cellCheck.y < 0 || cellCheck.x >= GridSize.x || cellCheck.y >= GridSize.y) 
                            {
                                valid = false;
                                continue;
                            }
                            if(FloorGrid[PosToIdx(cellCheck)] != validPlacement)
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
                    // If the room should be adjecent, alter the weights accordingly
                    if(rooms[roomIdx].Neighbouring.Contains(rooms[i].Type))
                    {
                        for (int y = -2; y <= 2; y++)
                        {
                            for (int x = -2; x <= 2; x++)
                            {
                                Vector2Int offset = new Vector2Int(x,y);
                                Vector2Int cell = roomSeeds[i] + offset;
                                if(cell.x < 0 || cell.x >= GridSize.x || cell.y < 0 || cell.y >= GridSize.y) continue;
                                if(FloorGrid[PosToIdx(cell)] != validPlacement) continue;
                                int weight = (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))-1) * 7;
                                totalWeights += weight - gridWeight[PosToIdx(cell)];
                                gridWeight[PosToIdx(cell)] = weight;
                            }
                        }
                    }
                    // Else just set weight of cells around previous seeds to 0
                    else
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            for (int x = -1; x <= 1; x++)
                            {
                                Vector2Int offset = new Vector2Int(x,y);
                                Vector2Int cell = roomSeeds[i] + offset;
                                if(cell.x < 0 || cell.x >= GridSize.x || cell.y < 0 || cell.y >= GridSize.y) continue;
                                totalWeights -= gridWeight[PosToIdx(cell)];
                                gridWeight[PosToIdx(cell)] = 0;
                            }
                        }
                    }
                }

                // The hallway must always be connected to the public area
                if(rooms[roomIdx].Type == RoomType.Hallway)
                {
                    for (int cellIdx = 0; cellIdx < gridWeight.Length; cellIdx++)
                    {
                        if(FloorGrid[cellIdx] != validPlacement) continue;
                        bool valid = false;
                        for (int y = -1; y <= 1; y++)
                        {
                            for (int x = -1; x <= 1; x++)
                            {
                                if(valid) continue;
                                Vector2Int offset = new Vector2Int(x,y);
                                Vector2Int cellCheck = offset + IdxToPos(cellIdx);
                                if(cellCheck.x < 0 || cellCheck.y < 0 || cellCheck.x >= GridSize.x || cellCheck.y >= GridSize.y) 
                                {
                                    continue;
                                }
                                if(FloorGrid[PosToIdx(cellCheck)] == PublicIdx)
                                {
                                    valid = true;
                                }
                            }
                        }
                        if(!valid) 
                        {
                            totalWeights -= gridWeight[cellIdx];
                            gridWeight[cellIdx] = 0;
                            continue;
                        }

                        int weight = Mathf.Max(1, gridWeight[cellIdx]);
                        int dif = weight - gridWeight[cellIdx];
                        gridWeight[cellIdx] = weight;
                        totalWeights += dif;
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
            for (int i = 0; i < FloorGrid.Length; i++)
            {
                if(FloorGrid[i] == validPlacement) availableArea ++;
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
                FloorGrid[PosToIdx(roomSeeds[i])] = rooms[i].Idx;
            }

            List<RoomWInfo> roomsToExpand = new List<RoomWInfo>();
            roomsToExpand.AddRange(rooms);
            int repeats = 0;
            // Grow in rectangles
            while (roomsToExpand.Count != 0)
            {
                repeats ++;
                int idx = PickRoomWeighted(roomsToExpand);
                List<List<int>> expansions = FindExpansions(validPlacement, roomsToExpand[idx]);
                bool canGrow = GrowRect(roomsToExpand[idx], expansions);
                if(!canGrow) 
                {
                    roomsToExpand.RemoveAt(idx);
                }
                if(repeats > 100) 
                {   
                    Debug.Log("loop1");
                    break;
                }
            }
            roomsToExpand.AddRange(rooms);
            repeats = 0;
            // Grow with allowed L shapes
            while (roomsToExpand.Count != 0)
            {
                repeats++;
                int idx = PickRoomWeighted(roomsToExpand);
                List<List<int>> expansions = FindExpansions(validPlacement, roomsToExpand[idx]);
                bool canGrow = GrowLShape(roomsToExpand[idx], expansions, validPlacement);
                if(!canGrow) 
                {
                    roomsToExpand.RemoveAt(idx);
                }
                if(repeats > 100) 
                {   
                    Debug.Log("loop2");
                    break;
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
            for (int cellIdx = 0; cellIdx < FloorGrid.Length; cellIdx++)
            {
                if(FloorGrid[cellIdx] != room.Idx) continue;
                Vector2Int cellPos = IdxToPos(cellIdx);
                for (int i = 0; i < dirs.Length; i++)
                {
                    Vector2Int checking = cellPos + dirs[i];
                    if(checking.x < 0 || checking.y < 0 || checking.x >= GridSize.x || checking.y >= GridSize.y) continue;
                    if(FloorGrid[PosToIdx(checking)] == validPlacement) outsideCells.Add(PosToIdx(checking));
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
                else if(outsideCells.Contains(currentCell + GridSize.x))
                {
                    dif = GridSize.x;
                }
                else 
                {
                    expansion.Add(currentCell);
                    outsideCells.Remove(currentCell);
                }

                while (currentCell<FloorGrid.Length && outsideCells.Contains(currentCell))
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
                FloorGrid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
                room.CurrentArea ++;
            }
            return true;
        }

        private List<List<int>> FilterRectExpansions(List<List<int>> expansions, int roomIdx)
        {
            if(expansions.Count == 0) return expansions;

            int minX = int.MaxValue; int maxX = int.MinValue;
            int minY = int.MaxValue; int maxY = int.MinValue;
            for (int i = 0; i < FloorGrid.Length; i++)
            {
                if(FloorGrid[i] != roomIdx) continue;
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
                FloorGrid[candidateExpansions[bestExpansionIndex][i]] = room.Idx;
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
                    // Throw out expansions that aren't an L shape
                    int minX = int.MaxValue; int maxX = int.MinValue;
                    int minY = int.MaxValue; int maxY = int.MinValue;
                    for (int i = 0; i < FloorGrid.Length; i++)
                    {
                        if(FloorGrid[i] != room.Idx) continue;
                        Vector2Int pos = IdxToPos(i);
                        if(pos.x < minX) minX = pos.x;
                        if(pos.x > maxX) maxX = pos.x;
                        if(pos.y < minY) minY = pos.y;
                        if(pos.y > maxY) maxY = pos.y;
                    }
                    List<List<int>> filtered = new List<List<int>>();
                    for (int i = 0; i < expansions.Count; i++)
                    {
                        Vector2Int pos0 = IdxToPos(expansions[i][0]);
                        Vector2Int pos1 = IdxToPos(expansions[i][expansions[i].Count-1]);
                        if(pos0.x == minX || pos0.y == minY || pos1.x == maxX || pos1.y == maxY)
                        {
                            filtered.Add(expansions[i]);
                        }
                    }
                    if(filtered.Count == 0) return new List<List<int>>();
                    expansions = filtered;

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
                    int[] dirs = new int[] {-1, 1, -GridSize.x, GridSize.x};
                    if(chosenExpansion.Count == 1)
                    {
                        for (int i = 0; i < dirs.Length; i++)
                        {
                            int check = chosenExpansion[0] + dirs[i];
                            if(check >= FloorGrid.Length || check < 0) continue;
                            if(FloorGrid[check] == room.Idx)
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
                            if(chosenExpansion[0] + GridSize.x < FloorGrid.Length && 
                                FloorGrid[chosenExpansion[0] + GridSize.x] == room.Idx)
                            {
                                room.LExpansionDif = -GridSize.x;
                            }
                            else 
                            {
                                room.LExpansionDif = GridSize.x;
                            }
                        }
                        else
                        {
                            // vertical wall
                            if(chosenExpansion[0] + 1 < FloorGrid.Length && 
                                FloorGrid[chosenExpansion[0] + 1] == room.Idx)
                            {
                                room.LExpansionDif = -1;
                            }
                            else 
                            {
                                room.LExpansionDif = 1;
                            }
                        }
                    }

                    room.DidLExpansion = true;
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
                if(pos1.x == 0 || pos1.x == GridSize.x-1 || pos1.y == 0 || pos1.y == GridSize.y-1) 
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
                    if(FloorGrid[newExpansion[i]] != validPlacement) 
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

        private bool CheckValidity(RoomWInfo[] rooms, int theirValidPlacement)
        {
            // Check if all cells were filled
            for (int i = 0; i < FloorGrid.Length; i++)
            {
                if(FloorGrid[i] == theirValidPlacement) return false;
            }

            List<KeyValuePair<RoomType,int>> typeIndeces = new List<KeyValuePair<RoomType, int>>();
            for (int i = 0; i < rooms.Length; i++)
            {
                typeIndeces.Add(new KeyValuePair<RoomType, int>(rooms[i].Type, rooms[i].Idx));
            }

            for (int roomIdx = 0; roomIdx < rooms.Length; roomIdx++)
            {
                // Figure out which indeces the room needs to be neighbouring
                HashSet<int> requiredNeighbours = new HashSet<int>();
                Lookup<RoomType,int> lookup = (Lookup<RoomType,int>)typeIndeces.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
                for (int i = 0; i < rooms[roomIdx].Neighbouring.Count; i++)
                {
                    foreach (var item in lookup[rooms[roomIdx].Neighbouring[i]])
                    {
                        if(requiredNeighbours.Contains(item)) continue;
                        requiredNeighbours.Add(item);
                        break;
                    }
                }

                // Check if neighbouring all required neighbours
                Vector2Int[] dirs = new Vector2Int[] {Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.up};
                for (int cellIdx = 0; cellIdx < FloorGrid.Length; cellIdx++)
                {
                    if(FloorGrid[cellIdx] != rooms[roomIdx].Idx) continue;
                    Vector2Int cellPos = IdxToPos(cellIdx);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        Vector2Int checking = cellPos + dirs[i];
                        if(checking.x < 0 || checking.y < 0 || checking.x >= GridSize.x || checking.y >= GridSize.y) continue;
                        int val = FloorGrid[PosToIdx(checking)];
                        if(requiredNeighbours.Contains(val)) requiredNeighbours.Remove(val);
                    }
                    if(requiredNeighbours.Count == 0) break;
                }
                if(requiredNeighbours.Count > 0) return false;
            }
            return true;
        }

        private int PosToIdx(Vector2Int pos)
        {
            return pos.y*GridSize.x + pos.x;
        }

        private Vector2Int IdxToPos(int idx)
        {
            int y = idx / GridSize.x;
            int x = idx - y*GridSize.x;
            return new Vector2Int(x, y);
        }

        private Transform _debugObject;
        public void ShowDebug()
        {
            _debugObject = new GameObject("view").transform;
            for (int i = 0; i < FloorGrid.Length; i++)
            {
                if(FloorGrid[i] == -1) continue;

                Vector2 pos = IdxToPos(i);

                Transform tr = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                tr.position = new Vector3(pos.x,0f,pos.y);
                tr.rotation = Quaternion.Euler(90f,0f,0f);
                tr.parent = _debugObject;
                
                tr = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                tr.position = new Vector3(pos.x,.25f*FloorGrid[i],pos.y);
                tr.parent = _debugObject;
            }
        }

        public void DeleteDebug() 
        {
            Object.Destroy(_debugObject.gameObject);
        }

        public class RoomWInfo : Room
        {
            public int Idx;
            public int MaxArea;
            public int CurrentArea;

            public bool DidLExpansion;
            public List<int> LastLExpansion;
            public int LExpansionDif;

            public RoomWInfo(Room baseRoom, int idx) 
                : base(baseRoom.Type, baseRoom.AreaRatio, baseRoom.Neighbouring)
            {
                Idx = idx;
                DidLExpansion = false;
            }
        }
    }
}
