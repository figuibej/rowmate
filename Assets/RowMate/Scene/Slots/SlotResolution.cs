using System.Collections.Generic;
using RowMate.Core.Common;
using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Lo que se usa para un slot (06a-escena-lago/02-tech-design §3.7): el asset (fuente Asset, presente y válido) o el procedural.
    // Reason dice por qué cayó al procedural cuando la fuente era Asset (vacío con la fuente Procedural: no es una falla).
    public readonly struct SlotResolution
    {
        public SlotResolution(string slotId, SlotSource source, Object? asset, string reason)
        {
            SlotId = slotId;
            Source = source;
            Asset = asset;
            Reason = reason;
        }

        public string SlotId { get; }

        public SlotSource Source { get; }

        // El asset a usar, o null = el procedural.
        public Object? Asset { get; }

        public bool UsesProcedural => Asset == null;

        public string Reason { get; }

        public T? As<T>() where T : Object
        {
            return Asset as T;
        }
    }

    // La resolución de los slots (paso 2 del armado, §3.4; QA-G6aT-04): fuente Procedural → el procedural, sin aviso; fuente Asset
    // con el asset presente y válido → se usa; fuente Asset con el asset que falta (una referencia vacía, o un GUID de
    // Assets/ThirdParty/ cuyo archivo no está: Unity lo da como null) o que no valida → el procedural y UN aviso
    // "[SLOT] <id>: <motivo>" por slot y por anfitrión (SlotLog). Nunca un error.
    public static class SlotResolver
    {
        public const string Tag = "SLOT";

        public static SlotResolution Resolve(AssetSlotDefinition? slot, string slotId, SlotLog log)
        {
            if (slot == null)
            {
                log.Warn(slotId, "no hay .asset del slot: se usa el procedural");
                return new SlotResolution(slotId, SlotSource.Procedural, null, "sin .asset");
            }

            if (slot.Source == SlotSource.Procedural)
            {
                return new SlotResolution(slot.SlotId, SlotSource.Procedural, null, "");
            }

            if (slot.AssignedObject == null)
            {
                var reason = "falta el asset asignado" + (slot.ExpectedPath.Length > 0 ? " (" + slot.ExpectedPath + ")" : "") + ": se usa el procedural";
                log.Warn(slot.SlotId, reason);
                return new SlotResolution(slot.SlotId, SlotSource.Asset, null, reason);
            }

            if (!slot.ValidateAssigned(out var invalid))
            {
                var reason = invalid + ": se usa el procedural";
                log.Warn(slot.SlotId, reason);
                return new SlotResolution(slot.SlotId, SlotSource.Asset, null, reason);
            }

            return new SlotResolution(slot.SlotId, SlotSource.Asset, slot.AssignedObject, "");
        }
    }

    // Los avisos de los slots de UN anfitrión de la escena (no estático: lo que ve una prueba no depende del orden en que corren).
    public sealed class SlotLog
    {
        private readonly HashSet<string> _warned = new HashSet<string>();
        private readonly IAppLogger? _logger;
        private readonly List<string> _lines = new List<string>();

        public SlotLog(IAppLogger? logger)
        {
            _logger = logger;
        }

        public IReadOnlyList<string> Lines => _lines;

        public int Warnings => _lines.Count;

        public void Warn(string slotId, string reason)
        {
            if (!_warned.Add(slotId))
            {
                return;
            }

            var line = "[" + SlotResolver.Tag + "] " + slotId + ": " + reason;
            _lines.Add(line);
            if (_logger != null)
            {
                _logger.Log(LogLevel.Warning, SlotResolver.Tag, slotId + ": " + reason);
            }
            else
            {
                Debug.LogWarning(line);
            }
        }
    }
}
