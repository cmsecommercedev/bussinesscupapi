using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using BussinessCupApi.CloudflareManager;
using System.IO;

public class CloudflareR2Manager
{
    private readonly CloudflareR2Options _options;
    private readonly IAmazonS3 _s3Client;

    public CloudflareR2Manager(IOptions<CloudflareR2Options> options)
    {
        _options = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = _options.Region
        };

        _s3Client = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task UploadFileAsync(string key, Stream fileStream, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            AutoCloseStream = true,
            UseChunkEncoding = false // <-- BU SATIR ZORUNLU
        };

        await _s3Client.PutObjectAsync(request); 
    }
     

    public async Task DeleteFileAsync(string key)
    {
        // Baştaki / varsa temizle
        if (key.StartsWith("/"))
            key = key.Substring(1);


        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key            
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public string GetFileUrl(string key)
    {
        return $"{_options.PublicURL}/{key}";
    }

    public async Task<List<string>> ListFilesAsync()
    {
        var keys = new List<string>();
        string continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                ContinuationToken = continuationToken
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            foreach (var obj in response.S3Objects)
            {
                keys.Add(obj.Key);
            }

            continuationToken = response.IsTruncated ==true ? response.NextContinuationToken : null;

        } while (continuationToken != null);

        return keys;
    }

    public async Task<Stream> DownloadFileAsync(string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key
        };

        var response = await _s3Client.GetObjectAsync(request);

        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0; // okuma için başa sar

        return memoryStream;
    }

}
