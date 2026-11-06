using Google.Cloud.Firestore;

namespace ZU_AuditIntel.Web.Models
{
    public enum AuditorRole
    {
        /// <summary>مسؤول الجهة: يرفع أدلة بنود جهته فقط.</summary>
        EntityRepresentative,

        /// <summary>ضابط الارتباط/المدقق البشري: راجع ملفات الجهة بعد اجتياز الفحص الآلي.</summary>
        LiaisonOfficer,

        /// <summary>رئيس وحدة التطوير وضمان الجودة: الاعتماد النهائي على مستوى الجامعة.</summary>
        QualityHead,

        /// <summary>
        /// حساب إداري واحد يفتح كل لوحات الجهات لأغراض المعاينة والدعم، ويملك صلاحيات الأدوار
        /// نفسها ليختبر المسار كاملاً؛ لكنه يوقّع باسمه هو، فيبقى أثره مفروقاً في السجل.
        /// </summary>
        SystemAdministrator,

        /// <summary>
        /// المدقّق البشري لجهة واحدة: شاشته صفحة جهته وحدها، فلا يرى بنود جهة أخرى ولا يوقّع عليها.
        /// الرفع والتوقيع يبقى طرفاه حسابين مختلفين داخل الجهة نفسها، كما في سائر الشاشات.
        /// </summary>
        EntityAuditor,

        /// <summary>
        /// صاحب النتيجة النهائية: لا يرفع دليلاً ولا يوقّع بنداً ولا يرى شاشة تدقيق، بل ينتظر ما
        /// أغلقه المدقّق البشري من بنود كل الجهات فيقرأه مع ملفاته. شاشته منفصلة لأنه شخص مختلف.
        /// </summary>
        FinalResultReader
    }

    /// <summary>
    /// صفة صاحب الحساب عند الجامعة، وهي غير <see cref="AuditorRole" /> الذي يحدّد صلاحياته في
    /// النظام: الكادر الأكاديمي عضو هيئة تدريس، والموظف إداري. تُشتق من المسمى الوظيفي لا من الدور،
    /// فمدير النظام ورئيس الوحدة قد يكونان من أيّ الصنفين.
    /// </summary>
    public enum StaffAdjective
    {
        /// <summary>موظف عادي: إداري في دائرة أو عمادة أو مركز أو وحدة.</summary>
        Employee,

        /// <summary>كادر أكاديمي: ممثل كلية من الكليات.</summary>
        AcademicStaff
    }

    /// <summary>
    /// حساب مستخدم، ووثيقته في Firestore مفتاحها رقمه الوظيفي. أسماء الخصائص هي أسماء الحقول
    /// نفسها كما زرعها المصدّر، فلا خريطة أسماء بين الكود والقاعدة تُنسى عند التعديل.
    /// </summary>
    [FirestoreData]
    public class User
    {
        [FirestoreProperty("id")]
        public int Id { get; set; }

        [FirestoreProperty("employeeId")]
        public string EmployeeId { get; set; } = "";

        /// <summary>اسم الشخص نفسه، لا وظيفته: يظهر في السجل وفي بطاقة الهوية.</summary>
        [FirestoreProperty("fullName")]
        public string FullName { get; set; } = "";

        /// <summary>المسمى الوظيفي صاحبُ الحساب («مسؤول دائرة…» ونحوه)، لا يُعرض مكان الاسم.</summary>
        [FirestoreProperty("jobTitle")]
        public string JobTitle { get; set; } = "";

        /// <summary>أكاديمي أم إداري؛ يُشتق من <see cref="JobTitle" /> عند الزرع ويُخزن ليُقرأ منه.</summary>
        [FirestoreProperty("adjective", ConverterType = typeof(FirestoreEnumNameConverter<StaffAdjective>))]
        public StaffAdjective Adjective { get; set; }

        [FirestoreProperty("role", ConverterType = typeof(FirestoreEnumNameConverter<AuditorRole>))]
        public AuditorRole Role { get; set; }

        /// <summary>الجهة التي يخدمها الحساب؛ null لرئيس وحدة الجودة لأنه على مستوى الجامعة.</summary>
        [FirestoreProperty("departmentId")]
        public string? DepartmentId { get; set; }

        [FirestoreProperty("passwordSalt")]
        public string PasswordSalt { get; set; } = "";

        [FirestoreProperty("passwordHash")]
        public string PasswordHash { get; set; } = "";

        [FirestoreProperty("passwordIterations")]
        public int PasswordIterations { get; set; }

        [FirestoreProperty("mustChangePassword")]
        public bool MustChangePassword { get; set; }
    }
}
