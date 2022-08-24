using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BuildingGeneration
{      
    public class FloorDisplayer : MonoBehaviour
    {
        [SerializeField] private Material _bedromMaterialo;
        [SerializeField] private Material _bathroomMaterial;
        [SerializeField] private Material _hallwayMaterial;
        [SerializeField] private Material _livingRoomMaterial;
        [SerializeField] private Material _diningRoomMaterial;
        [SerializeField] private Material _kitchenMaterial;

        public void Display(Floor floor)
        {
            Dictionary<RoomType,Material> typeMaterial = new Dictionary<RoomType, Material>();
            typeMaterial.Add(RoomType.Bedroom, _bedromMaterialo);
            typeMaterial.Add(RoomType.Bathroom, _bathroomMaterial);
            typeMaterial.Add(RoomType.Hallway, _hallwayMaterial);
            typeMaterial.Add(RoomType.LivingRoom, _livingRoomMaterial);
            typeMaterial.Add(RoomType.DiningRoom, _diningRoomMaterial);
            typeMaterial.Add(RoomType.Kitchen, _kitchenMaterial);
            Dictionary<int,RoomType> idxType = new Dictionary<int, RoomType>();
            for (int i = 0; i < floor.PrivateRooms.Length; i++)
            {
                idxType.Add(floor.PrivateRooms[i].Idx, floor.PrivateRooms[i].Type);
            }
            for (int i = 0; i < floor.PublicRooms.Length; i++)
            {
                idxType.Add(floor.PublicRooms[i].Idx, floor.PublicRooms[i].Type);
            }

            // Place walls
            for (int i = 0; i < floor.FloorGrid.Length; i++)
            {
                if(floor.FloorGrid[i] == Floor.OutsideIdx) continue;
                int cellRoomIdx = floor.FloorGrid[i];
                int z = i / floor.GridSize.x;
                int x = i - z*floor.GridSize.x;
                Vector3 basePos = new Vector3(x, 0f, z);
                GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                baseGo.transform.position = basePos + .5f*Vector3.forward + .5f*Vector3.right;
                baseGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                baseGo.GetComponent<MeshRenderer>().material = typeMaterial[idxType[cellRoomIdx]];
                
                // Left check
                if (i%floor.GridSize.x == 0 || floor.FloorGrid[i-1] != cellRoomIdx)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = basePos + .5f*Vector3.forward + .5f*Vector3.up;
                    go.transform.localScale = new Vector3(.1f, 1f, 1.1f);
                }
                // Right check
                if (i%floor.GridSize.x == floor.GridSize.x-1 || floor.FloorGrid[i+1] != cellRoomIdx)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = basePos + .5f*Vector3.forward + Vector3.right + .5f*Vector3.up;
                    go.transform.localScale = new Vector3(.1f, 1f, 1.1f);
                }
                // Down check
                if (i < floor.GridSize.x || floor.FloorGrid[i-floor.GridSize.x] != cellRoomIdx)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = basePos + .5f*Vector3.right + .5f*Vector3.up;
                    go.transform.localScale = new Vector3(1.1f, 1f, .1f);
                }
                // Up check
                if (i+floor.GridSize.x >= floor.FloorGrid.Length || floor.FloorGrid[i+floor.GridSize.x] != cellRoomIdx)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = basePos + Vector3.forward + .5f*Vector3.right + .5f*Vector3.up;
                    go.transform.localScale = new Vector3(1.1f, 1f, .1f);
                }
            }
        }
    }
}
