using System.Collections.Generic;
using SoltaLaPala.Sonido;
using UnityEditor;
using UnityEngine;

namespace SoltaLaPala.Editor
{
    // Arma los prefabs de Assets/Prefabs/con sonido: objetos de oficina que ya
    // traen su sonido puesto, partiendo de los mismos modelos que usa el nivel 2.
    //
    // Uso: menu "Solta La Pala > Generar prefabs con sonido". Se puede volver a
    // correr: reemplaza los prefabs que ya existan con ese nombre.
    public static class GeneradorPrefabsConSonido
    {
        private const string CarpetaPadre = "Assets/Prefabs";
        private const string NombreCarpeta = "con sonido";
        private const string Carpeta = CarpetaPadre + "/" + NombreCarpeta;
        private const string Audio = "Assets/Resources/Audio/Objetos/";

        // Modelos de origen (los mismos que se ven en el nivel 2, salvo impresora
        // y telefono, que la tarea pide de otras carpetas).
        private const string ModeloHeladera = "Assets/librerias office/kenney/kenney_furniture-kit/Models/FBX format/kitchenFridge.fbx";
        private const string ModeloLuz = "Assets/librerias office/LowPolyOfficeProps_FULL/Prefabs/Light_SuspendedStripLight_A.prefab";
        private const string ModeloMicroondas = "Assets/librerias office/VNB - Office Set/Prefabs/Office/microwave_new.prefab";
        private const string ModeloTeclado = "Assets/librerias office/LowPolyOfficeProps_FULL/Prefabs/ComputerKeyboard.prefab";
        private const string ModeloMouse = "Assets/librerias office/LowPolyOfficeProps_FULL/Meshes/ComputerGamingMouse.fbx";
        private const string ModeloImpresora = "Assets/librerias office/Office Pack/Models/Printer/Printer/Printer_LOD0.fbx";
        private const string ModeloTelefono = "Assets/Modelos/Telefono.fbx";

        [MenuItem("Solta La Pala/Generar prefabs con sonido")]
        public static void Generar()
        {
            List<string> avisos = new List<string>();
            AsegurarCarpeta();

            int creados = 0;

            creados += Crear("Heladera", () => ArmarConstante(ModeloHeladera, "Heladera.mp3", 12f, avisos), avisos);
            creados += Crear("LuzFluorescente", () => ArmarConstante(ModeloLuz, "LuzFluorescente.mp3", 8f, avisos), avisos);
            creados += Crear("Microondas", () => ArmarInteractuable(ModeloMicroondas, "[E] Usar microondas",
                new[] { "Microondas1.mp3", "Microondas2.mp3" }, avisos), avisos);
            creados += Crear("Impresora", () => ArmarInteractuable(ModeloImpresora, "[E] Usar impresora",
                new[] { "Impresora2.mp3" }, avisos), avisos);
            creados += Crear("Telefono", () => ArmarInteractuable(ModeloTelefono, "[E] Usar teléfono",
                new[] { "Telefono.mp3" }, avisos), avisos);
            creados += Crear("TecladoConMouse", () => ArmarTecladoConMouse(avisos), avisos);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string mensaje = $"Prefabs creados en '{Carpeta}': {creados} de 6.";
            if (avisos.Count > 0)
            {
                mensaje += "\n\nAvisos:\n- " + string.Join("\n- ", avisos);
            }

            Debug.Log("[GeneradorPrefabsConSonido] " + mensaje);
            EditorUtility.DisplayDialog("Prefabs con sonido", mensaje, "OK");
        }

