using UnityEngine;

namespace SoltaLaPala.Interaction
{
    public interface IInteractuable
    {
        Transform Transform { get; }

        string ObtenerTextoInteraccion();

        bool PuedeInteractuar();

        void Interactuar(GameObject quienInteractua);
    }
}
