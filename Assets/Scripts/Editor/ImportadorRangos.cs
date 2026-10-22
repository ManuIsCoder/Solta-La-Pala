using UnityEditor;

namespace SoltaLaPala.EditorTools
{
    // Configura solas las hojas de la escalera de rangos al importarlas.
    //
    // Son de 4096x3072: sin esto Unity las importaria como textura comun,
    // escalandolas a potencia de 2 (deformando las celdas) y a 2048 con
    // filtrado de mipmaps. Como Sprite se respetan las proporciones; el tope de
    // 2048 deja celdas de 256px, de sobra para lo chico que se ve en pantalla.
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
