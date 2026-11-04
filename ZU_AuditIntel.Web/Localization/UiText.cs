using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace ZU_AuditIntel.Web.Localization;

/// <summary>
/// نصوص الواجهة تُكتب في الرازر بالعربية كما وردت في الوثيقة، وهذه الطبقة تقلبها إلى الإنجليزية
/// عند طلب المستخدم. العبارة غير المترجمة تبقى عربية ولا تختفي، فلا يسقط نص من الشاشة عند نقص الترجمة.
/// </summary>
public static partial class Ui
{
    /// <summary>اسم ملف تعريف الارتباط الذي يحمل خيار اللغة، وهو مصدر اللغة الوحيد.</summary>
    public const string CookieName = "zu-lang";

    public const string Arabic = "ar";
    public const string English = "en";

    /// <summary>لغة العرض الآن، من ثقافة الطلب أو الدائرة بعد أن تختارها نقطة اللغة في الوسط.</summary>
    public static bool IsEnglish => string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, English,
        StringComparison.OrdinalIgnoreCase);

    public static string Lang => IsEnglish ? English : Arabic;

    /// <summary>اتجاه الصفحة يتبع اللغة، والشريط الجانبي يبقى مثبّتاً على يمين الشاشة في اللغتين.</summary>
    public static string Dir => IsEnglish ? "ltr" : "rtl";

    /// <summary>ترجمة عبارة ثابتة؛ وما لا يوجد له مقابل يبقى بالعربية.</summary>
    public static string T(string arabic)
        => IsEnglish && EnText.Map.TryGetValue(arabic, out var english) ? english : arabic;

    /// <summary>
    /// ترجمة قالب فيه {0} و{1}: المفتاح هو القالب العربي نفسه، فيبقى مصدر واحد للعبارة في اللغتين
    /// ولا تُترك أرقامٌ أو أسماءُ ملفات بلا سياق.
    /// </summary>
    public static string F(string arabicTemplate, params object?[] args)
    {
        var template = IsEnglish && EnText.Map.TryGetValue(arabicTemplate, out var english) ? english : arabicTemplate;
        return string.Format(CultureInfo.CurrentCulture, template, args);
    }

    /// <summary>
    /// تسمية صاحب الحساب كما تُعرض: حسابات النظام مزروعة بتسميات الأدوار لا بأسماء أشخاص
    /// («مسؤول كلية القانون»)، فتنقلب إنجليزية. اسم شخص حقيقي لا يطابق مفتاحاً فيبقى كما حُفظ.
    /// </summary>
    public static string Person(string storedName)
    {
        const string officerPrefix = "مسؤول ";
        if (IsEnglish && storedName.StartsWith(officerPrefix, StringComparison.Ordinal))
            return F("مسؤول {0}", T(storedName[officerPrefix.Length..]));

        return T(storedName);
    }
}

/// <summary>
/// خيار اللغة يُقرأ من ملف تعريف ارتباط واحد فقط، بلا سلسلة استعلام ولا من ترويسة المتصفح:
/// فمتصفّح إنجليزي لا يفتح الموقع نصف مترجم لأن نصف نصوصه لم يُرحَّل بعد.
/// </summary>
public sealed class LanguageCookieProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var code = httpContext.Request.Cookies[Ui.CookieName] switch
        {
            Ui.English => Ui.English,
            Ui.Arabic => Ui.Arabic,
            _ => null
        };

        return Task.FromResult(code is null ? null : new ProviderCultureResult(code));
    }
}
