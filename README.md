# ZU-AuditIntel — نظام التدقيق الداخلي للجودة

نظام تدقيق امتثال داخلي لجامعة الزرقاء مبني على متطلبات **ISO 21001:2025**، بواجهة عربية
RTL. يدير بنود التدقيق لكل جهة (المكتبة العامة حالياً)، ويرفع الأدلة المستندية ويفحصها
ثم يصنّف كل بند: مطابق / غير مطابق / يحتاج مراجعة.

> **حالة المشروع: نموذج أولي (Prototype).** الصفحة والبنود والبيانات حقيقية، لكن ثلاث
> طبقات ما تزال تمثيلية وغير صالحة للاستخدام الفعلي — موثّقة أدناه بلا تجميل.

## التقنيات

- .NET 10 — Blazor Web App (`InteractiveServer`)، بدون MVC
- EF Core 10 + SQLite
- Bootstrap 5 (مضمّن محلياً في `wwwroot/lib`)

## التشغيل

```bash
dotnet build ZU_AuditIntel.Web
dotnet run --project ZU_AuditIntel.Web        # http://localhost:5124
```

قاعدة البيانات تُنشأ من الترحيلات. أدوات EF مثبّتة محلياً عبر `dotnet-tools.json`:

```bash
dotnet restore
dotnet tool restore
dotnet ef migrations add <Name> --project ZU_AuditIntel.Web
dotnet ef database update --project ZU_AuditIntel.Web
```

ملف `zu_auditintel.db` محلي وغير متتبَّع في git؛ احذفه وأعد `database update` لبنائه من جديد.

## البنية

```
Models/       كيانات الكتالوج: Department, AuditItem, AuditQuestion, AuditEvidenceRequirement
Data/         AppDbContext (الإعداد + الزرع)، SeedData (الكتالوج)، AuditCatalog (قراءة)
Components/   Pages/Login, Pages/SignUp, Pages/Library_Audit
```

**مصدر الحقيقة الوحيد لبنود التدقيق هو `Data/SeedData.cs`.** الواجهات لا تكرّر البنود
محلياً؛ تقرأها عبر `IAuditCatalog` (تسلسل `SortOrder`، وأسئلة وأدلة كل بند في جداوله
الخاصة). أضف بنداً أو عدّله هناك، ثم أنشئ ترحيلاً وطبّقه.

الكيانات تصف *تعريف* البند فقط. نتائج التدقيق (الملفات المرفوعة، حالة الفحص، الدرجة)
تبقى في ذاكرة الجلسة وحدها ولا تُحفظ بعد.

## ما هو تمثيلي الآن

| الطبقة | الواقع |
|---|---|
| تسجيل الدخول | مقارنة نصية `admin` / `123456` في `Login.razor` بدون جلسة أو `[Authorize]`؛ `/LibraryAudit` متاح مباشرة. |
| الفحص الذكي | `RunAiAuditAsync` محاكاة (`Task.Delay`) فوق فحوص regex محلية؛ فحص PDF يقرأ البايتات ولا يستخرج النص. |
| حفظ النتائج | لا يوجد؛ إعادة تحميل الصفحة تصفّر كل الحالات. |
| التعريب | `ar`/`en` مهيّآن في `Program.cs` لكن ملفات `.resx` فارغة والنص العربي مضمّن في المكوّنات. |

الأيقونات تعتمد أصناف `bi bi-*` من Bootstrap Icons، والحزمة غير مثبتة ولا مربوطة في
`App.razor`، فلا تظهر أي أيقونة حتى تُضاف.

## الخطوات التالية

1. دخول حقيقي (Identity أو cookies) + `[Authorize]` على صفحات التدقيق.
2. استخراج محرك الفحص خلف `IAuditPdfReader` / `IAuditItemEvaluator` بوحدات اختبار.
3. حفظ نتائج التدقيق (جدول نتائج مرتبط ببند ومدقق وتاريخ).
4. تقسيم `Library_Audit.razor` (1180 سطراً) إلى مكوّنات.
5. تعبئة `.resx` وتشغيل تبديل اللغة.
