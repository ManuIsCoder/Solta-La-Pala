using UnityEditor;
using UnityEngine;

namespace SoltaLaPala.EditorTools
{
    // Configura sola la musica al importarla: en streaming y comprimida.
    //
    // Por defecto Unity descomprime cada clip entero en memoria al cargarlo,
    // que para pistas de varios minutos son decenas de MB. En streaming se va
    // leyendo de a poco mientras suena.
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
