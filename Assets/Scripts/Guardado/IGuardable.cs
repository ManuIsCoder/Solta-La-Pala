namespace SoltaLaPala.Guardado
{
    // Lo implementa cualquier sistema que tenga estado que sobreviva a cerrar el juego.
    //
    // Existe para que GestorGuardado no necesite conocer al inventario, al jugador
    // ni a los dialogos: los busca por esta interfaz y les pasa el DTO. Sumar un
    // sistema nuevo al guardado no obliga a tocar el gestor.
    public interface IGuardable
    {
        // Vuelca el estado propio en el DTO. Solo debe escribir sus campos.
        void Capturar(DatosPartidaGuardada datos);

        // Lee del DTO y se pone en ese estado.
        void Restaurar(DatosPartidaGuardada datos);
    }
}
