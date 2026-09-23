using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace StackBraid.Shared.Storage;

/// <summary>
/// Stores and retrieves blobs in an S3-compatible object store (e.g. AWS S3, MinIO).
/// Supports path-style addressing and custom endpoints.
/// </summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly bool _disposeClient;

    public S3FileStorage(IOptions<S3FileStorageOptions> options)
    {
        var opt = options.Value;
        _bucketName = opt.BucketName;
        var config = new AmazonS3Config
        {
            ServiceURL = opt.ServiceUrl,
            ForcePathStyle = opt.ForcePathStyle,
            AuthenticationRegion = opt.Region,
        };
        _s3Client = new AmazonS3Client(opt.AccessKey, opt.SecretKey, config);
        _disposeClient = true;
    }

    /// <summary>Constructor accepting an existing <see cref="IAmazonS3"/> client for testing and custom lifecycles.</summary>
    public S3FileStorage(IAmazonS3 s3Client, IOptions<S3FileStorageOptions> options)
    {
        _s3Client = s3Client;
        _bucketName = options.Value.BucketName;
        _disposeClient = false;
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
            };

            var response = await _s3Client.GetObjectAsync(getRequest, cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Position = 0;
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
        {
            // S3 deletion of a non-existing object is idempotent.
        }
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _s3Client.Dispose();
        }
    }
}
