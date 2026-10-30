using System;

namespace RowMate.Core.Common
{
    // Con qué se supo que el equipo durmió (P-17 de la 4a; 02-tech-design §2.9 de la It. 4b; ADR 020): el aviso del sistema
    // ("se suspende" / "reanuda"), la contabilidad de sueño del núcleo (la medida) o el salto de la hora de pared con un aviso
    // pendiente.
    [Flags]
    public enum SleepEvidence
    {
        None = 0,
        Signal = 1,
        Measure = 2,
        WallJump = 4,
    }

    // Un despertar del equipo: lo que durmió según el sistema (0 si solo llegó el aviso y no hay medida), lo que le restó el
    // reloj de la app al monótono crudo y con qué evidencia. Lo avisan FrameScheduler.Woke y IAppLifecycle.Woke.
    public readonly struct SystemWake
    {
        public SystemWake(double asleepSeconds, double discountedSeconds, SleepEvidence evidence)
        {
            AsleepSeconds = asleepSeconds;
            DiscountedSeconds = discountedSeconds;
            Evidence = evidence;
        }

        public double AsleepSeconds { get; }

        public double DiscountedSeconds { get; }

        public SleepEvidence Evidence { get; }

        // "aviso, medida" para el registro ([WORKOUT] el equipo durmió N s: no cuenta (P-17; aviso, medida)).
        public string EvidenceText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>(3);
                if ((Evidence & SleepEvidence.Signal) != 0)
                {
                    parts.Add("aviso");
                }

                if ((Evidence & SleepEvidence.Measure) != 0)
                {
                    parts.Add("medida");
                }

                if ((Evidence & SleepEvidence.WallJump) != 0)
                {
                    parts.Add("hora de pared");
                }

                return parts.Count == 0 ? "sin evidencia" : string.Join(", ", parts);
            }
        }
    }

    // La fuente de despertares que nunca avisa: la de ForegroundLifecycle y la implementación por defecto de
    // IAppLifecycle.Woke (Android e iOS no tienen señal en la 4b: la pantalla bloqueada es segundo plano, Q-91).
    public sealed class NeverWakes : IEventSource<SystemWake>
    {
        public static readonly NeverWakes Instance = new NeverWakes();

        private NeverWakes()
        {
        }

        public IDisposable Subscribe(Action<SystemWake> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return Nothing.Instance;
        }

        private sealed class Nothing : IDisposable
        {
            public static readonly Nothing Instance = new Nothing();

            public void Dispose()
            {
            }
        }
    }
}
