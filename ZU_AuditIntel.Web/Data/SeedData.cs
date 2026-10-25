using ZU_AuditIntel.Web.Models;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// كتالوج بنود التدقيق المعتمد. هذا الملف هو مصدر الحقيقة الوحيد للبنود وأدلتها
    /// وأسئلتها؛ الواجهات تقرأ منه عبر <see cref="IAuditCatalog"/> ولا تكرّره محلياً.
    /// </summary>
    public static class SeedData
    {
        public static Department[] Departments() =>
        [
            new() { DepartmentId = DepartmentIds.HealthCenter, DepartmentName = "المركز الصحي" },
            new() { DepartmentId = DepartmentIds.Admission, DepartmentName = "دائرة القبول والتسجيل" },
            new() { DepartmentId = DepartmentIds.Library, DepartmentName = "دائرة المكتبة العامة" },
            new() { DepartmentId = DepartmentIds.HumanResources, DepartmentName = "دائرة الموارد البشرية" },
            new() { DepartmentId = DepartmentIds.Presidency, DepartmentName = "ديوان الرئاسة" },
            new() { DepartmentId = DepartmentIds.StudentAffairs, DepartmentName = "عمادة شؤون الطلبة" },
            new() { DepartmentId = DepartmentIds.ItCenter, DepartmentName = "مركز الحاسوب" },
            new() { DepartmentId = DepartmentIds.QualityUnit, DepartmentName = "وحدة التطوير وضمان الجودة" },
            new() { DepartmentId = DepartmentIds.Postgraduate, DepartmentName = "عمادة الدراسات العليا" },
            new() { DepartmentId = DepartmentIds.Faculties, DepartmentName = "الكليات الأكاديمية" },
            new() { DepartmentId = DepartmentIds.ScientificResearch, DepartmentName = "عمادة البحث العلمي" },
            new() { DepartmentId = DepartmentIds.Procurement, DepartmentName = "دائرة اللوازم والمشتريات" }
        ];

        public static AuditItem[] AuditItems() =>
            Catalog.Select((s, index) => new AuditItem
            {
                Id = s.Id,
                DepartmentId = s.DepartmentId,
                Category = s.Category,
                Title = s.Title,
                IsoReference = s.IsoReference,
                Workflow = s.Workflow,
                SortOrder = index + 1
            }).ToArray();

        public static AuditQuestion[] Questions() =>
            Catalog.SelectMany(s => s.Questions.Select((text, i) => new AuditQuestion
            {
                Id = s.Id * 1000 + i + 1,
                AuditItemId = s.Id,
                Text = text,
                SortOrder = i + 1
            })).ToArray();

        public static AuditEvidenceRequirement[] EvidenceRequirements() =>
            Catalog.SelectMany(s => s.Evidences.Select((text, i) => new AuditEvidenceRequirement
            {
                Id = s.Id * 1000 + 501 + i,
                AuditItemId = s.Id,
                Text = text,
                SortOrder = i + 1
            })).ToArray();

        private sealed record ItemSeed(
            int Id,
            string DepartmentId,
            AuditItemCategory Category,
            string Title,
            string IsoReference,
            string Workflow,
            string[] Questions,
            string[] Evidences);

        // بنود تدقيق المكتبة: تسعة بنود مشتركة من نموذج التدقيق العام وأربعة بنود خاصة بالمكتبة.
        // مراجع الآيزو وخطوات العمل لا تُملأ إلا حيث وردت في سجل الوثائق المعتمد.
        private static readonly ItemSeed[] Catalog =
        [
            new(1, DepartmentIds.Library, AuditItemCategory.Common, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد للمكتبة العامة يوضح الشعب والأقسام وخطوط الإشراف ومطابقته للكادر الحالي.",
                ["هل يوجد هيكل تنظيمي معتمد ومحدث؟", "هل يوضح خطوط السلطة والمسؤوليات؟", "هل يتم مراجعته وتعميمه دورياً؟"],
                ["نسخة الهيكل المعتمد", "قرار الاعتماد", "سجل التحديثات", "تعميم داخلي"]),

            new(2, DepartmentIds.Library, AuditItemCategory.Common, "الوصف الوظيفي", "7.2",
                "التحقق من وجود بطاقات وصف وظيفي معتمدة لجميع المسميات الوظيفية في المكتبة وتوقيع الموظفين عليها.",
                ["هل يوجد وصف وظيفي لكل موظف؟", "هل يتوافق مع المهام الفعلية؟", "هل يتم تحديثه بشكل دوري؟"],
                ["بطاقات الوصف الوظيفي", "تواقيع الموظفين", "سجل التحديث"]),

            new(3, DepartmentIds.Library, AuditItemCategory.Common, "ضبط الوثائق والسجلات", "7.1.6 / 7.5",
                "التحقق من ضبط فهارس المكتبة (الورقية / الإلكترونية) وكافة السجلات والنماذج المعتمدة وفق متطلبات نظام الجودة.",
                ["هل يتم تطبيق إجراء ضبط الوثائق؟", "هل يتم التحكم بالإصدارات؟", "هل يتم أرشفة الوثائق وإتاحتها؟"],
                ["سجل الوثائق", "سجل الإصدارات", "طلبات تعديل الوثائق", "نظام الأرشفة"]),

            new(4, DepartmentIds.Library, AuditItemCategory.Common, "الخطة الاستراتيجية", "", "",
                ["هل توجد خطة استراتيجية معتمدة؟", "هل تتماشى مع رؤية الجامعة؟", "هل يتم تتبع تنفيذها؟"],
                ["وثيقة الخطة", "تقارير متابعة", "مؤشرات الأداء"]),

            new(5, DepartmentIds.Library, AuditItemCategory.Common, "مؤشرات الأداء", "", "",
                ["هل توجد مؤشرات أداء واضحة؟", "هل يتم قياسها وتحليلها دورياً؟", "هل تتخذ إجراءات بناءً على النتائج؟"],
                ["تقارير KPI", "تحليل الأداء", "خطط التحسين"]),

            new(6, DepartmentIds.Library, AuditItemCategory.Common, "تحليل SWOT", "", "",
                ["هل تم إعداد تحليل SWOT؟", "هل يتم تحديثه واستخدامه في التخطيط؟"],
                ["تقرير SWOT", "محاضر الاجتماعات"]),

            new(7, DepartmentIds.Library, AuditItemCategory.Common, "إدارة المخاطر", "", "",
                ["هل تم تحديد وتقييم المخاطر؟", "هل توجد خطط معالجة ومراجعة دورية؟"],
                ["سجل المخاطر", "خطة المعالجة", "تقارير المراجعة"]),

            new(8, DepartmentIds.Library, AuditItemCategory.Common, "التدريب والكفاءة", "", "",
                ["هل توجد خطة تدريب سنوية؟", "هل يتم تنفيذها وتقييم أثرها؟"],
                ["خطة تدريب", "سجلات حضور", "تقارير التقييم"]),

            new(9, DepartmentIds.Library, AuditItemCategory.Common, "الشكاوى ورضا المستفيدين", "", "",
                ["هل توجد آلية فعالة للشكاوى؟", "هل يتم تحليل الشكاوى وقياس رضا المستفيدين؟", "هل تنفذ تحسينات؟"],
                ["نماذج الشكاوى", "تقارير التحليل", "استبانات الرضا"]),

            new(10, DepartmentIds.Library, AuditItemCategory.DepartmentSpecific, "نظام إدارة المكتبة", "7.1.3 / 7.1.6",
                "التحقق من كفاءة النظام الآلي لإدارة المكتبة في الفهرسة والبحث والإعارة وإصدار التقارير الإحصائية الدقيقة.",
                ["هل يوجد نظام مكتبة فعال؟", "هل يتم تحديثه وصيانته؟"],
                ["نظام المكتبة", "تقارير الاستخدام"]),

            new(11, DepartmentIds.Library, AuditItemCategory.DepartmentSpecific, "الإعارة", "", "",
                ["هل توجد إجراءات إعارة واضحة؟", "هل يتم توثيق العمليات؟"],
                ["سجلات الإعارة"]),

            new(12, DepartmentIds.Library, AuditItemCategory.DepartmentSpecific, "المصادر الإلكترونية", "", "",
                ["هل يتم تحديث الاشتراكات وقواعد البيانات؟"],
                ["اشتراكات", "تقارير التحديث"]),

            new(13, DepartmentIds.Library, AuditItemCategory.DepartmentSpecific, "رضا المستفيدين", "", "",
                ["هل يتم قياس رضا المستفيدين؟", "هل تُحل الشكاوى؟"],
                ["اشتراكات", "تقارير التحديث"]),

            // بنود بقية الجهات (كتالوج موسّع للاستخدام لاحقاً).
            new(100, DepartmentIds.HealthCenter, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد يحدد العيادات والتمريض والصيدلية والاستقبال وخطوط السلطة والمسؤولية.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع للمركز الصحي", "كشف بأسماء الكادر الطبي والتمريضي والإداري"]),

            new(101, DepartmentIds.HealthCenter, AuditItemCategory.DepartmentSpecific, "الوصف الوظيفي", "5.3 / 7.2",
                "التحقق من أن لكل مسمى وظيفي بطاقة وصف وظيفي معتمدة وموقعة مع سريان تراخيص مزاولة المهنة الصحية.",
                [],
                ["بطاقات الوصف الوظيفي المعتمدة", "عينة 3 موظفين (طبيب، ممرض، صيدلاني)", "نسخ مزاولات المهنة السارية"]),

            new(102, DepartmentIds.HealthCenter, AuditItemCategory.DepartmentSpecific, "السياسة الصحية", "5.2",
                "التحقق من وجود سياسة صحية وتعليمات واضحة لتقديم الرعاية الصحية والتأمين الصحي ومعلنة ومفهومة.",
                [],
                ["وثيقة السياسة الصحية وتعليمات المركز الصحي", "شواهد إعلان السياسة الصحية داخل المركز وعلى الموقع"]),

            new(200, DepartmentIds.Admission, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد للدائرة يوضح شعب القبول والتسجيل والوثائق ومطابقته للكادر الفعلي.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع", "دليل تعميم الهيكل على موظفي الدائرة", "كشف بأسماء الموظفين"]),

            new(201, DepartmentIds.Admission, AuditItemCategory.DepartmentSpecific, "الوصف الوظيفي", "5.3 / 7.2",
                "التحقق من وجود بطاقات وصف وظيفي معتمدة لمسجلي التخصصات وموظفي القبول والوثائق وتوقيعهم بالعلم.",
                [],
                ["بطاقات الوصف الوظيفي لكافة الوظائف", "عينة 3 موظفين من نماذج استلام الوصف الوظيفي الموقعة"]),

            new(300, DepartmentIds.HumanResources, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لدائرة الموارد البشرية ومطابقته للكادر الفعلي.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع", "دليل تعميمه للموظفين", "كشف بأسماء موظفي الدائرة"]),

            new(400, DepartmentIds.Presidency, AuditItemCategory.DepartmentSpecific, "تحديد الأطراف المعنية", "4.2",
                "التحقق من قيام ديوان الرئاسة بحصر كافة الجهات التي يتعامل معها وتحديد متطلبات كل جهة.",
                [],
                ["سجل مصفوفة حصر الأطراف المعنية", "آلية متابعة تلبية متطلبات الأطراف المعنية وتحديثها"]),

            new(500, DepartmentIds.StudentAffairs, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لعمادة شؤون الطلبة يوضح الأقسام وشعب وخطوط السلطة.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع للعمادة", "دليل تعميمه للموظفين", "كشف بأسماء الموظفين"]),

            new(600, DepartmentIds.ItCenter, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لمركز الحاسوب يوضح الأقسام والشعب التقنية والبرمجية.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع لمركز الحاسوب", "دليل تعميمه للموظفين"]),

            new(700, DepartmentIds.QualityUnit, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لوحدة التطوير وضمان الجودة ولجان وضباط الارتباط.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد", "قرارات تشكيل مجلس الجودة وضباط الارتباط", "كشف بالأسماء"]),

            new(800, DepartmentIds.Postgraduate, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لعمادة الدراسات العليا ومجلس الدراسات العليا والأقسام التابعة.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد", "قرار تشكيل مجلس الدراسات العليا", "كشف بأسماء كادر العمادة"]),

            new(900, DepartmentIds.Faculties, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد للكلية يوضح العمادة والمجالس والأقسام الأكاديمية واللجان.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد", "قرارات تشكيل مجلس الكلية ومجالس الأقسام", "كشف بأعضاء الهيئة التدريسية"]),

            new(1000, DepartmentIds.ScientificResearch, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لعمادة البحث العلمي ومجلس البحث العلمي والمجلات العلمية.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد", "قرار تشكيل مجلس البحث العلمي وهيئات تحرير المجلات", "كشف بالأسماء"]),

            new(1100, DepartmentIds.Procurement, AuditItemCategory.DepartmentSpecific, "الهيكل التنظيمي", "5.3",
                "التحقق من وجود هيكل تنظيمي معتمد لدائرة اللوازم والمشتريات يوضح شعب الشراء والمستودعات.",
                [],
                ["نسخة الهيكل التنظيمي المعتمد والموقع للدائرة", "دليل تعميمه للموظفين", "كشف بأسماء الموظفين"])
        ];
    }
}
