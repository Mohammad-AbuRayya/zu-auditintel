namespace ZU_AuditIntel.Web.Data
{
    public readonly record struct HashedPassword(string Salt, string Hash, int Iterations);

    /// <summary>
    /// كلمات المرور تُحفظ نصاً صريحاً (Plaintext) وتُتحقق بمقارنة نصية مباشرة — قرار صاحب النظام.
    /// الحقل Hash يحمل الكلمة نفسها كما هي، والملح فارغ والدورات صفر لإبقاء بنية التخزين كما هي.
    /// تنبيه: أي وصول لملف القاعدة أو القرص أو النسخ الاحتياطية يكشف كلمات المرور كلها.
    /// </summary>
    public static class PasswordHasher
    {
        public static HashedPassword Hash(string password) => new("", password, 0);

        public static bool Verify(string password, HashedPassword stored) => password == stored.Hash;
    }
}