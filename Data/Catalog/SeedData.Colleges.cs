using ZU_AuditIntel.Web.Models;

// كتالوجات الكليات الست عشرة: كل كلية لوحة مستقلة تأخذ بنود الكليات كما هي في قالب
// <see cref="FacultiesCatalog"/> المنقول حرفياً من قائمة الفحص ZU/QP02F003، ويُزاح رقم البند
// إلى بلوك خاص بالكلية ليتعرّف الوكيل على بند كليته وحده.
namespace ZU_AuditIntel.Web.Data
{
    public static partial class SeedData
    {
        // (المعرّف، الاسم المعروض، أساس أرقام البنود، وهل في الكلية مختبرات علمية أو طبية)
        private static readonly (string Id, string Name, int CatalogBase, bool HasLabs)[] Colleges =
        [
            (DepartmentIds.FacultySharia, "كلية الشريعة", 1200, false),
            (DepartmentIds.FacultyArts, "كلية الآداب", 1300, false),
            (DepartmentIds.FacultyBusiness, "كلية العلوم الاقتصادية والإدارية", 1400, false),
            (DepartmentIds.FacultyEducation, "كلية العلوم التربوية", 1500, false),
            (DepartmentIds.FacultyLaw, "كلية القانون", 1600, false),
            (DepartmentIds.FacultyDesign, "كلية الفنون والتصميم", 1700, false),
            (DepartmentIds.FacultyMedia, "كلية الإعلام", 1800, false),
            (DepartmentIds.FacultyDentistry, "كلية طب الأسنان", 1900, true),
            (DepartmentIds.FacultyPharmacy, "كلية الصيدلة", 2000, true),
            (DepartmentIds.FacultyNursing, "كلية التمريض", 2100, true),
            (DepartmentIds.FacultyAlliedHealth, "كلية العلوم الطبية المساندة", 2200, true),
            (DepartmentIds.FacultyScience, "كلية العلوم", 2300, true),
            (DepartmentIds.FacultyInformationTechnology, "كلية تكنولوجيا المعلومات", 2400, true),
            (DepartmentIds.FacultyEngineeringTechnology, "كلية التكنولوجيا الهندسية", 2500, true),
            (DepartmentIds.FacultyGraduateStudies, "كلية الدراسات العليا", 2600, false),
            (DepartmentIds.FacultyTechnical, "الكلية التقنية", 2700, true)
        ];

        private static ItemSeed[] CollegesCatalog() =>
        [
            .. Colleges.SelectMany(college => FacultiesCatalog()
                .Select((template, offset) => template with
                {
                    Id = college.CatalogBase + offset,
                    DepartmentId = college.Id
                })
                // «خاص بالكليات العلمية والطبية» في قائمة الفحص، فلا يُزرع لغيرها.
                .Where(item => college.HasLabs || !item.LabOnly))
        ];
    }
}
