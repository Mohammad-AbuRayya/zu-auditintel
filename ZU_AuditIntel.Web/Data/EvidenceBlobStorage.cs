using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// قسم EvidenceBlob في appsettings.json. المفتاح لا يدخل المستودع: اسم الدلو ومسار النهاية
    /// عامّيان، وسرّ الوصول إما في ملف خارج الشجرة أو في متغيّرات بيئة المنصّة المستضيفة.
    /// </summary>
    public sealed class EvidenceBlobOptions
    {
        public const string SectionName = "EvidenceBlob";

        public string Bucket { get; set; } = "";

        /// <summary>نهاية Backblaze B2 المتوافقة مع S3؛ تغيّرها وحده ينقل المخزن إلى مزوّد آخر.</summary>
        public string Endpoint { get; set; } = "https://s3.us-west-004.backblazeb2.com";

        /// <summary>ملف {accessKeyId, secretAccessKey} خارج المستودع، أو يُترك فارغاً فتُقرأ المتغيّرات.</summary>
        public string CredentialsPath { get; set; } = "";
    }

    /// <summary>
    /// مخزن بايتات الأدلة في دلو خاص واحد، والمفتاح شجرةُ الجهة والبند كما كان المسار على القرص،
    /// فقيمة storedPath لا تتغيّر دلالتها عند الانتقال. القرص خرج من المشروع لأن الخادم المستضاف
    /// لا قرص دائم له: كل إعادة نشر تُمحو ما رُفع، فيبقى سجلّ التدقيق بلا أدلته.
    /// </summary>
    public sealed class EvidenceBlobStorage : IDisposable
    {
        private readonly AmazonS3Client s3;
        private readonly string bucket;

        /// <summary>يُبنى عند أول ملف يُمسّ لا عند الإقلاع، فالدلو غير المضبوط يُسقط رفعَ دليلٍ واحد
        /// برسالةٍ تقول ما ينقص، لا الموقعَ كله.</summary>
        public EvidenceBlobStorage(IOptions<EvidenceBlobOptions> options)
        {
            var section = options.Value;

            if (string.IsNullOrWhiteSpace(section.Bucket))
                throw new InvalidOperationException(
                    "لا اسم دلو في EvidenceBlob:Bucket — بلا دلو لا مخزن ملفات، فلا رفع ولا تنزيل.");

            var (accessKeyId, secretAccessKey) = ReadCredentials(section);

            // path-style وحده يعمل مع B2: الدلو جزء من المسار لا من اسم المضيف.
            s3 = new AmazonS3Client(
                new BasicAWSCredentials(accessKeyId, secretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = section.Endpoint,
                    ForcePathStyle = true
                });
            bucket = section.Bucket;
        }

        /// <summary>
        /// مسار المفتاح: {الجهة}/{البند}/{guid}{الامتداد}. الاسم مولَّد عشوائياً فلا يكتب ملفان
        /// متماثلَا الاسم فوق بعضهما، والامتداد يُنقّى لأن ما يأتي من المتصفح ليس ثقة.
        /// </summary>
        public async Task<string> PutAsync(
            string departmentId,
            int auditItemId,
            string extension,
            byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            var key = $"{Sanitize(departmentId)}/{auditItemId}/{Guid.NewGuid():N}{Sanitize(extension)}";

            await using var stream = new MemoryStream(bytes);
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream,
                ContentType = ContentType(extension)
            }, cancellationToken);

            return key;
        }

        /// <summary>مفتاح غير موجود بلا استثناء: الحذف سبق القراءة أحياناً، والصفحة تردّ بأربعٍ لا وجدت.</summary>
        public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            try
            {
                using var response = await s3.GetObjectAsync(
                    new GetObjectRequest { BucketName = bucket, Key = key }, cancellationToken);
                using var buffer = new MemoryStream();
                await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
                return buffer.ToArray();
            }
            catch (AmazonS3Exception error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = key }, cancellationToken);
        }

        /// <summary>
        /// إثبات حيّ واحد: كتابة ثم قراءة ثم مقارنة ثم محو ثم تأكيد أن المحو محقاً. به يعرف صاحبه
        /// أن المفتاح والدلو والصلاحية — بما فيها صلاحية الحذف التي تحتاجها اللوحة عند شطب ملف —
        /// تعمل قبل أن يرفع أول جهة ملفها على شاشة حقيقية.
        /// </summary>
        public async Task<string> ProbeAsync(CancellationToken cancellationToken = default)
        {
            byte[] bytes = "إثبات الاتصال بمخزن الملفات."u8.ToArray();

            var key = await PutAsync("_probe", 0, ".txt", bytes, cancellationToken);
            var back = await GetAsync(key, cancellationToken);
            await DeleteAsync(key, cancellationToken);
            var gone = await GetAsync(key, cancellationToken) is null;

            var round = back is not null && back.SequenceEqual(bytes);

            return round && gone
                ? $"مخزن الملفات يعمل: دلو «{bucket}» قبل الكتابة والحذف، و{bytes.Length} بايتاً ذهبت وعادت متطابقة ثم مُحيت."
                : $"خلل في مخزن الملفات «{bucket}»: عودة البايتات متطابقة={round}، والمحو مؤكَّد={gone}. " +
                  "تحقّق من اسم الدلو وصلاحية المفتاح والمنطقة في EvidenceBlob:Endpoint.";
        }

        public void Dispose() => s3.Dispose();

        private static (string AccessKeyId, string SecretAccessKey) ReadCredentials(EvidenceBlobOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(options.CredentialsPath));
                var root = document.RootElement;
                return (Required(root, "accessKeyId"), Required(root, "secretAccessKey"));
            }

            // متغيّرات البيئة معيارية الأسماء، فهي نفسها التي تضعها منصّة الاستضافة بلا تعديل كود.
            var accessKeyId = Environment.GetEnvironmentVariable("B2_ACCESS_KEY_ID")
                ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? "";
            var secretAccessKey = Environment.GetEnvironmentVariable("B2_SECRET_ACCESS_KEY")
                ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? "";

            if (accessKeyId.Length == 0 || secretAccessKey.Length == 0)
                throw new InvalidOperationException(
                    "لا سرّ وصول لمخزن الملفات: اضبط B2_ACCESS_KEY_ID وB2_SECRET_ACCESS_KEY، أو املأ " +
                    "EvidenceBlob:CredentialsPath بملف مفتاح خارج المستودع.");

            return (accessKeyId, secretAccessKey);
        }

        private static string Required(JsonElement root, string name) =>
            root.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text
                ? text
                : throw new InvalidOperationException($"ملف المفتاح بلا {name}.");

        private static string ContentType(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" or ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        private static string Sanitize(string value) =>
            string.Concat(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c) && c != '/' && c != '\\'));
    }
}
