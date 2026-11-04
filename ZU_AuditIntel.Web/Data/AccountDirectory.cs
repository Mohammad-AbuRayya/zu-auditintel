using Microsoft.EntityFrameworkCore;
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

    public sealed class EfAccountDirectory(IDbContextFactory<AppDbContext> contextFactory) : IAccountDirectory
    {
        public async Task<User?> AuthenticateAsync(string employeeId, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrEmpty(password))
                return null;

            var normalized = employeeId.Trim();
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(
                u => u.EmployeeId == normalized, cancellationToken);

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

            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EmployeeId == normalized, cancellationToken);
            return user?.Adjective;
        }

        public async Task<User?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task ChangePasswordAsync(User user, string newPassword, CancellationToken cancellationToken = default)
        {
            var hashed = PasswordHasher.Hash(newPassword);
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            db.Attach(user);
            user.PasswordSalt = hashed.Salt;
            user.PasswordHash = hashed.Hash;
            user.PasswordIterations = hashed.Iterations;
            user.MustChangePassword = false;
            await db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// الحسابات الأولية لا تُزرع عبر HasData لأن الملح عشوائي، فثبات الترحيل يتكسّر.
        /// تُنشأ هنا عند الإقلاع بشكل متكرّر الآمن إن لم تكن موجودة.
        /// </summary>
        public async Task SeedInitialUsersAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            foreach (var seed in InitialAccounts)
            {
                if (await db.Users.AnyAsync(u => u.EmployeeId == seed.EmployeeId, cancellationToken))
                    continue;

                var hashed = PasswordHasher.Hash(seed.InitialPassword);
                db.Users.Add(new User
                {
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
            }

            await db.SaveChangesAsync(cancellationToken);
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
        // مع كل جهة مدقّق يدوي واحد بحساب مستقل، فصار لكل لوحة صاحبٌ يرفع وشاشةٌ يوقَّع عليها.
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

            // التدقيق اليدوي على كل جهة بيده شخص غير الذي رفع أدلتها، فلكل جهة من الجهات السبع
            // والعشرين مدقّق خاص بشاشته. أرقامهم مرآة أرقام ممثلي الجهات: 4000104 يقابل 1000104
            // في الموارد البشرية، و4000110 يقابل 1000110 في كلية الشريعة، فلا يتوهّم أحد قرناً.
            new("4000101", "منذر تيسير عبد الرحيم العبداللات", "مدقق دائرة المكتبة العامة", AuditorRole.EntityAuditor, DepartmentIds.Library, "Zu#AudLibrary1"),
            new("4000102", "هاني ماجد سالم الطورة", "مدقق المركز الصحي", AuditorRole.EntityAuditor, DepartmentIds.HealthCenter, "Zu#AudHealth1"),
            new("4000103", "ساري فتحي كريم الغرايبة", "مدقق دائرة القبول والتسجيل", AuditorRole.EntityAuditor, DepartmentIds.Admission, "Zu#AudAdmission1"),
            new("4000104", "وائل عبد المطلب حسين البشابشة", "مدقق دائرة الموارد البشرية", AuditorRole.EntityAuditor, DepartmentIds.HumanResources, "Zu#AudHr1"),
            new("4000105", "مازن سامي داود الزبون", "مدقق ديوان الرئاسة", AuditorRole.EntityAuditor, DepartmentIds.Presidency, "Zu#AudPresidency1"),
            new("4000106", "عزام لطفي سعيد الشرايحة", "مدقق عمادة شؤون الطلبة", AuditorRole.EntityAuditor, DepartmentIds.StudentAffairs, "Zu#AudStudents1"),
            new("4000107", "فراس عوني نعمان الرشايدة", "مدقق مركز الحاسوب", AuditorRole.EntityAuditor, DepartmentIds.ItCenter, "Zu#AudItCenter1"),
            new("4000108", "عادل كامل فريح المحايشة", "مدقق وحدة التطوير وضمان الجودة", AuditorRole.EntityAuditor, DepartmentIds.QualityUnit, "Zu#AudQualityUnit1"),
            new("4000109", "صفوان بهجت علم الدين الجرادات", "مدقق عمادة الدراسات العليا", AuditorRole.EntityAuditor, DepartmentIds.Postgraduate, "Zu#AudPostgraduate1"),
            new("4000111", "مكي عوض سالم الحجاجلة", "مدقق عمادة البحث العلمي", AuditorRole.EntityAuditor, DepartmentIds.ScientificResearch, "Zu#AudResearch1"),
            new("4000112", "حاتم عدلي ياسين المصاروة", "مدقق دائرة اللوازم والمشتريات", AuditorRole.EntityAuditor, DepartmentIds.Procurement, "Zu#AudProcurement1"),

            new("4000110", "ذيب حربي مفلح الفقهاء", "مدقق كلية الشريعة", AuditorRole.EntityAuditor, DepartmentIds.FacultySharia, "Zu#AudSharia1"),
            new("4000113", "رشدي كمال أنيس الطروانة", "مدقق كلية الآداب", AuditorRole.EntityAuditor, DepartmentIds.FacultyArts, "Zu#AudArts1"),
            new("4000114", "غسان فخري ناجي الجرارحة", "مدقق كلية العلوم الاقتصادية والإدارية", AuditorRole.EntityAuditor, DepartmentIds.FacultyBusiness, "Zu#AudBusiness1"),
            new("4000115", "لؤي سمير عطا الله المرايات", "مدقق كلية العلوم التربوية", AuditorRole.EntityAuditor, DepartmentIds.FacultyEducation, "Zu#AudEducation1"),
            new("4000116", "ياسين مهدي كامل السبيلات", "مدقق كلية القانون", AuditorRole.EntityAuditor, DepartmentIds.FacultyLaw, "Zu#AudLaw1"),
            new("4000117", "أيمن صبري رشدي الحواتمة", "مدقق كلية الفنون والتصميم", AuditorRole.EntityAuditor, DepartmentIds.FacultyDesign, "Zu#AudDesign1"),
            new("4000118", "تامر وديع حسن النويرات", "مدقق كلية الإعلام", AuditorRole.EntityAuditor, DepartmentIds.FacultyMedia, "Zu#AudMedia1"),
            new("4000119", "باسل جودت عقلة الشراح", "مدقق كلية طب الأسنان", AuditorRole.EntityAuditor, DepartmentIds.FacultyDentistry, "Zu#AudDentistry1"),
            new("4000120", "بسام نبيل فؤاد العجارمة", "مدقق كلية الصيدلة", AuditorRole.EntityAuditor, DepartmentIds.FacultyPharmacy, "Zu#AudPharmacy1"),
            new("4000121", "جهاد راغب سعد الله الحياصات", "مدقق كلية التمريض", AuditorRole.EntityAuditor, DepartmentIds.FacultyNursing, "Zu#AudNursing1"),
            new("4000122", "داود زهدي كامل الخلايلة", "مدقق كلية العلوم الطبية المساندة", AuditorRole.EntityAuditor, DepartmentIds.FacultyAlliedHealth, "Zu#AudAllied1"),
            new("4000123", "إياد شكري نعمان الرمثاوي", "مدقق كلية العلوم", AuditorRole.EntityAuditor, DepartmentIds.FacultyScience, "Zu#AudScience1"),
            new("4000124", "وليد خيري جميل الشوبكي", "مدقق كلية تكنولوجيا المعلومات", AuditorRole.EntityAuditor, DepartmentIds.FacultyInformationTechnology, "Zu#AudFacultyIt1"),
            new("4000125", "مروان حسام الدين صالح العبيدين", "مدقق كلية التكنولوجيا الهندسية", AuditorRole.EntityAuditor, DepartmentIds.FacultyEngineeringTechnology, "Zu#AudEngineering1"),
            new("4000126", "نضال عبد الظاهر فريحان الزبن", "مدقق كلية الدراسات العليا", AuditorRole.EntityAuditor, DepartmentIds.FacultyGraduateStudies, "Zu#AudGraduate1"),
            new("4000127", "رياض فتحي عبد الصمد المبيض", "مدقق الكلية التقنية", AuditorRole.EntityAuditor, DepartmentIds.FacultyTechnical, "Zu#AudTechnical1"),

            new("3000301", "علي أحمد حسين الجبور", "رئيس وحدة التطوير وضمان الجودة", AuditorRole.QualityHead, null, "Zu#Quality1"),

            // صاحب النتيجة شخص غير من يدقّق: لا يرفع دليلاً ولا يوقّع بنداً، فشاشته هي الأخرى منفصلة.
            // رقمه 5000501 لا يقابل جهة، لأن نظره على الجامعة كلها لا على لوحة واحدة.
            new("5000501", "عبد الرحمن ياسر صالح الروابدة", "مساعد الرئيس لشؤون الجودة", AuditorRole.FinalResultReader, null, "Zu#Result1"),

            // الحساب الإداري الوحيد الذي يرى لوحات كل الجهات؛ للمعاينة والدعم، ولا يرفع دليلاً ولا يعتمد نتيجة.
            new("9999999", "محمد خالد عمر أبو رية", "مدير النظام", AuditorRole.SystemAdministrator, null, "Zu#Admin1")
        ];
    }
}
