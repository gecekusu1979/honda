namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// Yama işleminde uygulanacak doğrulama kuralları.
    /// </summary>
    public enum ValidationRule
    {
        RequireExpectedBytes,
        RequireChecksumValid,
        RequireFeature,
        RequireRomSize,
        RequireCompatibleEcu,
        RequireTransaction,
        RequireBackup
    }
}
