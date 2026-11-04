using System.Security.Cryptography;

namespace ZU_AuditIntel.Web.Data
{
    public readonly record struct HashedPassword(string Salt, string Hash, int Iterations);

    /// <summary>تجزئة كلمات المرور بـ PBKDF2-SHA256 مع ملح عشوائي لكل حساب.</summary>
    public static class PasswordHasher
    {
        // توصي OWASP بـ 600 ألف دورة لـ PBKDF2-SHA256؛ الدخول نادر بما يكفي لتحمل كلفته.
        public const int Iterations = 600_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static HashedPassword Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return new(Convert.ToBase64String(salt), Convert.ToBase64String(key), Iterations);
        }

        public static bool Verify(string password, HashedPassword stored)
        {
            if (stored.Iterations <= 0 || string.IsNullOrEmpty(stored.Salt) || string.IsNullOrEmpty(stored.Hash))
                return false;

            var salt = Convert.FromBase64String(stored.Salt);
            var expected = Convert.FromBase64String(stored.Hash);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, stored.Iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
