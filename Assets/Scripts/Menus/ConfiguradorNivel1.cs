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
                "Necesito una pala para arreglar la maceta de la oficina. ¿Me ayudas a conseguirla?",
                "¿Pudiste encontrar la pala para arreglar la maceta?",
                "¡Gracias! Ahora puedo arreglar la maceta."));
            ConfigurarTarea(helena, new DatosTarea(
                nameof(MinijuegoDiscurso), "Elige un discurso para la oficina",
                "Hay que elegir un discurso para la presentación de la oficina. ¿Me ayudas a seleccionar uno en la computadora?",
                "¿Ya elegiste el discurso para la presentación?",
                "¡El poema a la milanesa fue una elección inolvidable!"), aceptarAutomaticamente: true);
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
                    new DatosTarea(nameof(MinijuegoMicroondas), "Calienta y entrega el sándwich a Lucía",
                        "¿Puedes calentarme un sándwich en el microondas y traérmelo cuando esté listo?",
                        "¿Ya está listo mi sándwich caliente?",
                        "¡Gracias por traerlo caliente!",
                        "NPCMujer1"),
                    new DatosTarea(nameof(MinijuegoBorrarDatos), "Descarta los archivos de la laptop del escritorio con la foto del gato",
                        "Descarta los archivos de la laptop que está en el escritorio con la foto del gato. Solo borra los archivos personales.",
                        "¿Pudiste descartar los archivos personales de la laptop del escritorio con la foto del gato?",
                        "Perfecto, ya quedó ordenado el escritorio de la laptop.",
                        "NPCHombre3")
                },
                "nivel 3" => new[]
                {
                    new DatosTarea(nameof(MinijuegoFotocopiar), "Entrega la fotocopia a Ramiro",
                        "Necesito una copia para la reunión. ¿Puedes usar la fotocopiadora y traerme el resultado?",
                        "¿Pudiste traerme la copia para la reunión?",
                        "Gracias por traerme la copia para la reunión.",
                        "NPCHombre1"),
                    new DatosTarea(nameof(MinijuegoRedactarDocumento), "Entrega el documento a Clara",
                        "¿Puedes completar el documento que dejé en el papel del escritorio de al lado y traerme el resultado?",
                        "¿Pudiste traerme el documento del papel que dejé en el escritorio de al lado?",
                        "Gracias por traerme el documento; ya puedo enviarlo.",
                        "NPCMujer4")
                },
                "nivel 4" => new[]
                {
                    new DatosTarea(nameof(MinijuegoCafetera), "Prepara el café para Diego",
                        "¿Puedes prepararme un café en la cafetera? Lo necesito para seguir trabajando.",
                        "¿Ya está listo el café?",
                        "¡Gracias por el café!",
                        "NPCHombre3"),
                    new DatosTarea(nameof(MinijuegoDestaparBaño), "Destapa el baño para Valentina",
                        "El baño está tapado y nadie se está haciendo cargo. ¿Puedes destaparlo?",
                        "¿Pudiste arreglar el baño?",
                        "¡Menos mal! Ya se puede usar el baño.",
                        "NPCMujer3"),
                    new DatosTarea(nameof(MinijuegoRegarPlanta), "Riega la planta para Sofía",
                        "La planta de la oficina necesita atención. ¿Puedes ocuparte de ella?",
                        "¿Cómo quedó la planta?",
                        "Gracias, la planta ya está atendida.",
                        "NPCMujer1")
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

            if (escena == "nivel 2")
            {
                foreach (DialogoNPC npc in npcDisponibles)
                {
                    ConfigurarIdentidadNpcNivelDos(npc);
                }
            }
            else if (escena == "nivel 3")
            {
                ImpresoraInteractuable impresora =
                    UnityEngine.Object.FindAnyObjectByType<ImpresoraInteractuable>(
                        FindObjectsInactive.Include);
                if (impresora == null)
                {
                    Debug.LogError("[Nivel 3] No se encontró una impresora para configurar la entrega de la carpeta.");
                }
                else
                {
                    impresora.ReiniciarImpresora();
                    impresora.retirarCarpetaAlInteractuar = true;
                    impresora.bloquearDespuesDeColocarDocumento = true;
                }

                foreach (DialogoNPC npc in npcDisponibles)
                {
                    ConfigurarIdentidadNpcNivelTres(npc);
                }
            }
            else if (escena == "nivel 4")
            {
                foreach (DialogoNPC npc in npcDisponibles)
                {
                    ConfigurarIdentidadNpcNivelCuatro(npc);
                }

                ConfigurarSospechaSabotajeNivelCuatro();
            }

            MinijuegoBase[] minijuegos = UnityEngine.Object.FindObjectsByType<MinijuegoBase>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<string> ids = new List<string>();
            HashSet<DialogoNPC> npcsAsignados = new HashSet<DialogoNPC>();
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

                DialogoNPC npc = string.IsNullOrEmpty(tareas[i].npcId)
                    ? npcDisponibles.Find(disponible => !npcsAsignados.Contains(disponible))
                    : npcDisponibles.Find(disponible => disponible.id == tareas[i].npcId);
                if (npc == null || npcsAsignados.Contains(npc))
                {
                    Debug.LogError($"[Nivel] No hay un NPC disponible para asignar la tarea '{tareas[i].descripcion}' en {escena}.");
                    continue;
                }

                ConfigurarTarea(npc, tareas[i]);
                if (escena == "nivel 3")
                {
                    ConfigurarEntregaResultadoNivelTres(npc, tareas[i].id);
                }

                if (escena == "nivel 2" && npc.id == "NPCMujer1")
                {
                    DatosItem sandwichCaliente = Resources.Load<DatosItem>("Items/SandwichCaliente");
                    if (sandwichCaliente == null)
                    {
                        Debug.LogError("[Nivel 2] No se encontró Items/SandwichCaliente para el encargo de Lucía.");
                    }
                    else
                    {
                        ConfigurarMision(npc, sandwichCaliente, "¡Gracias por traerlo caliente!");
                        npc.EstablecerTipoDialogoInicial(TipoDialogo.Mision);
                    }
                }
                npcsAsignados.Add(npc);
                ids.Add(tareas[i].id);
            }

            if (escena == "nivel 4")
            {
                ConfigurarRegaloSoreteNivelCuatro(npcDisponibles);
            }

            if (escena == "nivel 2")
            {
                foreach (DialogoNPC npc in npcDisponibles)
                {
                    npc.gameObject.name = npc.nombre;
                }
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

        private static void ConfigurarIdentidadNpcNivelDos(DialogoNPC npc)
        {
            switch (npc.id)
            {
                case "NPCHombre3":
                    npc.nombre = "Bruno";
                    break;
                case "NPCMujer1":
                    npc.nombre = "Lucía";
                    break;
                case "NPCMujer4":
                    npc.nombre = "Marta";
                    break;
                default:
                    Debug.LogWarning($"[Nivel 2] El NPC '{npc.name}' no tiene un nombre configurado.");
                    npc.nombre = "Compañero";
                    npc.id = "Ejemplo";
                    break;
            }

            npc.EstablecerTipoDialogoInicial(TipoDialogo.Charla);
        }

        private static void ConfigurarIdentidadNpcNivelTres(DialogoNPC npc)
        {
            switch (npc.id)
            {
                case "NPCHombre1":
                    npc.nombre = "Ramiro";
                    break;
                case "NPCMujer4":
                    npc.nombre = "Clara";
                    break;
                default:
                    return;
            }

            npc.gameObject.name = npc.nombre;
            npc.EstablecerTipoDialogoInicial(TipoDialogo.Mision);
            npc.UsarDialogoTareaEnMision();
        }

        private static void ConfigurarIdentidadNpcNivelCuatro(DialogoNPC npc)
        {
            string nombre;
            switch (npc.id)
            {
                case "NPCHombre3":
                    nombre = "Diego";
                    break;
                case "NPCMujer3":
                    nombre = "Valentina";
                    break;
                case "NPCMujer1":
                    nombre = "Sofía";
                    break;
                case "NPCHombre4":
                    nombre = "Tomás";
                    break;
                default:
                    return;
            }

            npc.nombre = nombre;
            npc.gameObject.name = nombre;
            npc.EstablecerTipoDialogoInicial(TipoDialogo.Charla);
        }

        private static void ConfigurarSospechaSabotajeNivelCuatro()
        {
            int sospechaPorSabotaje = Mathf.CeilToInt(
                (EstadoPartida.Instancia != null ? EstadoPartida.Instancia.SospechaMaxima : 100) * 0.2f);

            MinijuegoCafetera cafetera = UnityEngine.Object.FindAnyObjectByType<MinijuegoCafetera>(
                FindObjectsInactive.Include);
            if (cafetera != null)
            {
                cafetera.sospechaSabotaje = sospechaPorSabotaje;
            }

            MinijuegoRegarPlanta planta = UnityEngine.Object.FindAnyObjectByType<MinijuegoRegarPlanta>(
                FindObjectsInactive.Include);
            if (planta != null)
            {
                planta.sospechaSabotaje = sospechaPorSabotaje;
            }
        }

        private static void ConfigurarRegaloSoreteNivelCuatro(List<DialogoNPC> npcDisponibles)
        {
            DialogoNPC npc = npcDisponibles.Find(disponible => disponible.id == "NPCHombre4");
            DatosItem sorete = Resources.Load<DatosItem>("Items/Sorete");
            if (npc == null || sorete == null)
            {
                Debug.LogError("[Nivel 4] No se pudo configurar el regalo del sorete para NPCHombre4.");
                return;
            }

            ConfigurarTarea(npc, new DatosTarea(
                "RegalarSorete", "Regálale el sorete a Tomás",
                "Cuando destapes el baño, ¿me regalarías el sorete que encuentres?",
                "¿Encontraste el sorete para regalarme?",
                "¡Gracias por el regalo! No preguntes para qué lo quiero.",
                "NPCHombre4"), requeridaParaNivel: false);
            ConfigurarMision(npc, sorete, "¡Gracias por el regalo! No preguntes para qué lo quiero.");
            MisionEntrega mision = npc.GetComponent<MisionEntrega>();
            mision.sospechaQueQuitaCumplir = 0;
            mision.textoAccionEntrega = "Regalar";
            npc.EstablecerTipoDialogoInicial(TipoDialogo.Mision);
            npc.UsarDialogoTareaEnMision();
        }

        private static void ConfigurarEntregaResultadoNivelTres(DialogoNPC npc, string idTarea)
        {
            string idResultadoCorrecto;
            string idResultadoSaboteado;
            string respuestaCorrecta;
            string respuestaSaboteada;

            if (idTarea == nameof(MinijuegoFotocopiar))
            {
                idResultadoCorrecto = "Carpeta";
                idResultadoSaboteado = "CarpetaSaboteada";
                respuestaCorrecta = "Gracias por traerme los informes fotocopiados.";
                respuestaSaboteada = "¿Por qué hay una foto en lugar de los informes?";

                MinijuegoFotocopiar minijuego = UnityEngine.Object.FindAnyObjectByType<MinijuegoFotocopiar>(
                    FindObjectsInactive.Include);
                if (minijuego != null)
                {
                    minijuego.aplicarPuntajeAlEntregarAlNPC = true;
                    minijuego.completarTareaAlEntregarAlNPC = true;
                }
            }
            else if (idTarea == nameof(MinijuegoRedactarDocumento))
            {
                idResultadoCorrecto = "Documento";
                idResultadoSaboteado = "Dibujo";
                respuestaCorrecta = "Gracias por traerme el documento redactado.";
                respuestaSaboteada = "Esto es un dibujo, no el documento que necesitaba.";

                MinijuegoRedactarDocumento minijuego =
                    UnityEngine.Object.FindAnyObjectByType<MinijuegoRedactarDocumento>(
                        FindObjectsInactive.Include);
                if (minijuego != null)
                {
                    minijuego.aplicarPuntajeAlEntregarAlNPC = true;
                    minijuego.completarTareaAlEntregarAlNPC = true;
                }
            }
            else
            {
                return;
            }

            DatosItem resultadoCorrecto = Resources.Load<DatosItem>("Items/" + idResultadoCorrecto);
            DatosItem resultadoSaboteado = Resources.Load<DatosItem>("Items/" + idResultadoSaboteado);
            if (resultadoCorrecto == null || resultadoSaboteado == null)
            {
                Debug.LogError($"[Nivel 3] No se encontraron los resultados '{idResultadoCorrecto}' y/o '{idResultadoSaboteado}'.");
                return;
            }

            ConfigurarMision(npc, resultadoCorrecto, respuestaCorrecta);
            MisionEntrega mision = npc.GetComponent<MisionEntrega>();
            mision.itemsSaboteados = new[] { resultadoSaboteado };
            mision.respuestaSaboteada = respuestaSaboteada;

            int sospechaPorEntrega = Mathf.FloorToInt(
                (EstadoPartida.Instancia != null ? EstadoPartida.Instancia.SospechaMaxima : 100) * 0.25f);
            mision.sospechaQueQuitaCumplir = sospechaPorEntrega;
            mision.impactoPorSabotaje = 3;
            mision.sospechaPorSabotaje = sospechaPorEntrega;
        }

        private static void ConfigurarTarea(DialogoNPC npc, DatosTarea datos,
            bool aceptarAutomaticamente = false, bool requeridaParaNivel = true)
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
                datos.seguimiento, datos.completado, aceptarAutomaticamente, requeridaParaNivel);
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
            public readonly string npcId;

            public DatosTarea(string id, string descripcion, string encargo,
                string seguimiento, string completado, string npcId = null)
            {
                this.id = id;
                this.descripcion = descripcion;
                this.encargo = encargo;
                this.seguimiento = seguimiento;
                this.completado = completado;
                this.npcId = npcId;
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

        private static void CrearObjetoRecogible(DatosItem item, Transform jugador,
            float desplazamientoLateral, string prefijoId = "Nivel1")
        {
            string nombreObjeto = $"Recogible{prefijoId}_{item.Id}";
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
            identificador.AsignarId($"{prefijoId}_{item.Id}");

            objeto.AddComponent<ResaltadoInteractuable>();
            ObjetoRecogible recogible = objeto.AddComponent<ObjetoRecogible>();
            recogible.item = item;
            objeto.SetActive(true);
        }
    }
}
