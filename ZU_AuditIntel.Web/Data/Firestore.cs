using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// قسم Firestore في appsettings.json. المفتاح لا يدخل المستودع: مسارُه يُذكر هنا وحده،
    /// ومتى تُرك فارغاً قرأت Google المتغيّر GOOGLE_APPLICATION_CREDENTIALS نيابةً عنه.
    /// </summary>
    public sealed class FirestoreOptions
    {
        public const string SectionName = "Firestore";

        public string ProjectId { get; set; } = "zu-autointellgent";
        public string CredentialsPath { get; set; } = "";
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

                // المسار يُحمَّل لا يُمرَّر إلى CredentialsPath: تلك خاصية مهجورة في الحزمة، والاعتماد
                // الصريح هو ما توصي به. ومتى تُرِكَ المسار فارغاً بقي السقوط إلى متغيّر البيئة.
                var credential = string.IsNullOrWhiteSpace(options.CredentialsPath)
                    ? null
                    : CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();

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