        private static void AsegurarCarpeta()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaPadre))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(Carpeta))
            {
                AssetDatabase.CreateFolder(CarpetaPadre, NombreCarpeta);
            }
        }

        // Arma un prefab con la funcion indicada y lo guarda. Devuelve 1 si salio bien.
        private static int Crear(string nombre, System.Func<GameObject> armar, List<string> avisos)
        {
            GameObject raiz = armar();
            if (raiz == null)
            {
                return 0;
            }

            raiz.name = nombre;
            PrefabUtility.SaveAsPrefabAsset(raiz, $"{Carpeta}/{nombre}.prefab");
            Object.DestroyImmediate(raiz);

            return 1;
        }

        // Objeto que suena siempre (heladera, luz): modelo + SonidoObjeto en bucle.
        private static GameObject ArmarConstante(string rutaModelo, string archivoAudio, float distanciaMaxima, List<string> avisos)
        {
            GameObject raiz = InstanciarModelo(rutaModelo, avisos);
            AudioClip clip = CargarAudio(archivoAudio, avisos);
            if (raiz == null || clip == null)
            {
                DestruirSiExiste(raiz);
                return null;
            }

            SonidoObjeto sonido = raiz.AddComponent<SonidoObjeto>();
            sonido.modo = ModoSonidoObjeto.Constante;
            sonido.sonidos = new[] { clip };
            sonido.distanciaMaxima = distanciaMaxima;

            return raiz;
        }

        // Objeto que suena solo al usarlo con [E]: modelo + collider + interaccion.
        private static GameObject ArmarInteractuable(string rutaModelo, string texto, string[] archivosAudio, List<string> avisos)
        {
            GameObject raiz = InstanciarModelo(rutaModelo, avisos);
            List<AudioClip> clips = new List<AudioClip>();

            foreach (string archivo in archivosAudio)
            {
                AudioClip clip = CargarAudio(archivo, avisos);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            if (raiz == null || clips.Count == 0)
            {
                DestruirSiExiste(raiz);
                return null;
            }

            AsegurarCollider(raiz);

            SonidoObjeto sonido = raiz.AddComponent<SonidoObjeto>();
            sonido.modo = ModoSonidoObjeto.AlInteractuar;
            sonido.sonidos = clips.ToArray();

            InteractuableConSonido interactuable = raiz.AddComponent<InteractuableConSonido>();
            interactuable.textoInteraccion = texto;

            return raiz;
        }

        // Teclado y mouse en un solo objeto, con un unico componente que reparte los sonidos.
        private static GameObject ArmarTecladoConMouse(List<string> avisos)
        {
            GameObject teclado = InstanciarModelo(ModeloTeclado, avisos);
            GameObject mouse = InstanciarModelo(ModeloMouse, avisos);

            AudioClip teclado1 = CargarAudio("Teclado1.mp3", avisos);
            AudioClip teclado2 = CargarAudio("Teclado2.mp3", avisos);
            AudioClip clipMouse = CargarAudio("Mouse.mp3", avisos);

            if (teclado == null || mouse == null || teclado1 == null || teclado2 == null || clipMouse == null)
            {
                DestruirSiExiste(teclado);
                DestruirSiExiste(mouse);
                return null;
            }

            GameObject raiz = new GameObject("TecladoConMouse");
            teclado.transform.SetParent(raiz.transform, false);
            mouse.transform.SetParent(raiz.transform, false);

            // El mouse se pone al lado del teclado, apoyado en la misma base.
            Bounds cajaTeclado = CalcularCaja(teclado);
            Bounds cajaMouse = CalcularCaja(mouse);
            const float separacion = 0.05f;

            mouse.transform.position += new Vector3(
                cajaTeclado.max.x + separacion + cajaMouse.extents.x - cajaMouse.center.x,
                cajaTeclado.min.y - cajaMouse.min.y,
                cajaTeclado.center.z - cajaMouse.center.z);

            SonidoTecladoMouse sonidos = raiz.AddComponent<SonidoTecladoMouse>();
            sonidos.sonidosTeclado = new[] { teclado1, teclado2 };
            sonidos.sonidosMouse = new[] { clipMouse };

            return raiz;
        }

        private static GameObject InstanciarModelo(string ruta, List<string> avisos)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (modelo == null)
            {
                avisos.Add($"No se encontro el modelo '{ruta}'.");
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(modelo);
        }

        private static AudioClip CargarAudio(string archivo, List<string> avisos)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + archivo);
            if (clip == null)
            {
                avisos.Add($"No se encontro el audio '{Audio}{archivo}' (si acabas de copiarlo, " +
                           "espera a que Unity termine de importarlo y volve a correr el menu).");
            }

            return clip;
        }

        // Si el modelo no trae ningun collider, le pone una caja del tamano del modelo
        // para que el jugador pueda apuntarlo.
        private static void AsegurarCollider(GameObject raiz)
        {
            if (raiz.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            Bounds caja = CalcularCaja(raiz);
            BoxCollider colision = raiz.AddComponent<BoxCollider>();

            // La caja esta en coordenadas del mundo y el objeto esta en el origen sin
            // rotar ni escalar, asi que coincide con las locales.
            colision.center = raiz.transform.InverseTransformPoint(caja.center);
            colision.size = caja.size;
        }

        private static Bounds CalcularCaja(GameObject objeto)
        {
            Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(objeto.transform.position, Vector3.one * 0.5f);
            }

            Bounds caja = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                caja.Encapsulate(renderers[i].bounds);
            }

            return caja;
        }

        private static void DestruirSiExiste(GameObject objeto)
        {
            if (objeto != null)
            {
                Object.DestroyImmediate(objeto);
            }
        }
    }
}
