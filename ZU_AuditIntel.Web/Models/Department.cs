using Google.Cloud.Firestore;

namespace ZU_AuditIntel.Web.Models
{
    /// <summary>جهة مدقَّقة. عدد بنودها محفوظ معها لأن شاشة الإحصائيات تقرأ جهاتٍ لا بنوداً.</summary>
    [FirestoreData]
    public class Department
    {
        [FirestoreProperty("departmentId")]
        public string DepartmentId { get; set; } = "";

        [FirestoreProperty("departmentName")]
        public string DepartmentName { get; set; } = "";

        [FirestoreProperty("itemCount")]
        public int ItemCount { get; set; }
    }

    /// <summary>معرّفات الجهات الثابتة المستخدمة في الزرع وفي شاشات التدقيق.</summary>
    public static class DepartmentIds
    {
        public const string HealthCenter = "health_center";
        public const string Admission = "admission";
        public const string Library = "library";
        public const string HumanResources = "hr";
        public const string Presidency = "presidency";
        public const string StudentAffairs = "student_affairs";
        public const string ItCenter = "it_center";
        public const string QualityUnit = "quality_unit";
        public const string Postgraduate = "postgraduate";
        public const string ScientificResearch = "scientific_research";
        public const string Procurement = "procurement";

        // الكليات الأكاديمية: كل كلية لوحة تدقيق مستقلة، لا لوحة واحدة تجمعها.
        public const string FacultySharia = "faculty_sharia";
        public const string FacultyArts = "faculty_arts";
        public const string FacultyBusiness = "faculty_business";
        public const string FacultyEducation = "faculty_education";
        public const string FacultyLaw = "faculty_law";
        public const string FacultyDesign = "faculty_design";
        public const string FacultyMedia = "faculty_media";
        public const string FacultyDentistry = "faculty_dentistry";
        public const string FacultyPharmacy = "faculty_pharmacy";
        public const string FacultyNursing = "faculty_nursing";
        public const string FacultyAlliedHealth = "faculty_allied_health";
        public const string FacultyScience = "faculty_science";
        public const string FacultyInformationTechnology = "faculty_it";
        public const string FacultyEngineeringTechnology = "faculty_engineering";
        public const string FacultyGraduateStudies = "faculty_graduate_studies";
        public const string FacultyTechnical = "faculty_technical";
    }
}
