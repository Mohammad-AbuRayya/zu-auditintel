using Google.Cloud.Firestore;
using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    public interface IAccountDirectory
    {
        Task<User?> AuthenticateAsync(string employeeId, string password, CancellationToken cancellationToken = default);
        Task<StaffAdjective?> FindAdjectiveByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
        Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default);
        Task SeedInitialUsersAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// دليل الحسابات على Firestore. مفتاح الوثيقة هو الرقم الوظيفي نفسه، فالدخول قراءة مباشرة
    /// بـ <c>users/{employeeId}</c> بلا استعلام ولا فهرس مركّب. الرقم الوظيفي ثابت، فتبقى الوثيقة
    /// واحدة طول عمر الحساب، وتغيير الكلمة يكتب أربعة حقول فقط ولا يمسّ ما عداه.
    ///
    /// والقراءات محروسة: إن رُدّت السحابة بحصةٍ نافدة قُرئ الحساب من المخبأ المحلي الحقيقي، فيبقى
    /// الدخول يعمل ببصمة صاحب الحساب نفسها لا بكلمةٍ تُستبدل للاختبار. أما الكتابة فتُرفض أثناء
    /// الانقطاع، فلا تُحفظ كلمةٌ جديدة على القرص وحده ثم تضيع عند عودة السحابة.
    /// </summary>
    public sealed class FirestoreAccountDirectory(
        FirestoreDb firestore,
        FirestoreResilience resilience,
        LocalDataCache cache) : IAccountDirectory
    {
        private CollectionReference Users { get; } = firestore.Collection(AuditCollections.Users);

        public async Task<User?> AuthenticateAsync(string employeeId, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrEmpty(password))
                return null;

            var user = await LoadByEmployeeIdAsync(employeeId.Trim(), cancellationToken);

            // كلمة المرور الخاطئة تُعامل معاملة الرقم الوظيفي غير الموجود: لا تكشف أيّ الرقمين صحيح.
            if (user is null)
                return null;

            var stored = new HashedPassword(user.PasswordSalt, user.PasswordHash, user.PasswordIterations);
            return PasswordHasher.Verify(password, stored) ? user : null;
        }

        public async Task<StaffAdjective?> FindAdjectiveByEmployeeIdAsync(
            string employeeId, CancellationToken cancellationToken = default)
        {
            var normalized = employeeId.Trim();
            if (normalized.Length == 0)
                return null;

            var user = await LoadByEmployeeIdAsync(normalized, cancellationToken);
            return user?.Adjective;
        }

        public async Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            // «id» رقم السطر الموروث من القاعدة القديمة، وهو وحده الذي يصلح لهذا البحث:
            // مفتاح الوثيقة رقم وظيفي لا رقم سطر.
            return await resilience.ReadAsync<User?>(
                "قراءة حساب برقم سطره",
                async () =>
                {
                    var snapshot = await Users.WhereEqualTo("id", (long)userId)
                        .Limit(1).GetSnapshotAsync(cancellationToken);
                    var document = snapshot.Documents.FirstOrDefault();

                    return document is null || !document.Exists ? null : document.ConvertTo<User>();
                },
                async () => (await cache.GetUsersAsync(cancellationToken))?.ById(userId));
        }

        public async Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default)
        {
            // الكتابة على سحابةٍ لا تجيب تفصل كلمة صاحب الحساب عمّا في السجل، فتُرفض برسالة عربية
            // مفهومة ويُعاد المحاولة بعد عودة الخدمة. والتجزئة داخل الحارس فلا تُحرق دورةٌ كاملة على كلمةٍ لن تُكتب.
            var hashed = await resilience.WriteAsync("تغيير كلمة المرور", async () =>
            {
                var value = PasswordHasher.Hash(newPassword);

                await Users.Document(user.EmployeeId).UpdateAsync(new Dictionary<string, object>
                {
                    ["passwordSalt"] = value.Salt,
                    ["passwordHash"] = value.Hash,
                    ["passwordIterations"] = value.Iterations,
                    ["mustChangePassword"] = false
                }, cancellationToken: cancellationToken);

                return value;
            });

            user.PasswordSalt = hashed.Salt;
            user.PasswordHash = hashed.Hash;
            user.PasswordIterations = hashed.Iterations;
            user.MustChangePassword = false;
        }

        /// <summary>
        /// الحسابات الأولية لا تُزرع من وثيقة مُعدّة، لأن الملح عشوائي فكل تكرار يولّد بصمة مختلفة.
        /// تُنشأ هنا عند الإقلاع بشكل متكرّر الآمن: تُقرأ موجود الواجهة مرة، فلا تلمس الأداة حساباً
        /// غيّر صاحبه كلمته ولا حساباً استُبدل اسمه. أرقام الأسطر مورثة، فالحساب الناقص يأخذ
        /// رقمه بالترتيب نفسه ولا يتزحزح رقم حساب موجود.
        /// </summary>
        public async Task SeedInitialUsersAsync(CancellationToken cancellationToken = default)
        {
            var existing = await resilience.ReadAsync<HashSet<string>?>(
                "قراءة الحسابات للزرع الأولي",
                async () => new HashSet<string>(
                    (await Users.GetSnapshotAsync(cancellationToken)).Documents.Select(d => d.Id)),
                () => Task.FromResult<HashSet<string>?>(null));

            // رُدّت القراءة فلا كتابة على سحابةٍ لا تجيب. الزرع متكرّر الآمن، فالناقص يُضاف في إقلاعٍ لاحق.
            if (existing is null)
                return;

            var batch = firestore.StartBatch();
            var added = 0;

            for (var i = 0; i < InitialAccounts.Length; i++)
            {
                var seed = InitialAccounts[i];
                if (existing.Contains(seed.EmployeeId))
                    continue;

                var hashed = PasswordHasher.Hash(seed.InitialPassword);
                batch.Set(Users.Document(seed.EmployeeId), new User
                {
                    Id = i + 1,
                    EmployeeId = seed.EmployeeId,
                    FullName = seed.FullName,
                    JobTitle = seed.JobTitle,
                    Adjective = ClassifyAdjective(seed.JobTitle),
                    Role = seed.Role,
                    DepartmentId = seed.DepartmentId,
                    PasswordSalt = hashed.Salt,
                    PasswordHash = hashed.Hash,
                    PasswordIterations = hashed.Iterations,
                    MustChangePassword = true
                });
                added++;
            }

            if (added > 0)
                await batch.CommitAsync(cancellationToken);
        }

        private async Task<User?> LoadByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken)
        {
            if (employeeId.Length == 0)
                return null;

            return await resilience.ReadAsync<User?>(
                "قراءة حساب بالرقم الوظيفي",
                async () =>
                {
                    var snapshot = await Users.Document(employeeId).GetSnapshotAsync(cancellationToken);
                    return snapshot.Exists ? snapshot.ConvertTo<User>() : null;
                },
                async () => (await cache.GetUsersAsync(cancellationToken))?.ByEmployeeId(employeeId));
        }

        // قاعدة صاحب المشروع: كل من مسمّاه «مسؤول كلية» من الكليات هو كادر أكاديمي، وبقية الحسابات موظفون.
        // وجهان للمعريف في التسميات القائمة — «مسؤول كلية الشريعة» و«مسؤول الكلية التقنية» — فكلاهما يدخل.
        private static StaffAdjective ClassifyAdjective(string jobTitle) =>
            jobTitle.StartsWith("مسؤول كلية", StringComparison.Ordinal)
            || jobTitle.StartsWith("مسؤول الكلية", StringComparison.Ordinal)
                ? StaffAdjective.AcademicStaff
                : StaffAdjective.Employee;

        private sealed record InitialAccount(
            string EmployeeId,
            string FullName,
            string JobTitle,
            AuditorRole Role,
            string? DepartmentId,
            string InitialPassword);

        // كلمات هذه الحسابات أولية مُعلنة في README ويُجبر النظام على تغييرها عند أول دخول.
        // ممثل جهة واحد لكل جهة: للجهات الإدارية أحد عشر حساباً، ولكل كلية من الكليات الست عشرة حساب.
        // والتدقيق النهائي على كل الجهات بيد رئيس الوحدة من شاشة واحدة، فلكل لوحة صاحبٌ يرفع وشاشةٌ يوقَّع عليها.
        // رقم 1000110 بقايا لوحة الكليات المجمّعة القديمة؛ بقي رقمه وصاحبته الآن كلية الشريعة، ثم تتمة
        // الكليات من 1000113، فلا يُعاد ترقيم حساب موجود ويضيع ما ارتبط به من جلسات.
        // FullName اسم الشخص وJobTitle مسمّاه الوظيفي. الأسماء هنا مواضع شاغرة حتى يرد دليل
        // الجامعة الحقيقي بأصحابه، فأرقام الحسابات ومسمّياتها هي المعتمدة لا الأسماء.
        private static readonly InitialAccount[] InitialAccounts =
        [
            new("1000101", "عمر سليمان دعيج المجالي", "مسؤول دائرة المكتبة العامة", AuditorRole.EntityRepresentative, DepartmentIds.Library, "Zu#Library1"),
            new("1000102", "ياسر عبد الفتاح صالح السلايمة", "مسؤول المركز الصحي", AuditorRole.EntityRepresentative, DepartmentIds.HealthCenter, "Zu#Health1"),
            new("1000103", "ماهر سلامة سالم الحديد", "مسؤول دائرة القبول والتسجيل", AuditorRole.EntityRepresentative, DepartmentIds.Admission, "Zu#Admission1"),
            new("1000104", "طارق محمد علي الخصاونة", "مسؤول دائرة الموارد البشرية", AuditorRole.EntityRepresentative, DepartmentIds.HumanResources, "Zu#Hr1"),
            new("1000105", "سعيد داود إبراهيم الشرع", "مسؤول ديوان الرئاسة", AuditorRole.EntityRepresentative, DepartmentIds.Presidency, "Zu#Presidency1"),
            new("1000106", "فيصل كامل يوسف المراعية", "مسؤول عمادة شؤون الطلبة", AuditorRole.EntityRepresentative, DepartmentIds.StudentAffairs, "Zu#StudentAffairs1"),
            new("1000107", "بلال أحمد سالم الزريقات", "مسؤول مركز الحاسوب", AuditorRole.EntityRepresentative, DepartmentIds.ItCenter, "Zu#ItCenter1"),
            new("1000108", "جواد محمود علي الطرمان", "مسؤول وحدة التطوير وضمان الجودة", AuditorRole.EntityRepresentative, DepartmentIds.QualityUnit, "Zu#QualityUnit1"),
            new("1000109", "رمضان عوض الله الحراحشة", "مسؤول عمادة الدراسات العليا", AuditorRole.EntityRepresentative, DepartmentIds.Postgraduate, "Zu#Postgraduate1"),
            new("1000111", "يوسف عبد الله فؤاد القضاة", "مسؤول عمادة البحث العلمي", AuditorRole.EntityRepresentative, DepartmentIds.ScientificResearch, "Zu#Research1"),
            new("1000112", "حمزة اسماعيل محمد البطوش", "مسؤول دائرة اللوازم والمشتريات", AuditorRole.EntityRepresentative, DepartmentIds.Procurement, "Zu#Procurement1"),

            new("1000110", "أنس عبد الرحمن نصير المدادحة", "مسؤول كلية الشريعة", AuditorRole.EntityRepresentative, DepartmentIds.FacultySharia, "Zu#Sharia1"),
            new("1000113", "خالد ماجد سالم العبيدات", "مسؤول كلية الآداب", AuditorRole.EntityRepresentative, DepartmentIds.FacultyArts, "Zu#Arts1"),
            new("1000114", "رائد فهمي حسن الشقور", "مسؤول كلية العلوم الاقتصادية والإدارية", AuditorRole.EntityRepresentative, DepartmentIds.FacultyBusiness, "Zu#Business1"),
            new("1000115", "محمود عبد اللطيف سليم الدبابنة", "مسؤول كلية العلوم التربوية", AuditorRole.EntityRepresentative, DepartmentIds.FacultyEducation, "Zu#Education1"),
            new("1000116", "زياد كمال الدين يوسف النعيمات", "مسؤول كلية القانون", AuditorRole.EntityRepresentative, DepartmentIds.FacultyLaw, "Zu#Law1"),
            new("1000117", "ليث جمال عبد الرزاق الفناطسة", "مسؤول كلية الفنون والتصميم", AuditorRole.EntityRepresentative, DepartmentIds.FacultyDesign, "Zu#Design1"),
            new("1000118", "صهيب نايف عقيل الزعبي", "مسؤول كلية الإعلام", AuditorRole.EntityRepresentative, DepartmentIds.FacultyMedia, "Zu#Media1"),
            new("1000119", "رامي مالك فتحي الجازي", "مسؤول كلية طب الأسنان", AuditorRole.EntityRepresentative, DepartmentIds.FacultyDentistry, "Zu#Dentistry1"),
            new("1000120", "سامر عبد الإله كريم العبادي", "مسؤول كلية الصيدلة", AuditorRole.EntityRepresentative, DepartmentIds.FacultyPharmacy, "Zu#Pharmacy1"),
            new("1000121", "وسيم عطية الله حمد المكرادي", "مسؤول كلية التمريض", AuditorRole.EntityRepresentative, DepartmentIds.FacultyNursing, "Zu#Nursing1"),
            new("1000122", "نور الدين حسني جميل الخرابشة", "مسؤول كلية العلوم الطبية المساندة", AuditorRole.EntityRepresentative, DepartmentIds.FacultyAlliedHealth, "Zu#Allied1"),
            new("1000123", "أمير عبد الحكم نبيل الرمامنة", "مسؤول كلية العلوم", AuditorRole.EntityRepresentative, DepartmentIds.FacultyScience, "Zu#Science1"),
            new("1000124", "عصام محمد رفيق الصمادي", "مسؤول كلية تكنولوجيا المعلومات", AuditorRole.EntityRepresentative, DepartmentIds.FacultyInformationTechnology, "Zu#FacultyIt1"),
            new("1000125", "شادي محي الدين قاسم الزير", "مسؤول كلية التكنولوجيا الهندسية", AuditorRole.EntityRepresentative, DepartmentIds.FacultyEngineeringTechnology, "Zu#Engineering1"),
            new("1000126", "عماد عبد الصمد كامل القرالة", "مسؤول كلية الدراسات العليا", AuditorRole.EntityRepresentative, DepartmentIds.FacultyGraduateStudies, "Zu#Graduate1"),
            new("1000127", "بدر محمد عبد الباسط الرواضية", "مسؤول الكلية التقنية", AuditorRole.EntityRepresentative, DepartmentIds.FacultyTechnical, "Zu#Technical1"),

            new("3000301", "أنور حسين زكريا قطراوي", "رئيس وحدة التطوير وضمان الجودة", AuditorRole.QualityHead, null, "Zu#Quality1"),

            // صاحب النتيجة شخص غير من يدقّق: لا يرفع دليلاً ولا يوقّع بنداً، فشاشته هي الأخرى منفصلة.
            // رقمه 5000501 لا يقابل جهة، لأن نظره على الجامعة كلها لا على لوحة واحدة.
            new("5000501", "عبد الرحمن ياسر صالح الروابدة", "مساعد الرئيس لشؤون الجودة", AuditorRole.FinalResultReader, null, "Zu#Result1"),

            // الحساب الإداري الوحيد الذي يرى لوحات كل الجهات؛ للمعاينة والدعم، ولا يرفع دليلاً ولا يعتمد نتيجة.
            new("9999999", "محمد خالد عمر أبو رية", "مدير النظام", AuditorRole.SystemAdministrator, null, "Zu#Admin1")
        ];
    }
}
