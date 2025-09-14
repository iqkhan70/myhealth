using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Config _s3Config;

        public S3Service(IAmazonS3 s3Client, IOptions<S3Config> s3Config)
        {
            _s3Client = s3Client;
            _s3Config = s3Config.Value;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid contentGuid)
        {
            try
            {
                var key = $"{_s3Config.Folder}{contentGuid}_{fileName}";

                var request = new PutObjectRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = key,
                    InputStream = fileStream,
                    ContentType = contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };

                await _s3Client.PutObjectAsync(request);

                return key;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload file to S3: {ex.Message}", ex);
            }
        }

        public async Task<Stream?> GetFileStreamAsync(string s3Key)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = s3Key
                };

                var response = await _s3Client.GetObjectAsync(request);
                return response.ResponseStream;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get file stream from S3: {ex.Message}", ex);
            }
        }

        public async Task<string> GetPresignedUrlAsync(string s3Key, int expirationHours = 24)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = s3Key,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddHours(expirationHours),
                    Protocol = Protocol.HTTPS
                };

                return await _s3Client.GetPreSignedURLAsync(request);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to generate presigned URL: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string s3Key)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = s3Key
                };

                await _s3Client.DeleteObjectAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete file from S3: {ex.Message}", ex);
            }
        }

        public async Task<bool> FileExistsAsync(string s3Key)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _s3Config.BucketName,
                    Key = s3Key
                };

                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to check if file exists in S3: {ex.Message}", ex);
            }
        }
    }

    public class S3Config
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public string Folder { get; set; } = string.Empty;
    }
}
