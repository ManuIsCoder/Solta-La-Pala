using UnityEngine;

namespace SoltaLaPala.Menus
{
    public class TareaNivel : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string descripcion;
        [SerializeField, TextArea] private string dialogoEncargo;
        [SerializeField, TextArea] private string dialogoSeguimiento;
        [SerializeField, TextArea] private string dialogoCompletado;

        private bool accionRealizada;

        public string Id => id;
        public string Descripcion => descripcion;
        public bool Aceptada { get; private set; }
        public bool Completada { get; private set; }

        public void Configurar(string idTarea, string nombre, string encargo,
            string seguimiento, string completado)
        {
            id = idTarea;
            descripcion = nombre;
            dialogoEncargo = encargo;
            dialogoSeguimiento = seguimiento;
            dialogoCompletado = completado;
            accionRealizada = false;
            Aceptada = false;
            Completada = false;
        }

        public void Aceptar()
        {
            Aceptada = true;
            Completada = accionRealizada;
        }

        public bool Completar()
        {
            if (!Aceptada || Completada)
            {
                accionRealizada = true;
                return false;
            }

            accionRealizada = true;
            Completada = true;
            return true;
        }

        public void Reiniciar()
        {
            Aceptada = false;
            Completada = false;
            accionRealizada = false;
        }

        public string ObtenerDialogo()
        {
            if (Completada)
            {
                return dialogoCompletado;
            }

            return Aceptada ? dialogoSeguimiento : dialogoEncargo;
        }
    }
}
