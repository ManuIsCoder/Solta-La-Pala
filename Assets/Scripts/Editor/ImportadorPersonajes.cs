using UnityEditor;
using UnityEngine;

namespace SoltaLaPala.EditorTools
{
    public class ImportadorPersonajes : AssetPostprocessor
    {
        private const string Carpeta = "Assets/Personajes/";

        public override uint GetVersion()
        {
            return 5;
        }

        private void OnPostprocessModel(GameObject raiz)
        {
            if (!assetPath.StartsWith(Carpeta))
            {
                return;
            }

            Shader dobleCara = Shader.Find("SoltaLaPala/DobleCara");
            if (dobleCara == null)
            {
                return;
            }

            foreach (Renderer renderer in raiz.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && EsPelo(renderer.name, material))
                    {
                        material.shader = dobleCara;
                    }
                }
            }
        }

        private static bool EsPelo(string objeto, Material material)
        {
            string textura = material.mainTexture != null ? material.mainTexture.name : "";
            return $"{objeto} {material.name} {textura}".ToLowerInvariant().Contains("pelo");
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Carpeta))
            {
                return;
            }

            ModelImporter importador = (ModelImporter)assetImporter;
            importador.bakeAxisConversion = true;
            importador.animationType = ModelImporterAnimationType.Generic;
            importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importador.motionNodeName = "";
            importador.optimizeGameObjects = false;
            importador.importCameras = false;
            importador.importLights = false;
            importador.importAnimation = true;
            importador.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importador.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importador.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Carpeta))
            {
                return;
            }

            ModelImporter importador = (ModelImporter)assetImporter;
            bool enLoop = !assetPath.Contains("Agarrar");

            ModelImporterClipAnimation[] clips = importador.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.loopTime = enLoop;
            }

            importador.clipAnimations = clips;
        }
    }
}
