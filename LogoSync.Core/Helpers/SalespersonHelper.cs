// =============================================
// LogoSync.Core/Helpers/SalespersonHelper.cs
// =============================================

namespace LogoSync.Core.Helpers
{
    /// <summary>
    /// PUNTO'daki satış temsilcisi kodlarını Logo'daki karşılıklarına dönüştürür.
    /// Türkçe karakter farklılıkları (I→İ, O→Ö vb.) config üzerinden yönetilir.
    /// appsettings.json → "SalespersonMapping" bölümünden okunur.
    /// </summary>
    public class SalespersonHelper
    {
        private readonly Dictionary<string, string> _mapping;

        public SalespersonHelper(Dictionary<string, string> mapping)
        {
            // Case-insensitive lookup için
            _mapping = new Dictionary<string, string>(
                mapping ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// PUNTO temsilci kodunu Logo karşılığına dönüştürür.
        /// Mapping'de yoksa orijinal değeri döner.
        /// </summary>
        public string Resolve(string puntoCode)
        {
            if (string.IsNullOrEmpty(puntoCode))
                return puntoCode;

            return _mapping.TryGetValue(puntoCode.Trim(), out var logoCode)
                ? logoCode
                : puntoCode;
        }
    }
}
