using System;

namespace RowMate.Core.Common
{
    // Excepciones de los suscriptores de ObservableValue y EventStream (ADR 004, QA-V-01 de la It. 1).
    // Semántica del legacy: ChangeNotifier.notifyListeners atrapa la excepción de cada listener, la reporta
    // (FlutterError.reportError) y sigue con los demás; StreamController.broadcast manda el error de un listener a la zona
    // sin cortar a los otros. Acá se registra en el IAppLogger del observable (nivel Error, tag STATE) y se sigue.
    // Sin estado estático: cada observable recibe su logger por constructor (ARCHITECTURE §4.1, sin singletons).
    public static class SubscriberErrors
    {
        // Tag del log (sin corchetes, como los del legacy).
        public const string Tag = "STATE";

        // Solo corre cuando un suscriptor lanzó: arma el texto acá para que emitir siga sin reservar memoria.
        internal static void Report(IAppLogger logger, string source, Type valueType, Exception error)
        {
            logger.Log(LogLevel.Error, Tag,
                "un suscriptor de " + source + "<" + valueType.Name + "> lanzó " + error.GetType().Name + ": " + error.Message, error);
        }
    }
}
