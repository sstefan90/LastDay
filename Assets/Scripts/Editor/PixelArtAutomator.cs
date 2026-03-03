using UnityEngine;
using UnityEditor;
using System.IO;

namespace LastDay.Editor
{
    // We change 'MonoBehaviour' to 'EditorWindow' or just a plain class
    public class PixelArtAutomator : EditorWindow 
    {
        // This line creates the button in your top menu bar
        [MenuItem("LastDay/Fix All Pixel Art")]
        public static void FixPixelArt() 
        {
            // This finds every texture in your Art folder
            // Ensure your folder is named "Art" or change the string below
            string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/Art" }); 
            
            foreach (string guid in guids) 
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                
                if (importer != null)
                {
                    // Apply our "Space for the Unbound" style settings
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single; 
                    importer.filterMode = FilterMode.Point; // The "no blur" secret
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spritePixelsPerUnit = 32; // Matches your engineering schematic
                    
                    importer.SaveAndReimport();
                }
            }
            Debug.Log($"<color=green>Success!</color> Optimized {guids.Length} pixel art assets.");
        }
    }
}