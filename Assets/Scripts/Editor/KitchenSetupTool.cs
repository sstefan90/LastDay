using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace LastDay.Editor
{
    public class KitchenSetupTool : EditorWindow
    {
        // This adds a button to your top menu
        [MenuItem("LastDay/Setup Kitchen Grid")]
        public static void SetupKitchen()
        {
            // 1. Create the Grid Parent
            GameObject gridGo = new GameObject("Kitchen_Grid", typeof(Grid));
            Grid grid = gridGo.GetComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f); // Adjust this to match your PPU (e.g. 32 PPU = 1f, 16 PPU = 0.5f)

            // 2. Create the Wall Tilemap (The slow layer)
            GameObject wallGo = new GameObject("Tilemap_Wall", typeof(Tilemap), typeof(TilemapRenderer));
            wallGo.transform.parent = gridGo.transform;
            TilemapRenderer wallTr = wallGo.GetComponent<TilemapRenderer>();
            wallTr.sortingLayerName = "Background"; // From our engineering schematic
            wallTr.sortingOrder = 0;

            // 3. Create the Floor Tilemap (Walkable area)
            GameObject floorGo = new GameObject("Tilemap_Floor", typeof(Tilemap), typeof(TilemapRenderer));
            floorGo.transform.parent = gridGo.transform;
            TilemapRenderer floorTr = floorGo.GetComponent<TilemapRenderer>();
            floorTr.sortingLayerName = "Midground_Floor";
            floorTr.sortingOrder = 0;

            // 4. Create the Countertops/Stove Tilemap (Interactive Layer)
            GameObject propGo = new GameObject("Tilemap_Counters", typeof(Tilemap), typeof(TilemapRenderer));
            propGo.transform.parent = gridGo.transform;
            TilemapRenderer propTr = propGo.GetComponent<TilemapRenderer>();
            propTr.sortingLayerName = "Objects"; // From our engineering schematic
            propTr.sortingOrder = 0;
            
            // Add interaction component to counters
            propGo.AddComponent<TilemapCollider2D>(); 

            // 5. Select the Grid in Hierarchy
            Selection.activeGameObject = gridGo;
            Debug.Log("<color=green>Kitchen Grid Created!</color> Wall, Floor, and Counter layers are ready to paint.");
        }
    }
}