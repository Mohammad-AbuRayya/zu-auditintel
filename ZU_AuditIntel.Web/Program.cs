using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Threading.RateLimiting;
using ZU_AuditIntel.Web;
using ZU_AuditIntel.Web.Components;
using ZU_AuditIntel.Web.Data;
using ZU_AuditIntel.Web.Endpoints;
using ZU_AuditIntel.Web.Localization;
using ZU_AuditIntel.Web.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// مفاتيح الحماية تفكّ كوكيز الدخول وتوكنات antiforgery. في الحاوية تُكتب افتراضياً في مسارٍ
// يُمحى مع الماكينة، فيبطل عند كل إقلاعٍ بارد كل كوكيٍ صدر قبله. والمسار يأتي من الإعداد لا
// ثابتاً في الكود: التشغيل المحلي يبقى على مفتاحه الافتراضي ولا يُجبر على قرصٍ لا يملكه.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("ZU_AuditIntel");
}

// المنصّات المستضيفة تنهي TLS عند وسيطها ثم تدفع إلى الحاوية طلباً مجرّد http على المنفذ الذي
// يعطيها المتغيّر PORT. بلا قراءة ترويسات التحويل يظنّ التطبيق أنه لا https فيه إطلاقاً: فإما
// أعاد التوجيه إلى منفذٍ لا يسمعه الوسيط، أو أبى أن يصدّق كوكي الدخول الآمن لأن الطلب الداخل
// غير مؤمّن — وهو ما يظهر للعميل كثروة دخولٍ لا تنتهي. الاعتراض مفتوح لأن الوسيط واحد وهو
// المنصّة نفسها، ولا سبيل إلى وصولٍ مباشر إلى الحاوية يلتفّ عليه.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// القاعدة المحلية ملفٌ واحد على قرص دائم، وبايتات الأدلة شجرةٌ على القرص نفسه: لا قناة سحابية
// ولا حصة يومية تنفد. والمساران يأتيان من الإعداد لا ثابتَين في الكود، فالحاوية تربطهما بقرصها
// الدائم والتشغيل المحلي يبقى على App_Data داخل شجرة التطبيق.
builder.Services.Configure<LocalDbOptions>(builder.Configuration.GetSection(LocalDbOptions.SectionName));
builder.Services.AddSingleton<LocalDb>();
builder.Services.Configure<EvidenceFilesOptions>(builder.Configuration.GetSection(EvidenceFilesOptions.SectionName));
builder.Services.AddSingleton<LocalEvidenceStorage>();

