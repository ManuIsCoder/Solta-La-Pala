using SoltaLaPala.Dialogue;
using SoltaLaPala.Guardado;
using SoltaLaPala.Interaction;
using SoltaLaPala.Inventory;
using SoltaLaPala.Minijuegos;
using SoltaLaPala.Personajes;
using SoltaLaPala.Tutorial;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoltaLaPala.Menus
{
    internal static class ConfiguradorNivel1
    {
        private const string NombreEscena = "nivel 1";

        public static void Configurar()
        {
            string escena = SceneManager.GetActiveScene().name;
            if (escena == NombreEscena)
            {
                ConfigurarNivelUno();
                return;
            }

            ConfigurarOtrosNiveles(escena);
        }

        private static void ConfigurarNivelUno()
        {
            ConfigurarNpc("NPCMujer3", "Helena", TipoDialogo.Charla);
            ConfigurarNpc("NPCHombre2", "Juan", TipoDialogo.Charla);
            DialogoNPC mario = ConfigurarNpc("NPCHombre4", "Mario", TipoDialogo.Mision);
            DialogoNPC helena = GameObject.Find("Helena")?.GetComponent<DialogoNPC>();

            DatosItem pala = Resources.Load<DatosItem>("Items/Pala");

            if (pala == null)
            {
                Debug.LogError("[Nivel 1] No se encontró el item Pala en Resources/Items.");
                return;
            }

            ConfigurarMision(mario, pala, "¡Gracias! Justo lo que necesitaba.");
            EstadoPartida.Instancia?.ConfigurarMisionDiscurso(true);
            ConfigurarTarea(mario, new DatosTarea(
                "EntregaPala", "Entrega la pala a Mario",
                "Necesito una pala para terminar un arreglo. ¿Me la puedes traer?",
                "¿Pudiste encontrar la pala que te pedí?",
                "¡Gracias! Ya puedo terminar el arreglo."));
            ConfigurarTarea(helena, new DatosTarea(
                nameof(MinijuegoDiscurso), "Sabotea el discurso en la computadora",
                "El discurso de la oficina es demasiado aburrido. Arréglalo en la computadora.",
                "¿Ya pudiste cambiar el discurso de la computadora?",
                "¡Ese discurso no volverá a ser el mismo!"));
            ConfigurarPanel(new[] { "EntregaPala", nameof(MinijuegoDiscurso) });

            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador == null)
            {
                Debug.LogError("[Nivel 1] No se encontró al jugador para colocar los objetos del encargo.");
                return;
            }

            CrearObjetoRecogible(pala, jugador.transform, 0f);
        }

        private static void ConfigurarOtrosNiveles(string escena)
        {
            DatosTarea[] tareas = escena switch
            {
                "nivel 2" => new[]
                {
                    new DatosTarea(nameof(MinijuegoBorrarDatos), "Borra los archivos de la computadora",
                        "Hay archivos que no deberían seguir en esa computadora. ¿Puedes encargarte?",
                        "¿Pudiste revisar los archivos de la computadora?",
                        "Perfecto, ya quedó resuelto lo de la computadora."),
                    new DatosTarea(nameof(MinijuegoMicroondas), "Arruina el almuerzo en el microondas",
                        "Dejé mi almuerzo en el microondas. ¿Puedes hacer algo para que nadie lo quiera comer?",
                        "¿Qué pasó con el almuerzo que dejé en el microondas?",
                        "¡Ese almuerzo ya no se lo va a comer nadie!")
                },
                "nivel 3" => new[]
                {
                    new DatosTarea(nameof(MinijuegoFotocopiar), "Haz la fotocopia que te encargaron",
                        "Necesito una copia para la reunión. Usa la impresora y deja listo el trabajo.",
                        "¿Ya terminaste la fotocopia que te encargué?",
                        "Gracias, ya tengo la copia para la reunión."),
                    new DatosTarea(nameof(MinijuegoRedactarDocumento), "Termina el documento de la oficina",
                        "El documento quedó a medias en la computadora. ¿Puedes terminarlo?",
                        "¿Pudiste terminar el documento?",
                        "Gracias, ya puedo enviar el documento.")
                },
                "nivel 4" => new[]
                {
                    new DatosTarea(nameof(MinijuegoCafetera), "Prepara el café de la oficina",
                        "¿Puedes prepararme un café en la cafetera? Lo necesito para seguir trabajando.",
                        "¿Ya está listo el café?",
                        "¡Gracias por el café!"),
                    new DatosTarea(nameof(MinijuegoDestaparBaño), "Destapa el baño",
                        "El baño está tapado y nadie se está haciendo cargo. ¿Puedes destaparlo?",
                        "¿Pudiste arreglar el baño?",
                        "¡Menos mal! Ya se puede usar el baño."),
                    new DatosTarea(nameof(MinijuegoRegarPlanta), "Atiende la planta de la oficina",
                        "La planta de la oficina necesita atención. ¿Puedes ocuparte de ella?",
                        "¿Cómo quedó la planta?",
                        "Gracias, la planta ya está atendida.")
                },
                _ => Array.Empty<DatosTarea>()
            };

            if (tareas.Length == 0)
            {
                return;
            }

            List<DialogoNPC> npcDisponibles = new List<DialogoNPC>(
                UnityEngine.Object.FindObjectsByType<DialogoNPC>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            npcDisponibles.Sort((a, b) =>
            {
                bool aSentado = a.GetComponent<ModeloPersonaje>()?.sentado ?? false;
                bool bSentado = b.GetComponent<ModeloPersonaje>()?.sentado ?? false;
                return bSentado.CompareTo(aSentado);
            });

            MinijuegoBase[] minijuegos = UnityEngine.Object.FindObjectsByType<MinijuegoBase>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<string> ids = new List<string>();
            for (int i = 0; i < tareas.Length; i++)
            {
                bool actividadExiste = false;
                foreach (MinijuegoBase minijuego in minijuegos)
                {
                    if (minijuego != null && minijuego.GetType().Name == tareas[i].id)
                    {
                        actividadExiste = true;
                        break;
                    }
                }

                if (!actividadExiste)
                {
                    Debug.LogError($"[Nivel] No se encontró la actividad '{tareas[i].id}' en la escena {escena}.");
                    continue;
                }

                if (ids.Count >= npcDisponibles.Count)
                {
                    Debug.LogError($"[Nivel] No hay suficientes NPC para asignar todas las tareas de {escena}.");
                    break;
                }

                ConfigurarTarea(npcDisponibles[i], tareas[i]);
                ids.Add(tareas[i].id);
            }

            if (ids.Count > 0)
            {
                ConfigurarPanel(ids.ToArray());
            }
            else
            {
                Debug.LogError($"[Nivel] No se pudieron configurar tareas para {escena}.");
            }
        }

        private static void ConfigurarTarea(DialogoNPC npc, DatosTarea datos)
        {
            if (npc == null)
            {
                Debug.LogError($"[Nivel] No se encontró un NPC para entregar la tarea '{datos.descripcion}'.");
                return;
            }

            TareaNivel tarea = npc.GetComponent<TareaNivel>();
            if (tarea == null)
            {
                tarea = npc.gameObject.AddComponent<TareaNivel>();
            }

            tarea.Configurar(datos.id, datos.descripcion, datos.encargo,
                datos.seguimiento, datos.completado);
        }

        private static void ConfigurarPanel(string[] ids)
        {
            PanelTareasHUD panel = UnityEngine.Object.FindFirstObjectByType<PanelTareasHUD>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogError("[Nivel] No se encontró el panel de tareas para configurar los objetivos.");
                return;
            }

            string[] nombres = new string[ids.Length];
            TareaNivel[] tareasConfiguradas = UnityEngine.Object.FindObjectsByType<TareaNivel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < ids.Length; i++)
            {
                foreach (TareaNivel tarea in tareasConfiguradas)
                {
                    if (tarea.Id == ids[i])
                    {
                        nombres[i] = tarea.Descripcion;
                        break;
                    }
                }
            }

            panel.ConfigurarTareas(nombres, ids);
        }

        private readonly struct DatosTarea
        {
            public readonly string id;
            public readonly string descripcion;
            public readonly string encargo;
            public readonly string seguimiento;
            public readonly string completado;

            public DatosTarea(string id, string descripcion, string encargo,
                string seguimiento, string completado)
            {
                this.id = id;
                this.descripcion = descripcion;
                this.encargo = encargo;
                this.seguimiento = seguimiento;
                this.completado = completado;
            }
        }

        private static void ConfigurarMision(DialogoNPC npc, DatosItem item, string respuesta)
        {
            if (npc == null)
            {
                return;
            }

            MisionEntrega mision = npc.GetComponent<MisionEntrega>();
            if (mision == null)
            {
                mision = npc.gameObject.AddComponent<MisionEntrega>();
            }

            mision.itemCorrecto = item;
            mision.itemsSaboteados = System.Array.Empty<DatosItem>();
            mision.respuestaCorrecta = respuesta;
            mision.respuestaSaboteada = string.Empty;
        }

        private static DialogoNPC ConfigurarNpc(string nombreObjeto, string nombreVisible, TipoDialogo tipoInicial)
        {
            GameObject objeto = GameObject.Find(nombreObjeto);
            if (objeto == null)
            {
                objeto = GameObject.Find(nombreVisible);
                if (objeto == null)
                {
                    Debug.LogError($"[Nivel 1] No se encontró el NPC '{nombreObjeto}'.");
                    return null;
                }
            }

            DialogoNPC dialogo = objeto.GetComponent<DialogoNPC>();
            if (dialogo == null)
            {
                Debug.LogError($"[Nivel 1] El NPC '{nombreObjeto}' no tiene DialogoNPC.");
                return null;
            }

            dialogo.nombre = nombreVisible;
            objeto.name = nombreVisible;
            dialogo.EstablecerTipoDialogoInicial(tipoInicial);
            return dialogo;
        }

        private static void CrearObjetoRecogible(DatosItem item, Transform jugador, float desplazamientoLateral)
        {
            string nombreObjeto = $"RecogibleNivel1_{item.Id}";
            if (GameObject.Find(nombreObjeto) != null)
            {
                return;
            }

            if (item.prefabEnMano == null)
            {
                Debug.LogError($"[Nivel 1] El item '{item.nombre}' no tiene prefab visual asignado.");
                return;
            }

            Vector3 posicion = jugador.position
                + jugador.forward * 1.5f
                + jugador.right * desplazamientoLateral
                + Vector3.up * 2f;

            RaycastHit[] impactos = Physics.RaycastAll(
                posicion, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
            float distanciaSuelo = float.MaxValue;
            foreach (RaycastHit impacto in impactos)
            {
                if (impacto.transform != jugador && !impacto.transform.IsChildOf(jugador)
                    && impacto.distance < distanciaSuelo)
                {
                    distanciaSuelo = impacto.distance;
                    posicion = impacto.point;
                }
            }

            if (distanciaSuelo == float.MaxValue)
            {
                posicion.y = jugador.position.y + 0.15f;
            }

            GameObject objeto = new GameObject(nombreObjeto);
            objeto.SetActive(false);
            objeto.transform.position = posicion;
            GameObject visual = UnityEngine.Object.Instantiate(item.prefabEnMano, objeto.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            SphereCollider collider = objeto.AddComponent<SphereCollider>();
            collider.center = Vector3.up * 0.35f;
            collider.radius = 0.45f;

            IdentificadorObjeto identificador = objeto.AddComponent<IdentificadorObjeto>();
            identificador.AsignarId($"Nivel1_{item.Id}");

            objeto.AddComponent<ResaltadoInteractuable>();
            ObjetoRecogible recogible = objeto.AddComponent<ObjetoRecogible>();
            recogible.item = item;
            objeto.SetActive(true);
        }
    }
}
