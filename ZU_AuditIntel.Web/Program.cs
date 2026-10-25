using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using ZU_AuditIntel.Web;
using ZU_AuditIntel.Web.Components;
using ZU_AuditIntel.Web.Data;
using ZU_AuditIntel.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ZU.AuditIntel.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login?error=denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(AccountEndpoints.SessionHours);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.CanAudit, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => !context.User.HasClaim(AuditClaims.MustChangePassword, "true")));
});

// حدّ محاولات الدخول: يبطئ تخمين كلمات المرور على نقطتي الدخول وتغيير الكلمة.
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("account-login", limiter =>
{
    limiter.PermitLimit = 10;
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.QueueLimit = 0;
    limiter.AutoReplenishment = true;
}));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// التحقق من الرمز المضاد للتزييف بعد المصادقة: الرمز يُصدر باسم المستخدم الحالي، وسابقاً على
// UseAuthentication كان يُقارن بمجهول فيرفض كل نموذج بعد الدخول.
app.UseAntiforgery();

var supportedCultures = new[] { "ar", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("ar")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

using (var scope = app.Services.CreateScope())
{
    var directory = scope.ServiceProvider.GetRequiredService<IAccountDirectory>();
    await directory.SeedInitialUsersAsync();
}

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
