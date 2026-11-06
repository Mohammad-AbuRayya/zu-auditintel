using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// قسم Firestore في appsettings.json. المفتاح لا يدخل المستودع: على الجهاز المطوّر يُذكر
    /// مسارُه، وعلى منصّة الاستضافة يُذكر محتواه في متغيّر بيئة، ومتى تُرك الاثنان فارغين
    /// قرأت Google المتغيّر GOOGLE_APPLICATION_CREDENTIALS نيابةً عنها.
    /// </summary>
    public sealed class FirestoreOptions
    {
        public const string SectionName = "Firestore";

        public string ProjectId { get; set; } = "zu-autointellgent";

        /// <summary>مسار ملف المفتاح على هذا الجهاز؛ يُترك فارغاً في النشر بلا قرص.</summary>
        public string CredentialsPath { get; set; } = "";

        /// <summary>محتوى المفتاح نفسه، وهو ما تعطي المنصات المستضيفة سرّها به: قيمة متغيّر بيئة.</summary>
        public string CredentialsJson { get; set; } = "";
    }

    /// <summary>أسماء المجموعات كما زرعها مصدّر الكتالوج، فتكون مرجعاً واحداً لا حرفاً مكتوباً في كل مخزن.</summary>
    public static class AuditCollections
    {
        public const string Departments = "departments";
        public const string Items = "items";
        public const string Users = "users";
        public const string Submissions = "submissions";
        public const string Notifications = "notifications";
        public const string Threads = "threads";
        public const string Messages = "messages";
    }

    public static class FirestoreServiceCollectionExtensions
    {
        /// <summary>العميلsingleton واحد لكل التطبيق: يمسك قناة gRPC والاتصال، والمتاجر تستعيره.</summary>
        public static IServiceCollection AddAuditFirestore(this IServiceCollection services, IConfigurationSection section)
        {
            services.Configure<FirestoreOptions>(section);

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<FirestoreOptions>>().Value;

                // المفتاح يُحمَّل لا يُمرَّر إلى خاصية CredentialsPath المهجورة في الحزمة. ثلاثة مصادر
                // بالترتيب: محتوى في متغيّر بيئة (وهو ما تعطي به منصّة الاستضافة سرّها)، ثم ملف على
                // القرص (وهو وضع الجهاز المطوّر)، ثم discovery القياسية من حزمة Google نفسها.
                GoogleCredential? credential = null;

                if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
                    credential = CredentialFactory
                        .FromJson<ServiceAccountCredential>(options.CredentialsJson).ToGoogleCredential();
                else if (!string.IsNullOrWhiteSpace(options.CredentialsPath))
                    credential = CredentialFactory
                        .FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();

                return new FirestoreDbBuilder
                {
                    ProjectId = options.ProjectId,
                    Credential = credential
                }.Build();
            });

            return services;
        }
    }
}
