using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>قسم EvidenceFiles في appsettings.json: جذر شجرة الأدلة على القرص الدائم.</summary>
    public sealed class EvidenceFilesOptions
    {
        public const string SectionName = "EvidenceFiles";

        public string Root { get; set; } = "";
    }

    /// <summary>
    /// بايتات الأدلة على قرص التطبيق نفسه. المفتاح {جهة}/{بند}/{اسم مولّد}{امتداد} هو نفسه الذي كان
    /// في الدلو الخارجي، فصفوف المخزن لا تتغيّر قيمتها بالانتقال: الجذر وحده يُضبط بالإعداد.
    /// والاسم مولّد عشوائي فلا يكتب ملفان متماثلَا الاسم فوق بعضهما، والامتداد يُنقّى لأن ما يأتي
    /// من المتصفح ليس ثقة.
    /// </summary>
    public sealed class LocalEvidenceStorage
    {
        private readonly string root;

        public LocalEvidenceStorage(IOptions<EvidenceFilesOptions> options, IWebHostEnvironment environment)
        {
            var configured = options.Value.Root;

            root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "evidence")
                : configured);
        }

        public string RootPath => root;

        public async Task<string> PutAsync(
            string departmentId,
            int auditItemId,
            string extension,
            byte[] bytes,
            CancellationToken cancellationToken = default)
        {
            var key = $"{Sanitize(departmentId)}/{auditItemId}/{Guid.NewGuid():N}{Sanitize(extension)}";
            var path = Resolve(key);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, bytes, cancellationToken);

            return key;
        }

        /// <summary>مفتاح غير موجود بلا استثناء: الحذف سبق القراءة أحياناً، والصفحة تردّ بأربعٍ لا وجدت.</summary>
        public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var path = Resolve(key);
            if (!File.Exists(path))
                return null;

            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Task.CompletedTask;

            var path = Resolve(key);
            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

        /// <summary>المفتاح يبقى داخل الجذر: ما يأتي من صف المخزن لا يُفتح به مسارٌ خارجه.</summary>
        private string Resolve(string key)
        {
            var path = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));

            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException($"مفتاح دليل خارج جذر التخزين: {key}");

            return path;
        }

        private static string Sanitize(string value) =>
            string.Concat(value.Where(c => !Path.GetInvalidFileNameChars().Contains(c) && c != '/' && c != '\\'));
    }
}