namespace HondaTuner.Core
{
    /// <summary>
    /// Tek bir araç/donanım kaydı.
    /// </summary>
    public class VehicleEntry
    {
        public string Make { get; }   // Honda / Acura
        public string Model { get; }   // Civic, Integra…
        public string Trim { get; }   // EX, Si, GSR…
        public string YearRange { get; }   // "1992-1995"
        public string EngineCode { get; }   // D16Z6, B18C1…
        public float Displacement { get; }   // litre
        public int HorsePower { get; }   // HP (stock)
        public string Transmission { get; }   // "Manuel" / "Otomatik / Manuel"
        public string Region { get; }   // "USDM", "JDM", "EDM"
        public string Notes { get; }   // Ekstra bilgi

        public VehicleEntry(string make, string model, string trim,
            string yearRange, string engineCode, float displacement,
            int horsePower, string transmission, string region, string notes = "")
        {
            Make = make;
            Model = model;
            Trim = trim;
            YearRange = yearRange;
            EngineCode = engineCode;
            Displacement = displacement;
            HorsePower = horsePower;
            Transmission = transmission;
            Region = region;
            Notes = notes;
        }

        public override string ToString() =>
            $"{Make} {Model} {Trim} ({YearRange}) — {EngineCode}  {HorsePower}HP";
    }

    /// <summary>
    /// Bir ECU profili + ona ait araç listesini bir arada tutar.
    /// </summary>
    public class EcuRecord
    {
        public EcuProfile Profile { get; }
        public VehicleEntry[] Vehicles { get; }
        public string Category { get; }  // "Civic", "Integra"…
        public string VtecType { get; }  // "SOHC VTEC", "DOHC VTEC", "Non-VTEC"…
        public string ShortDescription { get; }

        public EcuRecord(EcuProfile profile, string category,
                         string vtecType, string shortDescription,
                         VehicleEntry[] vehicles)
        {
            Profile = profile;
            Category = category;
            VtecType = vtecType;
            ShortDescription = shortDescription;
            Vehicles = vehicles;
        }
    }

    /// <summary>
    /// Desteklenen tüm ECU'lar ve araç eşleşmeleri.
    /// Kaynak: pgmfi.org, honda-tech.com, hamotorsports.com
    /// </summary>
    public static class EcuDatabase
    {
        public static readonly EcuRecord[] Records =
        {
            // ───────────────────────── P05 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P05,
                category: "Civic",
                vtecType: "VTEC-E (Ekonomi)",
                shortDescription: "Düşük emisyon / yakıt tasarrufu odaklı VTEC-E motor. Performans odaklı değil.",
                vehicles: new[]
                {
                    new VehicleEntry("Honda","Civic","CX HF","1992-1995","D15Z1",1.5f,70,"Manuel","USDM",
                        "VTEC-E: düşük devirde tek supap çalışır — yakıt tasarrufu"),
                }),

            // ───────────────────────── P06 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P06,
                category: "Civic",
                vtecType: "Non-VTEC",
                shortDescription: "Standart 1.5L motor. VTEC devresi yok. Chipleme ile B-serisi swap'larda popüler.",
                vehicles: new[]
                {
                    new VehicleEntry("Honda","Civic","DX","1992-1995","D15B7",1.5f,102,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Honda","Civic","LX","1992-1995","D15B7",1.5f,102,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Honda","Civic","DX","1992-1995","D15B8",1.5f,70,"Otomatik","USDM",
                        "Otomatik vites versiyonu"),
                }),

            // ───────────────────────── P28 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P28,
                category: "Civic",
                vtecType: "SOHC VTEC",
                shortDescription: "En popüler OBD1 ECU. D16Z6 motor. Swap ve tuning için referans platform.",
                vehicles: new[]
                {
                    new VehicleEntry("Honda","Civic","EX","1992-1995","D16Z6",1.6f,125,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Honda","Civic","Si","1992-1995","D16Z6",1.6f,125,"Manuel","USDM"),
                    new VehicleEntry("Honda","Del Sol","Si","1993-1995","D16Z6",1.6f,125,"Manuel","USDM",
                        "Del Sol çatısız 2 kişilik"),
                    new VehicleEntry("Honda","Civic","1.6 iES","1996-2000","D16Z6",1.6f,125,"Manuel","EK/Türkiye",
                        "EK kasa iES — P28 OBD1 dönüşümü ile tuning"),
                    new VehicleEntry("Honda","Civic","1.6 iES Yumurta Kasa","1996-2000","D16Y8/D16Y6",1.6f,120,"Manuel/Otomatik","TR/EK",
                        "Yumurta kasa iES: OBD1 P28/P06 dönüşümüyle basemap ve sokak ayarı"),
                    new VehicleEntry("Honda","Civic","1.6 VTEC Swap","1996-2000","D16Z6/D16Y8",1.6f,125,"Manuel","TR/EK",
                        "iES kasaya SOHC VTEC swap veya mini-me kurulumları için"),
                }),

