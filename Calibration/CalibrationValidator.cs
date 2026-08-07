using System;
using HondaTuner.Core.Interfaces;

namespace HondaTuner.Calibration
{
    /// <summary>
    /// Yapılan kalibrasyon değişikliklerinin motor güvenliğini tehlikeye atmadığını denetleyen doğrulayıcı.
    /// </summary>
    public class CalibrationValidator
    {
        public void Validate(CalibrationChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));

            // Basit sayısal değer ayrıştırmaları ve koruyucu sınırlar
            if (change.Parameter != null)
            {
                string paramUpper = change.Parameter.ToUpperInvariant();

                if (paramUpper.Contains("REV LIMIT") || paramUpper.Contains("REVLIMIT"))
                {
                    if (double.TryParse(change.NewValue, out double revLimit))
                    {
                        if (revLimit < 4000 || revLimit > 10000)
                        {
                            throw new ArgumentOutOfRangeException(change.Parameter,
                                "Devir kesici limiti (Rev Limit) 4000 RPM ile 10000 RPM arasında olmalıdır.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Devir kesici değeri geçersiz sayısal formatta.", change.Parameter);
                    }
                }
                else if (paramUpper.Contains("VTEC") && (paramUpper.Contains("RPM") || paramUpper.Contains("LIMIT")))
                {
                    if (double.TryParse(change.NewValue, out double vtecRpm))
                    {
                        // 0 ise VTEC inaktif/Non-VTEC profili olabilir
                        if (vtecRpm != 0 && (vtecRpm < 1000 || vtecRpm > 9500))
                        {
                            throw new ArgumentOutOfRangeException(change.Parameter,
                                "VTEC açma devri 1000 RPM ile 9500 RPM arasında olmalıdır (veya 0).");
                        }
                    }
                }
                else if (paramUpper.Contains("SPEED LIMIT") || paramUpper.Contains("SPEEDLIMIT"))
                {
                    if (double.TryParse(change.NewValue, out double speed))
                    {
                        if (speed < 50 || speed > 300)
                        {
                            throw new ArgumentOutOfRangeException(change.Parameter,
                                "Hız sınırı 50 km/h ile 300 km/h arasında olmalıdır.");
                        }
                    }
                }
                else if (paramUpper.Contains("INJECTOR") || paramUpper.Contains("CC"))
                {
                    if (double.TryParse(change.NewValue, out double cc))
                    {
                        if (cc < 100 || cc > 2400)
                        {
                            throw new ArgumentOutOfRangeException(change.Parameter,
                                "Enjektör debisi 100cc ile 2400cc arasında olmalıdır.");
                        }
                    }
                }
            }

            // Fuel & Ignition Map sınırı denetimi
            if (change.MapName != null)
            {
                if (change.MapName.Contains("Fuel"))
                {
                    if (byte.TryParse(change.NewValue, out byte val))
                    {
                        // Fuel map byte modifikasyonu için 0 ile 255 arası her byte geçerlidir
                    }
                    else
                    {
                        throw new ArgumentException("Yakıt haritası hücresi byte (0-255) türünde olmalıdır.");
                    }
                }
                else if (change.MapName.Contains("Ignition"))
                {
                    if (byte.TryParse(change.NewValue, out byte val))
                    {
                        // Ham ateşleme değeri (genellikle derece) 0-60 aralığında olmalıdır (koruma kararı)
                        if (val > 60)
                        {
                            throw new ArgumentOutOfRangeException(change.Parameter,
                                "Ateşleme avansı değeri aşırı yüksek (> 60 derece). Güvenlik sınırı aşıldı.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Ateşleme haritası hücresi geçerli avans değeri olmalıdır.");
                    }
                }
            }
        }
    }
}
