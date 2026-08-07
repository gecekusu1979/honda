using System.Collections.Generic;

namespace HondaTuner.Core.Rom.Patch
{
    /// <summary>
    /// ROM Yama Motoru (Patch Engine) servis kontratı.
    /// Tamamen JSON tabanlı yama tanımlarının ROM tamponuna uygulanması,
    /// geri alınması ve doğrulama denetimlerinin yapılmasını yönetir.
    /// </summary>
    public interface IPatchEngine
    {
        /// <summary>
        /// ROM dosyasına belirtilen yama ID'sine göre yamayı uygulamaya çalışır.
        /// </summary>
        /// <param name="romData">Ham ROM byte dizisi</param>
        /// <param name="patchId">Uygulanacak yama kimliği</param>
        /// <param name="profile">Aktif ECU profil bilgileri</param>
        /// <param name="username">İşlemi tetikleyen kullanıcı</param>
        /// <returns>Yama uygulama sonucu</returns>
        PatchResult ApplyPatch(byte[] romData, string patchId, EcuProfile profile, string username);

        /// <summary>
        /// Belirtilen yamayı aktif yama listesinden ve sistemden kayıttan çıkarır.
        /// </summary>
        /// <param name="patchId">Çıkarılacak yama kimliği</param>
        /// <returns>İşlem başarılıysa true</returns>
        bool RemovePatch(string patchId);

        /// <summary>
        /// ROM dosyasına uygulanan yamayı geri alarak orijinal haline döndürür.
        /// </summary>
        /// <param name="romData">Ham ROM byte dizisi</param>
        /// <param name="patchId">Geri alınacak yama kimliği</param>
        /// <param name="profile">Aktif ECU profil bilgileri</param>
        /// <param name="username">İşlemi tetikleyen kullanıcı</param>
        /// <returns>Geri alma sonucu</returns>
        PatchResult RollbackPatch(byte[] romData, string patchId, EcuProfile profile, string username);

        /// <summary>
        /// Yamanın verilere uygulanabilir olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="romData">Ham ROM byte dizisi</param>
        /// <param name="patchId">Doğrulanacak yama kimliği</param>
        /// <param name="profile">Aktif ECU profil bilgileri</param>
        /// <param name="errorMessage">Doğrulama başarısız ise hata açıklaması</param>
        /// <returns>Yama uygulanabilir ise true</returns>
        bool ValidatePatch(byte[] romData, string patchId, EcuProfile profile, out string errorMessage);

        /// <summary>
        /// Yama işlemi öncesi önizleme verilerini oluşturur (bayt farkı, güvenlik seviyesi vb.)
        /// </summary>
        /// <param name="romData">Ham ROM byte dizisi</param>
        /// <param name="patchId">Önizlemesi oluşturulacak yama kimliği</param>
        /// <param name="profile">Aktif ECU profil bilgisi</param>
        /// <returns>Yama önizleme raporu</returns>
        PatchPreview PreviewPatch(byte[] romData, string patchId, EcuProfile profile);

        /// <summary>
        /// Bu ECU profili için kullanılabilir olan yamaların listesini döner.
        /// </summary>
        /// <param name="profile">Aktif ECU profil bilgisi</param>
        /// <returns>Yama tanımları listesi</returns>
        List<PatchDefinition> GetAvailablePatches(EcuProfile profile);

        /// <summary>
        /// Belirli bir yamanın ROM'a uygulanıp uygulanmadığını sorgular.
        /// </summary>
        /// <param name="patchId">Yama kimliği</param>
        /// <returns>Uygulanmışsa true</returns>
        bool IsPatchApplied(string patchId);

        /// <summary>
        /// Tüm yama denetim günlüğü kayıtlarını getirir.
        /// </summary>
        IReadOnlyList<PatchAuditEntry> GetPatchAudit();

        /// <summary>
        /// Mevcut tüm yama yedeklerini getirir.
        /// </summary>
        IReadOnlyList<PatchBackup> GetBackups();
    }
}
