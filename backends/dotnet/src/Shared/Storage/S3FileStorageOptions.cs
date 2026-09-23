namespace StackBraid.Shared.Storage;

public sealed class S3FileStorageOptions
{
    public const string SectionName = "Storage:S3";

    /// <summary>Endpoint URL for S3 or MinIO service (e.g. "http://localhost:9000").</summary>
    public string ServiceUrl { get; set; } = "http://localhost:9000";

    /// <summary>Target bucket name.</summary>
    public string BucketName { get; set; } = "stackbraid";

    /// <summary>Access key for authentication.</summary>
    public string AccessKey { get; set; } = "minioadmin";

    /// <summary>Secret key for authentication.</summary>
    public string SecretKey { get; set; } = "minioadmin";

    /// <summary>AWS region (default "us-east-1").</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Forces path-style addressing (e.g. http://endpoint/bucket/key), required by MinIO and local emulators.</summary>
    public bool ForcePathStyle { get; set; } = true;
}
