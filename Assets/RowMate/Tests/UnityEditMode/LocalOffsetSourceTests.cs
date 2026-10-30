using System;
using NUnit.Framework;
using RowMate.Platform.Time;
using UnityEngine;

namespace RowMate.Tests.UnityEditMode
{
    // LOS · El offset de la zona sin la caché de TimeZoneInfo (AC-WKT-19-04; 04b-hud-exr/02-tech-design §2.8.3; DT4b-13): en
    // Windows sale de GetTimeZoneInformation y, sin cambios de zona, es el de TimeZoneInfo.
    public sealed class LocalOffsetSourceTests
    {
        [Test, Category("AC-WKT-19-04")]
        public void WindowsOffsetIsTheSameAsTimeZoneInfoWithoutZoneChanges()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE: solo en el Editor de Windows");
            }

            using (var source = new LocalOffsetSource())
            {
                Assert.That(source.UsesWindowsApi, Is.True);
                var now = DateTimeOffset.UtcNow;
                Assert.That(source.OffsetFor(now), Is.EqualTo(TimeZoneInfo.Local.GetUtcOffset(now)));
            }
        }

        [Test, Category("AC-WKT-19-04")]
        public void OtherPlatformsUseTimeZoneInfo()
        {
            using (var source = new LocalOffsetSource(false))
            {
                Assert.That(source.UsesWindowsApi, Is.False);
                var now = DateTimeOffset.UtcNow;
                Assert.That(source.OffsetFor(now), Is.EqualTo(TimeZoneInfo.Local.GetUtcOffset(now)));
            }
        }

        [Test, Category("AC-WKT-19-04")]
        public void ReadingDoesNotAllocate()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("NO VERIFICABLE: solo en el Editor de Windows");
            }

            using (var source = new LocalOffsetSource())
            {
                var now = DateTimeOffset.UtcNow;
                source.OffsetFor(now);
                // Con el Recorder de "GC.Alloc" del Unity Test Framework: en Unity GC.GetAllocatedBytesForCurrentThread da siempre 0.
                var allocated = new UnityEngine.TestTools.Constraints.AllocatingGCMemoryConstraint().ApplyTo((TestDelegate)(() =>
                {
                    for (var i = 0; i < 100; i++)
                    {
                        source.OffsetFor(now);
                    }
                }));
                Assert.That(allocated.IsSuccess, Is.False, "el búfer se reserva una vez");
            }
        }
    }
}
