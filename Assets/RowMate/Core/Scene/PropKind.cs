namespace RowMate.Core.Scene
{
    // Los 15 tipos de objeto de la orilla del legacy (scene/environment.dart:12-15), en ese orden (el índice va en los bytes
    // canónicos de un segmento, 02-tech-design §6.2). Los "hitos" son todos menos LaneBuoy (environment.dart:104).
    public enum PropKind
    {
        Pine,
        Cabin,
        Pier,
        Building,
        Boathouse,
        Bridge,
        LaneBuoy,
        Lamp,
        Cliff,
        Lighthouse,
        BeachUmbrella,
        Grandstand,
        FinishTower,
        Flag,
        DistanceMarker,
    }
}
