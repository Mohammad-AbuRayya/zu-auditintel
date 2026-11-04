using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using ZU_AuditIntel.Web;
using ZU_AuditIntel.Web.Components;
using ZU_AuditIntel.Web.Data;
using ZU_AuditIntel.Web.Endpoints;
using ZU_AuditIntel.Web.Localization;
using ZU_AuditIntel.Web.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// تشغيل سيرفر بايثون للذكاء الاصطناعي تلقائياً بصمت في الخلفية
// ==========================================
/*try
{
    var aiScriptPath = Path.Combine(builder.Environment.ContentRootPath, "..", "ZU_AuditIntel.AI");

    var psi = new ProcessStartInfo
    {
        FileName = "py",
        Arguments = "-m uvicorn main:app --port 8000",
        WorkingDirectory = aiScriptPath,
        UseShellExecute = false,
        CreateNoWindow = true,        // يمنع ظهور الشاشة السوداء تماماً
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    var aiProcess = Process.Start(psi);

    // إغلاق سيرفر بايثون تلقائياً عند إيقاف موقع الـ Blazor
    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
    {
        try
        {
            if (aiProcess is not null && !aiProcess.HasExited)
            {
                aiProcess.Kill();
                aiProcess.Dispose();
            }
        }
        catch { }
    };
}
catch (Exception ex)
{
    Console.WriteLine($"تعذر تشغيل سيرفر الذكاء الاصطناعي تلقائياً: {ex.Message}");
}*/

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("AuditIntel")
    ?? throw new InvalidOperationException("Connection string 'AuditIntel' is not configured.");

builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IAuditCatalog, EfAuditCatalog>();
builder.Services.AddScoped<IAccountDirectory, EfAccountDirectory>();
builder.Services.AddSingleton<EvidenceStorage>();
builder.Services.AddScoped<IEvidenceStore, EfEvidenceStore>();
builder.Services.AddScoped<INotificationStore, EfNotificationStore>();
builder.Services.AddScoped<IMessageStore, EfMessageStore>();

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

builder.Services.AddHttpClient("AiAgentClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:8001");
    client.Timeout = TimeSpan.FromMinutes(4);
});

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

// SQLite بوضعه الافتراضي لا يحتمل قارئاً وكاتِباً معاً: لوحة الجهة تُعيد رسم نفسها أثناء حفظ
// الملفات، فيمسك اتصالُ القراءة القفلَ المشترك ويرفض الحفظ بـ«database is locked». وضع Wal يفصل
// بينهما، لكنه يُخزَّن في رأس الملف فالتحويل إليه يحتاج قفلاً استحواذياً؛ وإن كان تطبيقٌ آخر
// ما زال ماسكاً القاعدة — نسخةٌ لم تُغلق بعد مثلاً — ترتدّ المحاولة، وهذا لا يبيح قتل التطبيق
// عند الإقلاع: يبقى على وضعه ويُحوَّل في إقلاعٍ خالٍ.
using (var startupConnection = new SqliteConnection(connectionString))
{
    startupConnection.Open();
    startupConnection.DefaultTimeout = 5;
    using var pragma = startupConnection.CreateCommand();
    pragma.CommandText = "PRAGMA journal_mode=Wal";
    try
    {
        if (string.Equals(pragma.ExecuteScalar()?.ToString(), "wal", StringComparison.OrdinalIgnoreCase))
        {
            pragma.CommandText = "PRAGMA synchronous=Normal";
            pragma.ExecuteNonQuery();
        }
        else
        {
            app.Logger.LogWarning("تعذّر تحويل القاعدة إلى وضع Wal لأن تطبيقاً آخر يمسكها؛ بقيت على وضعها السابق.");
        }
    }
    catch (SqliteException exception)
    {
        app.Logger.LogWarning(exception, "تعذّر تحويل القاعدة إلى وضع Wal؛ بقيت على وضعها السابق.");
    }
}

using (var scope = app.Services.CreateScope())
{
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