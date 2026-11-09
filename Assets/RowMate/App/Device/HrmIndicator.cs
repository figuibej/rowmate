namespace RowMate.App.Device
{
    // El indicador de la identidad del sensor recordado (It. 11, A11-02; AC-HRM-07-02, -03): WaitingArc = arco que se llena durante
    // la espera entre intentos; Spinner = intento en curso; LinkOff = no encontrado (sin reintentos). None en el resto de los cuerpos.
    public enum HrmIndicator
    {
        None,
        Spinner,
        WaitingArc,
        LinkOff
    }
}
