using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
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

// Firestore عميلٌ واحد في عمر العملية: قناة gRPC مُكلفة، والبناء من مفتاح الخدمة لا من سلسلة اتصال.
builder.Services.AddAuditFirestore(builder.Configuration.GetSection(FirestoreOptions.SectionName));

// الكتالوج ثابت لا تكتب فيه إلا أداة التصدير، فيُقرأ من السحابة مرة واحدة في عمر العملية ويبقى
// في الذاكرة. أما الباقي فمتاجر جلسة: لا تحتفظ بشيء، فكل واحدة تُبنى مع الطلب.
builder.Services.AddSingleton<IAuditCatalog, FirestoreAuditCatalog>();
builder.Services.AddScoped<IAccountDirectory, FirestoreAccountDirectory>();
builder.Services.AddSingleton<EvidenceStorage>();
builder.Services.AddScoped<IEvidenceStore, FirestoreEvidenceStore>();
builder.Services.AddScoped<INotificationStore, FirestoreNotificationStore>();
builder.Services.AddScoped<IMessageStore, FirestoreMessageStore>();

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

    // شاشة تدقيق جهة واحدة: الحق موسّع هنا لأن المكوّن نفسه يحبس مدقّق الجهة داخل جهته،
    // ونقطة الملفات تتحقق من جهة الملف قبل أن ترسل بايتاته.
    options.AddPolicy(AuthorizationPolicies.CanReviewBoard, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.IsFinalReviewer() || context.User.IsEntityAuditor()));

    // صفحة النتيجة النهائية: صاحبها لا يدقّق، فلا يدخلها من له حق تدقيق. هي لقارئ النتيجة وللمدير.
    options.AddPolicy(AuthorizationPolicies.CanReadResults, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.CanReadFinalResults()));
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
    // المهلة لطلب واحد — إرسال أو استطلاع — لا لجلسة فحص كاملة، فال فحص صار مهمةً عند سيرفر الوكلاء.
    client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
});
builder.Services.AddScoped<AuditAiClient>();

var app = builder.Build();

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

var supportedCultures = new[] { "ar", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("ar")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

// مصدر اللغة كوكي الموقع وحده: ترويسة المتصفح تُسقط نصف شاشة بالعربية ونصفها بالإنجليزية
// قبل أن يختار صاحب الحساب لغةً، فالافتراضي يبقى العربية حتى يضغط زر اللغة.
localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
{
    new LanguageCookieProvider()
};

app.UseRequestLocalization(localizationOptions);

using (var scope = app.Services.CreateScope())
{
    // الحسابات وحدها ما يُنشأ عند الإقلاع: مفتاح الوثيقة رقم وظيفي، فالناقص يُضاف دون أن يمسّ
    // أحدٌ حساباً غيّر صاحبه كلمته. أما الكتالوج فثابت لا تكتب فيه إلا أداة التصدير، وإلا
    // الزرع المعلّق في نموذج EF الذي يولّد ترحيلاً يمحو ما كتبه صاحب اللوحة بيده.
    var directory = scope.ServiceProvider.GetRequiredService<IAccountDirectory>();
    await directory.SeedInitialUsersAsync();
}

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapReviewEndpoints();
app.MapBoardEvidenceEndpoints();
app.MapFinalResultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();