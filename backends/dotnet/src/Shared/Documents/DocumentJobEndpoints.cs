using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StackBraid.Shared.Jobs;
using StackBraid.Shared.Storage;

namespace StackBraid.Shared.Documents;

public sealed record DocumentJobResponse(Guid JobId, string Status, string StorageKey);

/// <summary>
/// Minimal API endpoints that queue Excel export and PDF generation as background jobs,
/// returning immediately with an HTTP 202 Accepted response while work executes asynchronously.
/// </summary>
public static class DocumentJobEndpoints
{
    public static IEndpointRouteBuilder MapDocumentJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/documents").WithTags("Documents");

        group.MapPost("/export/excel", QueueExcelExportAsync);
        group.MapPost("/export/pdf", QueuePdfGenerationAsync);

        return app;
    }

    public static async Task<IResult> QueueExcelExportAsync(
        ExcelExportRequest request,
        IJobScheduler jobScheduler,
        IFileStorage storage,
        IExcelExporter exporter,
        CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        var storageKey = $"exports/{jobId}.xlsx";

        jobScheduler.Enqueue(async (services, token) =>
        {
            var bytes = exporter.Export(request);
            using var stream = new MemoryStream(bytes);
            await storage.SaveAsync(storageKey, stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", token).ConfigureAwait(false);
        });

        return Results.Accepted($"/v1/documents/export/{jobId}", new DocumentJobResponse(jobId, "queued", storageKey));
    }

    public static async Task<IResult> QueuePdfGenerationAsync(
        PdfDocumentRequest request,
        IJobScheduler jobScheduler,
        IFileStorage storage,
        IPdfGenerator pdfGenerator,
        CancellationToken cancellationToken)
    {
        var jobId = Guid.NewGuid();
        var storageKey = $"exports/{jobId}.pdf";

        jobScheduler.Enqueue(async (services, token) =>
        {
            var bytes = pdfGenerator.Generate(request);
            using var stream = new MemoryStream(bytes);
            await storage.SaveAsync(storageKey, stream, "application/pdf", token).ConfigureAwait(false);
        });

        return Results.Accepted($"/v1/documents/export/{jobId}", new DocumentJobResponse(jobId, "queued", storageKey));
    }
}