            // ───────────────────────── P30 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P30,
                category: "Civic",
                vtecType: "Non-VTEC",
                shortDescription: "EG kasa 1.5i. Non-VTEC D15B2. Düşük maliyetli chip platform.",
                vehicles: new[]
                {
                    new VehicleEntry("Honda","Civic","1.5i","1992-1995","D15B2",1.5f,90,"Manuel/Otomatik","EG/EK"),
                    new VehicleEntry("Honda","Civic","DX","1992-1993","D15B2",1.5f,90,"Manuel","USDM"),
                }),

            // ───────────────────────── P61 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P61,
                category: "Integra",
                vtecType: "DOHC VTEC",
                shortDescription: "1.7L B17A1 DOHC VTEC. İlk Integra GS-R nesli. 8200 RPM sınır.",
                vehicles: new[]
                {
                    new VehicleEntry("Acura","Integra","GS-R","1992-1993","B17A1",1.7f,160,"Manuel","USDM",
                        "İlk DOHC VTEC Integra — B17A1"),
                }),

            // ───────────────────────── P72 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P72,
                category: "Integra",
                vtecType: "DOHC VTEC + IAB",
                shortDescription: "B18C1 DOHC VTEC + Intake Air Bypass. 170HP stock. Efsanevi tuning platformu.",
                vehicles: new[]
                {
                    new VehicleEntry("Acura","Integra","GSR","1994-1995","B18C1",1.8f,170,"Manuel","USDM",
                        "IAB (Intake Air Bypass) solenoidi mevcut — P72'ye özel"),
                    new VehicleEntry("Honda","Integra","Type-R","1995-2001","B18C5",1.8f,195,"Manuel","JDM",
                        "ITR — P73 ECU; P72 swap'la uyumlu"),
                }),

            // ───────────────────────── P74 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P74,
                category: "Integra",
                vtecType: "Non-VTEC",
                shortDescription: "B18B1 DOHC Non-VTEC. LS Vtec swap için temel ECU.",
                vehicles: new[]
                {
                    new VehicleEntry("Acura","Integra","LS","1992-1995","B18B1",1.8f,142,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Acura","Integra","GS","1992-1995","B18B1",1.8f,142,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Acura","Integra","RS","1992-1995","B18B1",1.8f,142,"Otomatik","USDM"),
                }),

            // ───────────────────────── P13 ──────────────────────────
            new EcuRecord(
                EcuProfiles.P13,
                category: "Prelude",
                vtecType: "DOHC VTEC",
                shortDescription: "H22A 2.2L DOHC VTEC. Prelude serisinin güçlü kalbi. 190HP JDM.",
                vehicles: new[]
                {
                    new VehicleEntry("Honda","Prelude","VTEC","1993-1996","H22A",2.2f,160,"Manuel/Otomatik","USDM"),
                    new VehicleEntry("Honda","Prelude","Si VTEC","1993-1996","H22A",2.2f,190,"Manuel","JDM",
                        "JDM versiyonu 190HP"),
                    new VehicleEntry("Honda","Accord","VTEC","1994-1997","H22A",2.2f,190,"Manuel","JDM/EDM",
                        "JDM/EDM Accord SiR — aynı motor, farklı kamera"),
                }),
        };

        /// <summary>ECU koduna göre kayıt döndürür.</summary>
        public static EcuRecord GetByCode(string ecuCode)
        {
            foreach (var r in Records)
                if (r.Profile.EcuCode == ecuCode) return r;
            return null;
        }

        /// <summary>Kategoriye göre filtrele (Civic, Integra, Prelude).</summary>
        public static EcuRecord[] GetByCategory(string category)
        {
            var list = new System.Collections.Generic.List<EcuRecord>();
            foreach (var r in Records)
                if (r.Category == category) list.Add(r);
            return list.ToArray();
        }

        /// <summary>Tüm benzersiz kategoriler.</summary>
        public static string[] Categories
        {
            get
            {
                var seen = new System.Collections.Generic.HashSet<string>();
                var list = new System.Collections.Generic.List<string>();
                foreach (var r in Records)
                    if (seen.Add(r.Category)) list.Add(r.Category);
                return list.ToArray();
            }
        }
    }
}
