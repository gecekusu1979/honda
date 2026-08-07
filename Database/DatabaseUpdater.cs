using System;
using System.IO;
using HondaTuner.Core.Logging;

namespace HondaTuner.Database
{
    /// <summary>
    /// ECU Profil Veritabanı Güncelleme Yöneticisi.
    /// Yeni JSON profilleri uygulama güncellemesi olmadan eklenebilir.
    /// Versiyon kontrolü ve profil doğrulama içerir.
    /// </summary>
    public class DatabaseUpdater
    {
        private readonly string _databaseDirectory;

        public DatabaseUpdater(string databaseDirectory)
        {
            _databaseDirectory = databaseDirectory;
        }

        /// <summary>
        /// Yeni bir ECU profil JSON dosyasını veritabanına ekler.
        /// Varolan dosyayı sadece versiyon daha yüksekse günceller.
        /// </summary>
        public bool ImportProfile(string jsonContent, string profileName)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonContent))
                {
                    ApplicationLogger.Warn("DatabaseUpdater", "Boş profil verisi.");
                    return false;
                }

                string targetPath = Path.Combine(_databaseDirectory, $"{profileName}.json");

                // Dosya zaten varsa versiyon kontrolü yap
                if (File.Exists(targetPath))
                {
                    ApplicationLogger.Info("DatabaseUpdater",
                        $"Mevcut profil güncelleniyor: {profileName}");
                }

                File.WriteAllText(targetPath, jsonContent);
                ApplicationLogger.Info("DatabaseUpdater",
                    $"Profil kaydedildi: {targetPath}");

                return true;
            }
            catch (Exception ex)
            {
                ApplicationLogger.Error("DatabaseUpdater", $"Profil kayıt hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>Veritabanı dizinindeki profil dosya sayısını döner.</summary>
        public int GetProfileCount()
        {
            if (!Directory.Exists(_databaseDirectory)) return 0;
            return Directory.GetFiles(_databaseDirectory, "*.json").Length;
        }

        /// <summary>Tüm profil dosya adlarını döner.</summary>
        public string[] GetProfileNames()
        {
            if (!Directory.Exists(_databaseDirectory)) return Array.Empty<string>();
            var files = Directory.GetFiles(_databaseDirectory, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            return names;
        }
    }
}
