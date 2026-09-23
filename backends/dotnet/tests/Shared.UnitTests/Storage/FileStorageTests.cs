using System.Net;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using StackBraid.Shared.Storage;

namespace StackBraid.Shared.UnitTests.Storage;

public sealed class FileStorageTests : IDisposable
{
    private readonly string _tempDir;

    public FileStorageTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"stackbraid_test_{Guid.NewGuid():N}");
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
                // Best effort cleanup.
            }
        }
    }

    [Fact]
    public async Task LocalFileStorage_saves_retrieves_and_deletes_files()
    {
        var options = Options.Create(new LocalFileStorageOptions { RootPath = _tempDir });
        var storage = new LocalFileStorage(options);

        var key = "documents/test.txt";
        var content = "Hello StackBraid Storage!"u8.ToArray();

        using (var saveStream = new MemoryStream(content))
        {
            await storage.SaveAsync(key, saveStream, "text/plain");
        }

        var retrievedStream = await storage.GetAsync(key);
        retrievedStream.ShouldNotBeNull();

        using (retrievedStream)
        {
            using var ms = new MemoryStream();
            await retrievedStream.CopyToAsync(ms);
            Encoding.UTF8.GetString(ms.ToArray()).ShouldBe("Hello StackBraid Storage!");
        }

        await storage.DeleteAsync(key);

        var afterDelete = await storage.GetAsync(key);
        afterDelete.ShouldBeNull();
    }

    [Fact]
    public async Task LocalFileStorage_sanitizes_keys_against_directory_traversal()
    {
        var options = Options.Create(new LocalFileStorageOptions { RootPath = _tempDir });
        var storage = new LocalFileStorage(options);

        var traversalKey = "../../escaped.txt";
        using var stream = new MemoryStream("traversal test"u8.ToArray());
        await storage.SaveAsync(traversalKey, stream, "text/plain");

        // The file must be strictly contained inside _tempDir, never outside
        var filesInRoot = Directory.GetFiles(_tempDir, "*escaped.txt*");
        filesInRoot.Length.ShouldBe(1);

        var parentEscaped = Path.Combine(Directory.GetParent(_tempDir)!.FullName, "escaped.txt");
        File.Exists(parentEscaped).ShouldBeFalse();
    }

    [Fact]
    public async Task S3FileStorage_saves_retrieves_and_deletes_objects()
    {
        var s3Mock = Substitute.For<IAmazonS3>();
        var options = Options.Create(new S3FileStorageOptions
        {
            BucketName = "my-bucket",
            ServiceUrl = "http://localhost:9000",
            ForcePathStyle = true,
        });

        var storage = new S3FileStorage(s3Mock, options);

        var key = "exports/report.xlsx";
        var content = "Fake Excel Content"u8.ToArray();

        // 1. Test SaveAsync
        using (var stream = new MemoryStream(content))
        {
            await storage.SaveAsync(key, stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        await s3Mock.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r =>
                r.BucketName == "my-bucket" &&
                r.Key == key &&
                r.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            Arg.Any<CancellationToken>());

        // 2. Test GetAsync (found)
        var responseStream = new MemoryStream(content);
        var getResponse = new GetObjectResponse { ResponseStream = responseStream };
        s3Mock.GetObjectAsync(
            Arg.Is<GetObjectRequest>(r => r.BucketName == "my-bucket" && r.Key == key),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(getResponse));

        var resultStream = await storage.GetAsync(key);
        resultStream.ShouldNotBeNull();
        using (resultStream)
        {
            using var ms = new MemoryStream();
            await resultStream.CopyToAsync(ms);
            Encoding.UTF8.GetString(ms.ToArray()).ShouldBe("Fake Excel Content");
        }

        // 3. Test GetAsync (not found -> null)
        s3Mock.GetObjectAsync(
            Arg.Is<GetObjectRequest>(r => r.Key == "missing.txt"),
            Arg.Any<CancellationToken>()).Returns<GetObjectResponse>(_ =>
                throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

        var missing = await storage.GetAsync("missing.txt");
        missing.ShouldBeNull();

        // 4. Test DeleteAsync
        await storage.DeleteAsync(key);
        await s3Mock.Received(1).DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r => r.BucketName == "my-bucket" && r.Key == key),
            Arg.Any<CancellationToken>());

        // 5. Test DeleteAsync (not found -> no exception)
        s3Mock.DeleteObjectAsync(
            Arg.Is<DeleteObjectRequest>(r => r.Key == "already-deleted.txt"),
            Arg.Any<CancellationToken>()).Returns<DeleteObjectResponse>(_ =>
                throw new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

        await Should.NotThrowAsync(async () => await storage.DeleteAsync("already-deleted.txt"));
    }

    [Fact]
    public void IFileStorage_swappable_between_Local_and_S3()
    {
        var local = new LocalFileStorage(Options.Create(new LocalFileStorageOptions { RootPath = _tempDir }));
        var s3 = new S3FileStorage(Substitute.For<IAmazonS3>(), Options.Create(new S3FileStorageOptions()));

        IFileStorage storage1 = local;
        IFileStorage storage2 = s3;

        storage1.ShouldNotBeNull();
        storage2.ShouldNotBeNull();
    }
}
