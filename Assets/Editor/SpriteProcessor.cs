using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// when an UI asset is imported to Unity (default type) into the menu folder then it sets the type to 2D(UI and Sprite automatically)
namespace Editor
{
    public class SpriteProcessor : AssetPostprocessor
    {
        private static readonly List<string> TexturePaths = new List<string>
        {
            "Assets/Menu"
        };

        private void OnPreprocessTexture()
        {
            var textureImporter = (TextureImporter) assetImporter;

            var asset = AssetDatabase.LoadAssetAtPath(textureImporter.assetPath, typeof(Texture2D));

            if (asset || textureImporter.textureType != TextureImporterType.Default ||
                !IsInAssetPath(textureImporter.assetPath))
            {
                return;
            }   
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            
            Debug.Log($"Imported new UI texture at path {textureImporter.assetPath}.");

        }

        private static bool IsInAssetPath(string path) => TexturePaths.Any(path.Contains);
    }
}
