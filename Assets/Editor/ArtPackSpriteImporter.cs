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
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;

            // Phone mockups are 1700px tall and drawn much smaller. Mipmaps with a
            // negative bias keep the post photos readable; other UI pieces stay 1:1.
            var file = System.IO.Path.GetFileName(assetPath);
            bool socialPost = file.StartsWith("手机界面帖子");
            importer.mipmapEnabled = socialPost;
            if (socialPost)
                importer.mipMapBias = -0.75f;
        }
    }
}
#endif
