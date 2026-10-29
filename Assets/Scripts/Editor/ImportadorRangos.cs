using UnityEditor;

namespace SoltaLaPala.EditorTools
{
    public class ImportadorRangos : AssetPostprocessor
    {
        private const string Carpeta = "Assets/Resources/Sprites/Rangos/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Carpeta))
            {
                return;
            }

            TextureImporter importador = (TextureImporter)assetImporter;
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.npotScale = TextureImporterNPOTScale.None;
            importador.mipmapEnabled = false;
            importador.alphaIsTransparency = true;
            importador.maxTextureSize = 2048;
            importador.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
