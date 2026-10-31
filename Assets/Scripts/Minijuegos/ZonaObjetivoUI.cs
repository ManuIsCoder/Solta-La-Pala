using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoltaLaPala.Minijuegos
{
    public class ZonaObjetivoUI : MonoBehaviour, IDropHandler
    {
        [Header("Filtro de Aceptación")]
        [Tooltip("Dejar vacío para aceptar cualquier arrastrable, o poner el idTipo (ej: 'Taza', 'Sandwich', 'Destapador', etc)")]
        public string idTipoAceptado;

        public event Action<ArrastrableUI> AlSoltarObjeto;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            ArrastrableUI arrastrable = eventData.pointerDrag.GetComponent<ArrastrableUI>();
            if (arrastrable != null)
            {
                if (string.IsNullOrEmpty(idTipoAceptado) || arrastrable.idTipo == idTipoAceptado)
                {
                    AlSoltarObjeto?.Invoke(arrastrable);
                }
            }
        }
    }
}