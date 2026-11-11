using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Export;

namespace RowMate.Tests.Support
{
    // El exportador de las pruebas y del arnés (It. 12; 02-tech-design §6.2): escribe EN LÍNEA con FolderExportWriter en la carpeta
    // que recibe (siempre una temporal: nunca Documentos ni Descargas) y "abrir carpeta" solo se REGISTRA (nunca abre el
    // Explorador). Con Delivery Share escribe igual y no abre nada.
    // - Hold(): la próxima exportación queda pendiente hasta Release() (CAP-04: el indicador a la vista).
    // - FailNext(error): la próxima exportación falla con ese error, sin escribir (CAP-07, AC-STR-09-21).
    // - Requests: cada pedido recibido (QA-G12-02); Reveals: cada "abrir carpeta"; Files: lo que escribió.
    // - FailReveals(): desde ahí, "abrir carpeta" se registra y lanza RevealFailedException, como el exportador real cuando el
    //   Explorador y su respaldo fallan (It. 15, AC-D15-13, CAP-06).
    public sealed class FakeFileExporter : IFileExporter
    {
        private readonly List<ExportRequest> _requests = new List<ExportRequest>();
        private readonly List<ExportedFile> _reveals = new List<ExportedFile>();
        private readonly List<ExportedFile> _files = new List<ExportedFile>();
        private bool _holdNext;
        private bool _failReveals;
        private Exception? _failNext;
        private ExportRequest? _heldRequest;
        private TaskCompletionSource<ExportedFile>? _held;

        public FakeFileExporter(string directory, string? fixedLabel, ExportDelivery delivery = ExportDelivery.SaveToFolder)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("FakeFileExporter necesita una carpeta temporal.", nameof(directory));
            }

            Location = new ExportLocation(directory, fixedLabel);
            Delivery = delivery;
        }

        public ExportDelivery Delivery { get; }

        public ExportLocation Location { get; }

        ExportLocation? IFileExporter.Location => Location;

        public IReadOnlyList<ExportRequest> Requests => _requests.ToArray();

        public IReadOnlyList<ExportedFile> Reveals => _reveals.ToArray();

        public IReadOnlyList<ExportedFile> Files => _files.ToArray();

        public bool IsHolding => _held != null;

        public void Hold()
        {
            _holdNext = true;
        }

        public void FailReveals()
        {
            _failReveals = true;
        }

        public void FailNext(Exception error)
        {
            _failNext = error ?? throw new ArgumentNullException(nameof(error));
        }

        // Completa la exportación retenida (la continuación corre en línea, como en el exportador real al volver al hilo principal).
        public void Release()
        {
            var held = _held;
            var request = _heldRequest;
            _held = null;
            _heldRequest = null;
            if (held == null || request == null)
            {
                return;
            }

            try
            {
                held.SetResult(Write(request));
            }
            catch (Exception error)
            {
                held.SetException(error);
            }
        }

        public Task<ExportedFile> ExportAsync(ExportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _requests.Add(request);
            if (_failNext != null)
            {
                var error = _failNext;
                _failNext = null;
                return Task.FromException<ExportedFile>(error);
            }

            if (_holdNext)
            {
                _holdNext = false;
                _heldRequest = request;
                _held = new TaskCompletionSource<ExportedFile>();
                return _held.Task;
            }

            try
            {
                return Task.FromResult(Write(request));
            }
            catch (Exception error)
            {
                return Task.FromException<ExportedFile>(error);
            }
        }

        public void Reveal(ExportedFile file)
        {
            _reveals.Add(file);
            if (_failReveals)
            {
                throw new RevealFailedException("no se pudo abrir el Explorador ni el respaldo: " + file.FullPath);
            }
        }

        private ExportedFile Write(ExportRequest request)
        {
            var file = FolderExportWriter.Write(Location, request.BaseName, request.Extension, request.Content());
            _files.Add(file);
            return file;
        }
    }
}
