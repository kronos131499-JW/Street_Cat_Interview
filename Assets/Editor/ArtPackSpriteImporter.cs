#if UNITY_EDITOR
using UnityEditor;

namespace StreetCat.Editor
{
    /// <summary>Keep 拆拆拆 PNG exports ready for Unity UI and preserve transparency.</summary>
    public sealed class ArtPackSpriteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/VnArt/UI/ArtPack/")) return;
            if (!(assetImporter is TextureImporter importer)) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
        }
    }
}
#endif
