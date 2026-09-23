using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using StackBraid.Shared.Documents;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Storage;

namespace StackBraid.Shared.UnitTests.Documents;

public sealed class DocumentJobQueueTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IFileStorage _storage;

    public DocumentJobQueueTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"stackbraid_jobs_{Guid.NewGuid():N}");
        _storage = new LocalFileStorage(Options.Create(new LocalFileStorageOptions { RootPath = _tempDir }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task Export_runs_as_queued_background_job_and_returns_before_job_completes()
    {
        // Arrange: A controlled job scheduler that gates execution so we can verify
        // that the HTTP handler returns 202 Accepted before the background job completes.
        var scheduler = new ControlledJobScheduler();
        var exporter = new ClosedXmlExcelExporter();
        var request = new ExcelExportRequest(
            Headers: ["ID", "Name", "Role"],
            Rows: [["1", "Alice", "Admin"], ["2", "Bob", "User"]]);

        // Act: Execute the HTTP handler
        var httpResult = await DocumentJobEndpoints.QueueExcelExportAsync(
            request,
            scheduler,
            _storage,
            exporter,
            CancellationToken.None);

        // Assert 1: The HTTP handler returned before the background job completed
        httpResult.ShouldBeOfType<Accepted<DocumentJobResponse>>();
        var acceptedResult = (Accepted<DocumentJobResponse>)httpResult;
        acceptedResult.StatusCode.ShouldBe(StatusCodes.Status202Accepted);

        var jobResponse = acceptedResult.Value;
        jobResponse.ShouldNotBeNull();
        jobResponse.Status.ShouldBe("queued");

        // The background job is enqueued but has NOT completed yet
        scheduler.WasJobEnqueued.ShouldBeTrue();
        scheduler.IsJobCompleted.ShouldBeFalse();

        // The file has not been saved yet while the job is still pending
        var pendingFile = await _storage.GetAsync(jobResponse.StorageKey);
        pendingFile.ShouldBeNull();

        // Act 2: Allow the enqueued background job to run to completion
        scheduler.ReleaseJob();
        await scheduler.WaitForJobCompletionAsync();

        // Assert 2: The job completed and the file exists with valid Excel content
        scheduler.IsJobCompleted.ShouldBeTrue();

        var savedFile = await _storage.GetAsync(jobResponse.StorageKey);
        savedFile.ShouldNotBeNull();

        using (savedFile)
        {
            using var workbook = new XLWorkbook(savedFile);
            var sheet = workbook.Worksheets.First();
            sheet.Cell(1, 1).GetString().ShouldBe("ID");
            sheet.Cell(2, 2).GetString().ShouldBe("Alice");
            sheet.Cell(3, 2).GetString().ShouldBe("Bob");
        }
    }

    [Fact]
    public async Task Pdf_generation_runs_as_queued_background_job_and_returns_before_job_completes()
    {
        // Arrange
        var scheduler = new ControlledJobScheduler();
        var pdfGenerator = new QuestPdfGenerator();
        var request = new PdfDocumentRequest(
            Title: "Invoice #1001",
            Lines: ["Customer: Acme Corp", "Amount: $500.00", "Status: Paid"]);

        // Act: Execute the HTTP handler
        var httpResult = await DocumentJobEndpoints.QueuePdfGenerationAsync(
            request,
            scheduler,
            _storage,
            pdfGenerator,
            CancellationToken.None);

        // Assert 1: The HTTP handler returned before the background job completed
        httpResult.ShouldBeOfType<Accepted<DocumentJobResponse>>();
        var acceptedResult = (Accepted<DocumentJobResponse>)httpResult;
        acceptedResult.StatusCode.ShouldBe(StatusCodes.Status202Accepted);

        var jobResponse = acceptedResult.Value;
        jobResponse.ShouldNotBeNull();
        jobResponse.Status.ShouldBe("queued");

        // The background job is enqueued but has NOT completed yet
        scheduler.WasJobEnqueued.ShouldBeTrue();
        scheduler.IsJobCompleted.ShouldBeFalse();

        // The file has not been saved yet while the job is still pending
        var pendingFile = await _storage.GetAsync(jobResponse.StorageKey);
        pendingFile.ShouldBeNull();

        // Act 2: Allow the enqueued background job to run to completion
        scheduler.ReleaseJob();
        await scheduler.WaitForJobCompletionAsync();

        // Assert 2: The job completed and the PDF exists with valid PDF header
        scheduler.IsJobCompleted.ShouldBeTrue();

        var savedFile = await _storage.GetAsync(jobResponse.StorageKey);
        savedFile.ShouldNotBeNull();

        using (savedFile)
        {
            using var ms = new MemoryStream();
            await savedFile.CopyToAsync(ms);
            var bytes = ms.ToArray();
            bytes.Length.ShouldBeGreaterThan(0);

            var header = Encoding.ASCII.GetString(bytes.Take(5).ToArray());
            header.ShouldBe("%PDF-");
        }
    }

    private sealed class ControlledJobScheduler : IJobScheduler
    {
        private readonly TaskCompletionSource _releaseSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completionSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Func<IServiceProvider, CancellationToken, Task>? _enqueuedJob;

        public bool WasJobEnqueued => _enqueuedJob != null;
        public bool IsJobCompleted => _completionSignal.Task.IsCompleted;

        public void Enqueue(Func<IServiceProvider, CancellationToken, Task> job)
        {
            _enqueuedJob = job;

            // Start running the job asynchronously, but pause inside until released
            _ = Task.Run(async () =>
            {
                await _releaseSignal.Task;
                try
                {
                    var services = new ServiceCollection().BuildServiceProvider();
                    await job(services, CancellationToken.None);
                    _completionSignal.SetResult();
                }
                catch (Exception ex)
                {
                    _completionSignal.SetException(ex);
                }
            });
        }

        public void ReleaseJob() => _releaseSignal.TrySetResult();

        public Task WaitForJobCompletionAsync() => _completionSignal.Task;
    }
}
