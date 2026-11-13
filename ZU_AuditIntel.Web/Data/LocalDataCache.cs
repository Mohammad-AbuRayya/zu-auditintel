using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// المخبأ المحلي الحقيقي: ملفان JSON بنفس أسماء حقول وثائق Firestore حرفياً، فتُقرأ منهما
    /// النماذج نفسها التي يولّدها عميل السحابة بلا خريطة أسماء ثانية تُنسى عند التعديل. بناهما
    /// <c>scratch/build_local_cache.py</c> من قاعدة SQLite بخريطة مصدّر الكتالوج نفسها.
    ///
    /// المسار خارج المستودع افتراضياً (<c>~/.config/zu-auditintel/cache</c>): ملف الحسابات يحمل
    /// بصمات كلمات المرور فلا يدخل المستودع ولا صورة النشر، ويُضبط الموقع بـ <c>Firestore:CacheDirectory</c>.
    /// </summary>
    public sealed class LocalDataCache(IOptions<FirestoreOptions> options, ILogger<LocalDataCache> logger)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            // الحقول في الملف camelCase كما في الوثيقة، والخاصية في النموذج PascalCase، والتعدادات
            // بأسمائها لا بأرقامها لأن FirestoreEnumNameConverter كتبها كذلك.
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly SemaphoreSlim gate = new(1, 1);
        private CachedCatalog? catalog;
        private CachedUsers? users;

        private string Directory => string.IsNullOrWhiteSpace(options.Value.CacheDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "zu-auditintel", "cache")
            : options.Value.CacheDirectory;

        /// <summary>الكتالوج كاملاً، أو null إن لم يكن للمخبأ ملفٌ على هذا الجهاز.</summary>
        public async Task<CachedCatalog?> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            if (catalog is { } cached)
                return cached;

            await gate.WaitAsync(cancellationToken);
            try
            {
                // الفشل لا يُخبأ: من بنى الملف بعد إقلاعٍ وجده التطبيق في القراءة التالية بلا إعادة تشغيل.
                catalog ??= await ReadAsync<CachedCatalog>("catalog_cache.json", cancellationToken);
                return catalog;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>الحسابات بمفتاحيها: الرقم الوظيفي للدخول، ورقم السطر للبحث الذي كانت تفعله الواجهة.</summary>
        public async Task<CachedUsers?> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            if (users is { } cached)
                return cached;

            await gate.WaitAsync(cancellationToken);
            try
            {
                if (users is null)
                {
                    var loaded = await ReadAsync<CachedUsers>("users_cache.json", cancellationToken);
                    loaded?.Index();
                    users = loaded;
                }

                return users;
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task<T?> ReadAsync<T>(string fileName, CancellationToken cancellationToken) where T : class
        {
            var path = Path.Combine(Directory, fileName);
            try
            {
                await using var stream = File.OpenRead(path);
                var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
                if (value is null)
                    throw new JsonException("الملف فارغ أو ليس كائناً.");

                logger.LogInformation("قُرئ المخبأ المحلي {Path}.", path);
                return value;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            {
                // لا ملف ولا إذن ولا JSON سليم: يُقال صراحةً، ولا يُقدَّم فراغٌ يقرؤه صاحب اللوحة على أنه
                // «لا بنود لجهتي» فيحسب أن كتالوجه ضاع.
                logger.LogError(error,
                    "تعذّر قراءة المخبأ المحلي {Path}، فلا بيانات محلية تُقدَّم أثناء انقطاع Firestore. " +
                    "يُبنى الملف بأداة scratch/build_local_cache.py.", path);
                return null;
            }
        }
    }

    /// <summary>بنود الكتالوج وجهاته كما في ملف المخبأ، وهي نماذج Firestore نفسها لا صورٌ عنها.</summary>
    public sealed class CachedCatalog
    {
        public List<AuditItem> Items { get; set; } = [];
        public List<Department> Departments { get; set; } = [];
    }

    /// <summary>حسابات المخبأ بمفتاحَي البحث المستعملين في دليل الحسابات.</summary>
    public sealed class CachedUsers
    {
        public List<User> Users { get; set; } = [];

        private Dictionary<string, User> byEmployeeId = new(StringComparer.Ordinal);
        private Dictionary<int, User> byId = [];

        /// <summary>يُبنى الفهرسان مرة واحدة تحت بوابة المخبأ، فلا يتسابق قارئان على بناءٍ مزدوج.</summary>
        internal void Index()
        {
            byEmployeeId = Users.ToDictionary(user => user.EmployeeId, StringComparer.Ordinal);
            byId = Users.ToDictionary(user => user.Id);
        }

        public User? ByEmployeeId(string employeeId) => byEmployeeId.GetValueOrDefault(employeeId);

        public User? ById(int userId) => byId.GetValueOrDefault(userId);
    }
}