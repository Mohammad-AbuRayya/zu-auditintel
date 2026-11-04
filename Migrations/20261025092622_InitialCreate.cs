using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZU_AuditIntel.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DepartmentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "AuditItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DepartmentId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsoReference = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Workflow = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditItems_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuditItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceRequirements_AuditItems_AuditItemId",
                        column: x => x.AuditItemId,
                        principalTable: "AuditItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuditItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_AuditItems_AuditItemId",
                        column: x => x.AuditItemId,
                        principalTable: "AuditItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "DepartmentId", "DepartmentName" },
                values: new object[,]
                {
                    { "admission", "دائرة القبول والتسجيل" },
                    { "faculties", "الكليات الأكاديمية" },
                    { "health_center", "المركز الصحي" },
                    { "hr", "دائرة الموارد البشرية" },
                    { "it_center", "مركز الحاسوب" },
                    { "library", "دائرة المكتبة العامة" },
                    { "postgraduate", "عمادة الدراسات العليا" },
                    { "presidency", "ديوان الرئاسة" },
                    { "procurement", "دائرة اللوازم والمشتريات" },
                    { "quality_unit", "وحدة التطوير وضمان الجودة" },
                    { "scientific_research", "عمادة البحث العلمي" },
                    { "student_affairs", "عمادة شؤون الطلبة" }
                });

            migrationBuilder.InsertData(
                table: "AuditItems",
                columns: new[] { "Id", "Category", "DepartmentId", "IsoReference", "SortOrder", "Title", "Workflow" },
                values: new object[,]
                {
                    { 1, "Common", "library", "5.3", 1, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد للمكتبة العامة يوضح الشعب والأقسام وخطوط الإشراف ومطابقته للكادر الحالي." },
                    { 2, "Common", "library", "7.2", 2, "الوصف الوظيفي", "التحقق من وجود بطاقات وصف وظيفي معتمدة لجميع المسميات الوظيفية في المكتبة وتوقيع الموظفين عليها." },
                    { 3, "Common", "library", "7.1.6 / 7.5", 3, "ضبط الوثائق والسجلات", "التحقق من ضبط فهارس المكتبة (الورقية / الإلكترونية) وكافة السجلات والنماذج المعتمدة وفق متطلبات نظام الجودة." },
                    { 4, "Common", "library", "", 4, "الخطة الاستراتيجية", "" },
                    { 5, "Common", "library", "", 5, "مؤشرات الأداء", "" },
                    { 6, "Common", "library", "", 6, "تحليل SWOT", "" },
                    { 7, "Common", "library", "", 7, "إدارة المخاطر", "" },
                    { 8, "Common", "library", "", 8, "التدريب والكفاءة", "" },
                    { 9, "Common", "library", "", 9, "الشكاوى ورضا المستفيدين", "" },
                    { 10, "DepartmentSpecific", "library", "7.1.3 / 7.1.6", 10, "نظام إدارة المكتبة", "التحقق من كفاءة النظام الآلي لإدارة المكتبة في الفهرسة والبحث والإعارة وإصدار التقارير الإحصائية الدقيقة." },
                    { 11, "DepartmentSpecific", "library", "", 11, "الإعارة", "" },
                    { 12, "DepartmentSpecific", "library", "", 12, "المصادر الإلكترونية", "" },
                    { 13, "DepartmentSpecific", "library", "", 13, "رضا المستفيدين", "" },
                    { 100, "DepartmentSpecific", "health_center", "5.3", 14, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد يحدد العيادات والتمريض والصيدلية والاستقبال وخطوط السلطة والمسؤولية." },
                    { 101, "DepartmentSpecific", "health_center", "5.3 / 7.2", 15, "الوصف الوظيفي", "التحقق من أن لكل مسمى وظيفي بطاقة وصف وظيفي معتمدة وموقعة مع سريان تراخيص مزاولة المهنة الصحية." },
                    { 102, "DepartmentSpecific", "health_center", "5.2", 16, "السياسة الصحية", "التحقق من وجود سياسة صحية وتعليمات واضحة لتقديم الرعاية الصحية والتأمين الصحي ومعلنة ومفهومة." },
                    { 200, "DepartmentSpecific", "admission", "5.3", 17, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد للدائرة يوضح شعب القبول والتسجيل والوثائق ومطابقته للكادر الفعلي." },
                    { 201, "DepartmentSpecific", "admission", "5.3 / 7.2", 18, "الوصف الوظيفي", "التحقق من وجود بطاقات وصف وظيفي معتمدة لمسجلي التخصصات وموظفي القبول والوثائق وتوقيعهم بالعلم." },
                    { 300, "DepartmentSpecific", "hr", "5.3", 19, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لدائرة الموارد البشرية ومطابقته للكادر الفعلي." },
                    { 400, "DepartmentSpecific", "presidency", "4.2", 20, "تحديد الأطراف المعنية", "التحقق من قيام ديوان الرئاسة بحصر كافة الجهات التي يتعامل معها وتحديد متطلبات كل جهة." },
                    { 500, "DepartmentSpecific", "student_affairs", "5.3", 21, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة شؤون الطلبة يوضح الأقسام وشعب وخطوط السلطة." },
                    { 600, "DepartmentSpecific", "it_center", "5.3", 22, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لمركز الحاسوب يوضح الأقسام والشعب التقنية والبرمجية." },
                    { 700, "DepartmentSpecific", "quality_unit", "5.3", 23, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لوحدة التطوير وضمان الجودة ولجان وضباط الارتباط." },
                    { 800, "DepartmentSpecific", "postgraduate", "5.3", 24, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة الدراسات العليا ومجلس الدراسات العليا والأقسام التابعة." },
                    { 900, "DepartmentSpecific", "faculties", "5.3", 25, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد للكلية يوضح العمادة والمجالس والأقسام الأكاديمية واللجان." },
                    { 1000, "DepartmentSpecific", "scientific_research", "5.3", 26, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لعمادة البحث العلمي ومجلس البحث العلمي والمجلات العلمية." },
                    { 1100, "DepartmentSpecific", "procurement", "5.3", 27, "الهيكل التنظيمي", "التحقق من وجود هيكل تنظيمي معتمد لدائرة اللوازم والمشتريات يوضح شعب الشراء والمستودعات." }
                });

            migrationBuilder.InsertData(
                table: "EvidenceRequirements",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 1501, 1, 1, "نسخة الهيكل المعتمد" },
                    { 1502, 1, 2, "قرار الاعتماد" },
                    { 1503, 1, 3, "سجل التحديثات" },
                    { 1504, 1, 4, "تعميم داخلي" },
                    { 2501, 2, 1, "بطاقات الوصف الوظيفي" },
                    { 2502, 2, 2, "تواقيع الموظفين" },
                    { 2503, 2, 3, "سجل التحديث" },
                    { 3501, 3, 1, "سجل الوثائق" },
                    { 3502, 3, 2, "سجل الإصدارات" },
                    { 3503, 3, 3, "طلبات تعديل الوثائق" },
                    { 3504, 3, 4, "نظام الأرشفة" },
                    { 4501, 4, 1, "وثيقة الخطة" },
                    { 4502, 4, 2, "تقارير متابعة" },
                    { 4503, 4, 3, "مؤشرات الأداء" },
                    { 5501, 5, 1, "تقارير KPI" },
                    { 5502, 5, 2, "تحليل الأداء" },
                    { 5503, 5, 3, "خطط التحسين" },
                    { 6501, 6, 1, "تقرير SWOT" },
                    { 6502, 6, 2, "محاضر الاجتماعات" },
                    { 7501, 7, 1, "سجل المخاطر" },
                    { 7502, 7, 2, "خطة المعالجة" },
                    { 7503, 7, 3, "تقارير المراجعة" },
                    { 8501, 8, 1, "خطة تدريب" },
                    { 8502, 8, 2, "سجلات حضور" },
                    { 8503, 8, 3, "تقارير التقييم" },
                    { 9501, 9, 1, "نماذج الشكاوى" },
                    { 9502, 9, 2, "تقارير التحليل" },
                    { 9503, 9, 3, "استبانات الرضا" },
                    { 10501, 10, 1, "نظام المكتبة" },
                    { 10502, 10, 2, "تقارير الاستخدام" },
                    { 11501, 11, 1, "سجلات الإعارة" },
                    { 12501, 12, 1, "اشتراكات" },
                    { 12502, 12, 2, "تقارير التحديث" },
                    { 13501, 13, 1, "اشتراكات" },
                    { 13502, 13, 2, "تقارير التحديث" },
                    { 100501, 100, 1, "نسخة الهيكل التنظيمي المعتمد والموقع للمركز الصحي" },
                    { 100502, 100, 2, "كشف بأسماء الكادر الطبي والتمريضي والإداري" },
                    { 101501, 101, 1, "بطاقات الوصف الوظيفي المعتمدة" },
                    { 101502, 101, 2, "عينة 3 موظفين (طبيب، ممرض، صيدلاني)" },
                    { 101503, 101, 3, "نسخ مزاولات المهنة السارية" },
                    { 102501, 102, 1, "وثيقة السياسة الصحية وتعليمات المركز الصحي" },
                    { 102502, 102, 2, "شواهد إعلان السياسة الصحية داخل المركز وعلى الموقع" },
                    { 200501, 200, 1, "نسخة الهيكل التنظيمي المعتمد والموقع" },
                    { 200502, 200, 2, "دليل تعميم الهيكل على موظفي الدائرة" },
                    { 200503, 200, 3, "كشف بأسماء الموظفين" },
                    { 201501, 201, 1, "بطاقات الوصف الوظيفي لكافة الوظائف" },
                    { 201502, 201, 2, "عينة 3 موظفين من نماذج استلام الوصف الوظيفي الموقعة" },
                    { 300501, 300, 1, "نسخة الهيكل التنظيمي المعتمد والموقع" },
                    { 300502, 300, 2, "دليل تعميمه للموظفين" },
                    { 300503, 300, 3, "كشف بأسماء موظفي الدائرة" },
                    { 400501, 400, 1, "سجل مصفوفة حصر الأطراف المعنية" },
                    { 400502, 400, 2, "آلية متابعة تلبية متطلبات الأطراف المعنية وتحديثها" },
                    { 500501, 500, 1, "نسخة الهيكل التنظيمي المعتمد والموقع للعمادة" },
                    { 500502, 500, 2, "دليل تعميمه للموظفين" },
                    { 500503, 500, 3, "كشف بأسماء الموظفين" },
                    { 600501, 600, 1, "نسخة الهيكل التنظيمي المعتمد والموقع لمركز الحاسوب" },
                    { 600502, 600, 2, "دليل تعميمه للموظفين" },
                    { 700501, 700, 1, "نسخة الهيكل التنظيمي المعتمد" },
                    { 700502, 700, 2, "قرارات تشكيل مجلس الجودة وضباط الارتباط" },
                    { 700503, 700, 3, "كشف بالأسماء" },
                    { 800501, 800, 1, "نسخة الهيكل التنظيمي المعتمد" },
                    { 800502, 800, 2, "قرار تشكيل مجلس الدراسات العليا" },
                    { 800503, 800, 3, "كشف بأسماء كادر العمادة" },
                    { 900501, 900, 1, "نسخة الهيكل التنظيمي المعتمد" },
                    { 900502, 900, 2, "قرارات تشكيل مجلس الكلية ومجالس الأقسام" },
                    { 900503, 900, 3, "كشف بأعضاء الهيئة التدريسية" },
                    { 1000501, 1000, 1, "نسخة الهيكل التنظيمي المعتمد" },
                    { 1000502, 1000, 2, "قرار تشكيل مجلس البحث العلمي وهيئات تحرير المجلات" },
                    { 1000503, 1000, 3, "كشف بالأسماء" },
                    { 1100501, 1100, 1, "نسخة الهيكل التنظيمي المعتمد والموقع للدائرة" },
                    { 1100502, 1100, 2, "دليل تعميمه للموظفين" },
                    { 1100503, 1100, 3, "كشف بأسماء الموظفين" }
                });

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "AuditItemId", "SortOrder", "Text" },
                values: new object[,]
                {
                    { 1001, 1, 1, "هل يوجد هيكل تنظيمي معتمد ومحدث؟" },
                    { 1002, 1, 2, "هل يوضح خطوط السلطة والمسؤوليات؟" },
                    { 1003, 1, 3, "هل يتم مراجعته وتعميمه دورياً؟" },
                    { 2001, 2, 1, "هل يوجد وصف وظيفي لكل موظف؟" },
                    { 2002, 2, 2, "هل يتوافق مع المهام الفعلية؟" },
                    { 2003, 2, 3, "هل يتم تحديثه بشكل دوري؟" },
                    { 3001, 3, 1, "هل يتم تطبيق إجراء ضبط الوثائق؟" },
                    { 3002, 3, 2, "هل يتم التحكم بالإصدارات؟" },
                    { 3003, 3, 3, "هل يتم أرشفة الوثائق وإتاحتها؟" },
                    { 4001, 4, 1, "هل توجد خطة استراتيجية معتمدة؟" },
                    { 4002, 4, 2, "هل تتماشى مع رؤية الجامعة؟" },
                    { 4003, 4, 3, "هل يتم تتبع تنفيذها؟" },
                    { 5001, 5, 1, "هل توجد مؤشرات أداء واضحة؟" },
                    { 5002, 5, 2, "هل يتم قياسها وتحليلها دورياً؟" },
                    { 5003, 5, 3, "هل تتخذ إجراءات بناءً على النتائج؟" },
                    { 6001, 6, 1, "هل تم إعداد تحليل SWOT؟" },
                    { 6002, 6, 2, "هل يتم تحديثه واستخدامه في التخطيط؟" },
                    { 7001, 7, 1, "هل تم تحديد وتقييم المخاطر؟" },
                    { 7002, 7, 2, "هل توجد خطط معالجة ومراجعة دورية؟" },
                    { 8001, 8, 1, "هل توجد خطة تدريب سنوية؟" },
                    { 8002, 8, 2, "هل يتم تنفيذها وتقييم أثرها؟" },
                    { 9001, 9, 1, "هل توجد آلية فعالة للشكاوى؟" },
                    { 9002, 9, 2, "هل يتم تحليل الشكاوى وقياس رضا المستفيدين؟" },
                    { 9003, 9, 3, "هل تنفذ تحسينات؟" },
                    { 10001, 10, 1, "هل يوجد نظام مكتبة فعال؟" },
                    { 10002, 10, 2, "هل يتم تحديثه وصيانته؟" },
                    { 11001, 11, 1, "هل توجد إجراءات إعارة واضحة؟" },
                    { 11002, 11, 2, "هل يتم توثيق العمليات؟" },
                    { 12001, 12, 1, "هل يتم تحديث الاشتراكات وقواعد البيانات؟" },
                    { 13001, 13, 1, "هل يتم قياس رضا المستفيدين؟" },
                    { 13002, 13, 2, "هل تُحل الشكاوى؟" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditItems_DepartmentId_SortOrder",
                table: "AuditItems",
                columns: new[] { "DepartmentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceRequirements_AuditItemId_SortOrder",
                table: "EvidenceRequirements",
                columns: new[] { "AuditItemId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_AuditItemId_SortOrder",
                table: "Questions",
                columns: new[] { "AuditItemId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvidenceRequirements");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "AuditItems");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