// الكتالوج ثابت لا تكتب فيه إلا أداة الاستيراد، فيُقرأ مرة واحدة في عمر العملية ويبقى في الذاكرة.
// أما الباقي فمتاجر جلسة: لا تحتفظ بشيء، فكل واحدة تُبنى مع الطلب.
builder.Services.AddSingleton<IAuditCatalog, SqliteAuditCatalog>();
builder.Services.AddScoped<IAccountDirectory, SqliteAccountDirectory>();
builder.Services.AddScoped<IEvidenceStore, SqliteEvidenceStore>();
builder.Services.AddScoped<INotificationStore, SqliteNotificationStore>();
builder.Services.AddScoped<IMessageStore, SqliteMessageStore>();
builder.Services.AddScoped<ISubmissionDeadlineStore, SqliteSubmissionDeadlineStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ZU.AuditIntel.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        // المسار المرفوض لا يحمل استعلاماً: فالُموجّه يلصق ReturnUrl بعده فيخرج رابطٌ لا يُعرف (/login%3Ferror=denied?ReturnUrl=…).
        options.AccessDeniedPath = "/login";
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToAccessDenied = ctx =>
            {
                // لا ReturnUrl هنا: فالدخول الناجح يذهب إلى لوحة صاحب الحساب دائماً لا إلى ما طلبه.
                ctx.Response.Redirect("/login?error=denied");
                return Task.CompletedTask;
            }
        };
        options.ExpireTimeSpan = TimeSpan.FromHours(AccountEndpoints.SessionHours);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.CanAudit, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !context.User.HasClaim(AuditClaims.MustChangePassword, "true")));

    options.AddPolicy(AuthorizationPolicies.CanReview, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsFinalReviewer()));

    // شاشة التدقيق النهائي ونقطة ملفاتها: الحق لرئيس الوحدة ومدير النظام، وهما من يريان كل الجهات.
    options.AddPolicy(AuthorizationPolicies.CanReviewBoard, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsFinalReviewer()));
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("account-login", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });

    // حدٌّ مستقل لنقطة تصنيف استعادة كلمة المرور: لا تحرق نسياناتُ كلمة السر رصيدَ محاولات الدخول،
    // ولا تُترك النقطة بلا سقف لأنها تُغذّى بأرقام يكتبها الزائر نفسه.
    options.AddFixedWindowLimiter("staff-kind", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

builder.Services.Configure<AuditAiOptions>(builder.Configuration.GetSection(AuditAiOptions.SectionName));

builder.Services.AddHttpClient(AuditAiClient.ClientName, (sp, client) =>
{
    var ai = sp.GetRequiredService<IOptions<AuditAiOptions>>().Value;
    client.BaseAddress = new Uri(ai.BaseUrl);
    // المهلة لطلب واحد — إرسال أو استطلاع — لا لجلسة فحص كاملة، فالفحص صار مهمةً عند سيرفر الوكلاء.
    client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
});
builder.Services.AddScoped<AuditAiClient>();

var app = builder.Build();

// أول الوسائط كلها: ما لم تُقرأ الترويسة قبل أن يُسأل «هل هذا https؟» فهي باطلة عند كل قرارٍ لاحق.
if (!app.Environment.IsDevelopment())
    app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// فاصل التاريخ في ثقافة "ar" تسبقه علامة اتجاه خفيّة (U+200F)، و«/» في صيغةٍ مثل "yyyy/MM/dd"
// ليست شرطةً حرفية بل تُستبدل بالفاصل، فتتسرّب العلامة إلى كل تاريخٍ في كل شاشة. نسخةٌ بفاصلٍ
// مجرّد تُمرَّر إلى الوسيط نفسه: فهو يطابق الثقافة بالاسم ويستعمل النسخة المعطاة لا الأصل المخزّن.
// والتمرير بالنسخ لا بالأسماء لأن AddSupportedCultures تقبل نصوصاً فتبني نسخها من الأصل.
var arabicCulture = (CultureInfo)CultureInfo.GetCultureInfo("ar").Clone();
arabicCulture.DateTimeFormat.DateSeparator = "/";

var supportedCultures = new[] { arabicCulture, CultureInfo.GetCultureInfo("en") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(arabicCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// مصدر اللغة كوكي الموقع وحده: ترويسة المتصفح تُسقط نصف شاشة بالعربية ونصفها بالإنجليزية
// قبل أن يختار صاحب الحساب لغةً، فالافتراضي يبقى العربية حتى يضغط زر اللغة.
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new LanguageCookieProvider()
};

app.UseRequestLocalization(localizationOptions);

// المخطط قبل أول طلب: الجداول والفهارس تُنشأ إن لم تكن، ولا يُمسّ ما فيها. وهي كتابةٌ على قرص
// محلي لا نداءٌ عبر الشبكة، فلا تحجب الإقلاع إلا بقدر ما يستغرقه إنشاء ملف.
await app.Services.GetRequiredService<LocalDb>().InitializeAsync();

// الحسابات وحدها ما يُنشأ عند الإقلاع: مفتاح الصف رقم وظيفي، فالناقص يُضاف دون أن يمسّ أحدٌ
// حساباً غيّر صاحبه كلمته. أما الكتالوج فثابت لا تكتب فيه إلا أداة الاستيراد.
// والزرع بعد أن يستمع المنفذ لا قبله: أول زائر لا ينتظر تجزئة سبعٍ وخمسين كلمة مرور.
app.Lifetime.ApplicationStarted.Register(() => _ = SeedInitialAccountsAsync(app));

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapReviewEndpoints();
app.MapBoardEvidenceEndpoints();

// نبضة النشاط: أخفّ نقطة في التطبيق، لا تلمس قاعدة البيانات ولا تطلب مصادقة، ووظيفتها الوحيدة
// تسجيل حركة HTTP عند وسيط Fly.io فيبقى الجهاز مستيقظاً ما دام تبويبٌ مفتوح يرسلها دورياً.
// ومع auto_stop_machines ينام الجهاز وحده بعد إغلاق كل التبويبات وتوقّف النبضات.
app.MapGet("/ping", () => Results.Ok("pong"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task SeedInitialAccountsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        await scope.ServiceProvider.GetRequiredService<IAccountDirectory>().SeedInitialUsersAsync();
    }
    catch (Exception ex)
    {
        // الزرع متكرّر الآمن، فالناقص يُضاف في إقلاعٍ لاحق ولا تُقتل العملية من أجله.
        app.Logger.LogWarning(ex, "تعذّر زرع الحسابات الأولية عند الإقلاع؛ سيُعاد المحاولة في إقلاعٍ لاحق.");
    }
}