using UnityEditor;
using UnityEngine;

namespace SoltaLaPala.EditorTools
{
    public class ImportadorMusica : AssetPostprocessor
    {
        private const string Carpeta = "Assets/Resources/Audio/Musica/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Carpeta))
            {
                return;
            }

            AudioImporter importador = (AudioImporter)assetImporter;
            importador.forceToMono = false;
            importador.loadInBackground = true;

            AudioImporterSampleSettings ajustes = importador.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.Streaming;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = 0.7f;
            importador.defaultSampleSettings = ajustes;
        }
    }
}
