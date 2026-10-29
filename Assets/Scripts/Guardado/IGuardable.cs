namespace SoltaLaPala.Guardado
{
    public interface IGuardable
    {
        void Capturar(DatosPartidaGuardada datos);

        void Restaurar(DatosPartidaGuardada datos);
    }
}
