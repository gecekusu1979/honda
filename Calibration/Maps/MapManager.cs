using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using HondaTuner.Core;
using HondaTuner.Core.Container;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Logging;

namespace HondaTuner.Calibration.Maps
{
    /// <summary>
    /// Harita yöneticisi. ECU kalibrasyon tablolarının hücre, satır, sütun ve bölge seviyesinde
    /// okunmasını, yazılmasını, kopyalama/yapıştırma ve içe/dışa aktarma işlemlerini yönetir.
    /// Tüm yazma işlemleri CalibrationTransaction ve Undo/Redo altyapısıyla entegre çalışır.
    /// </summary>
    public class MapManager
    {
        private IRomService GetRomService()
        {
            return ServiceContainer.Resolve<IRomService>();
        }

        private ICalibrationService GetCalibrationService()
        {
            return ServiceContainer.Resolve<ICalibrationService>();
        }

        /// <summary>
        /// Tanıma göre haritayı ve eksenlerini ROM'dan yükler.
        /// </summary>
        public TableDefinition LoadMap(MapDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var romService = GetRomService();
            if (romService == null || !romService.IsLoaded)
                throw new InvalidOperationException("ROM yüklenmeden harita okunamaz.");

            byte[] buffer = romService.GetBuffer();

            // Sınır kontrolü
            int mapSize = def.Rows * def.Columns;
            if (def.Offset < 0 || def.Offset + mapSize > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(def.Offset), "Harita tanımı ROM dosyası sınırlarının dışındadır.");

            // Profil elde et (Eksen offset bulmak için)
            var profile = romService.Profile;

            // X Eksenini yükle (RPM)
            var xAxis = new AxisDefinition { Name = "RPM", Unit = "RPM", ScaleFactor = 1.0 };
            int xAxisOffset = 0;

            if (profile != null)
            {
                xAxisOffset = (def.MapName.Contains("Fuel")) ? profile.FuelAxisOffset : profile.IgnitionAxisOffset;
            }
            if (xAxisOffset <= 0 || xAxisOffset + def.Columns > buffer.Length)
            {
                xAxisOffset = def.Offset - def.Columns; // Varsayılan fallback
            }

            xAxis.Offset = xAxisOffset;
            xAxis.Length = def.Columns;
            xAxis.RawValues = new byte[def.Columns];
            xAxis.ConvertedValues = new double[def.Columns];
            for (int i = 0; i < def.Columns; i++)
            {
                xAxis.RawValues[i] = buffer[xAxisOffset + i];
                // RPM ekseni çözünürlüğü: Honda standardında genellikle RPM ham byte * 50'dir.
                // Eğer standard eksense ölçekle, veya RawValues'dan dönüştür.
                xAxis.ConvertedValues[i] = xAxis.RawValues[i] * 50;
            }

            // Y Eksenini yükle (MAP / Load)
            var yAxis = new AxisDefinition { Name = "MAP/Load", Unit = "kPa", ScaleFactor = 1.0 };
            int yAxisOffset = xAxisOffset + def.Columns; // rpm ekseninin hemen arkasında
            if (yAxisOffset <= 0 || yAxisOffset + def.Rows > buffer.Length)
            {
                yAxisOffset = xAxisOffset + 16; // varsayılan
            }

            yAxis.Offset = yAxisOffset;
            yAxis.Length = def.Rows;
            yAxis.RawValues = new byte[def.Rows];
            yAxis.ConvertedValues = new double[def.Rows];
            for (int i = 0; i < def.Rows; i++)
            {
                yAxis.RawValues[i] = buffer[yAxisOffset + i];
                // Yük ekseni çözünürlüğü: ham degeri kPa cinsinden al
                yAxis.ConvertedValues[i] = yAxis.RawValues[i];
            }

            TableDefinition table;
            if (def.MapName.Contains("Fuel"))
                table = new FuelMap(def, xAxis, yAxis);
            else if (def.MapName.Contains("Ignition"))
                table = new IgnitionMap(def, xAxis, yAxis);
            else
                table = new GenericMap(def, xAxis, yAxis);

            // Hücreleri oku
            for (int r = 0; r < def.Rows; r++)
            {
                for (int c = 0; c < def.Columns; c++)
                {
                    table.RawCells[r, c] = buffer[def.Offset + (r * def.Columns) + c];
                }
            }

            table.RefreshConvertedCells();
            return table;
        }

        public double ReadCell(MapDefinition def, int row, int col)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var romService = GetRomService();
            if (romService == null || !romService.IsLoaded)
                throw new InvalidOperationException("ROM yüklenmeden okuma yapılamaz.");

            int offset = def.Offset + (row * def.Columns) + col;
            byte[] buffer = romService.GetBuffer();
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Hücre offset'i ROM sınırları dışındadır.");

            byte raw = buffer[offset];
            return raw * def.ScaleFactor + def.OffsetValue;
        }

        public void WriteCell(MapDefinition def, int row, int col, double val)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (row < 0 || row >= def.Rows || col < 0 || col >= def.Columns)
                throw new ArgumentOutOfRangeException("Hücre koordinatları harita sınırlarının dışındadır.");

            var romService = GetRomService();
            var calService = GetCalibrationService();
            if (romService == null || !romService.IsLoaded || calService == null)
                throw new InvalidOperationException("Kalibrasyon yazma altyapısı hazır değil.");

            int offset = def.Offset + (row * def.Columns) + col;
            byte[] buffer = romService.GetBuffer();
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Hücre offset'i ROM sınırları dışındadır.");

            byte oldRawValue = buffer[offset];
            double oldConvertedValue = oldRawValue * def.ScaleFactor + def.OffsetValue;

            // Güvenlik sınırlarına kırpma
            double clampedVal = Math.Max(def.MinimumValue, Math.Min(def.MaximumValue, val));

            // Mühendislik değeri -> Raw byte dönüşümü
            double rawValDouble = (clampedVal - def.OffsetValue) / def.ScaleFactor;
            byte newRawValue = (byte)Math.Clamp(Math.Round(rawValDouble), 0, 255);
            double newConvertedValue = newRawValue * def.ScaleFactor + def.OffsetValue;

            if (oldRawValue == newRawValue) return;

            var change = new CalibrationChange
            {
                Parameter = $"{def.MapName} Hücre [{row},{col}]",
                OldValue = oldRawValue.ToString(CultureInfo.InvariantCulture),
                NewValue = newRawValue.ToString(CultureInfo.InvariantCulture),
                Offset = offset,
                MapName = def.MapName,
                Source = "MapManager"
            };

            // CalibrationManager üzerinden değişikliği kaydet (Undo/Redo & transaction entegrasyonu tetiklenir)
            calService.RecordChange(change);
        }

        public double[] ReadRow(MapDefinition def, int row)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (row < 0 || row >= def.Rows) throw new ArgumentOutOfRangeException(nameof(row));

            var rowData = new double[def.Columns];
            for (int c = 0; c < def.Columns; c++)
            {
                rowData[c] = ReadCell(def, row, c);
            }
            return rowData;
        }

        public double[] ReadColumn(MapDefinition def, int col)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (col < 0 || col >= def.Columns) throw new ArgumentOutOfRangeException(nameof(col));

            var colData = new double[def.Rows];
            for (int r = 0; r < def.Rows; r++)
            {
                colData[r] = ReadCell(def, r, col);
            }
            return colData;
        }

        public double[,] CopyRegion(MapDefinition def, int startRow, int startCol, int endRow, int endCol)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            int rows = endRow - startRow + 1;
            int cols = endCol - startCol + 1;
            if (rows <= 0 || cols <= 0) throw new ArgumentException("Bölge sınırları geçersiz.");

            double[,] region = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    region[r, c] = ReadCell(def, startRow + r, startCol + c);
                }
            }
            return region;
        }

        public void PasteRegion(MapDefinition def, int startRow, int startCol, double[,] values)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            if (values == null) throw new ArgumentNullException(nameof(values));

            int rows = values.GetLength(0);
            int cols = values.GetLength(1);

            var calService = GetCalibrationService();
            bool hasOuterTx = false; // Aktif işlem varsa sarmalama

            try
            {
                if (calService is CalibrationManager manager)
                {
                    manager.BeginTransaction();
                    hasOuterTx = true;
                }

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        int targetRow = startRow + r;
                        int targetCol = startCol + c;
                        if (targetRow >= 0 && targetRow < def.Rows && targetCol >= 0 && targetCol < def.Columns)
                        {
                            WriteCell(def, targetRow, targetCol, values[r, c]);
                        }
                    }
                }

                if (hasOuterTx && calService is CalibrationManager calMgr)
                {
                    calMgr.CommitTransaction();
                }
            }
            catch
            {
                if (hasOuterTx && calService is CalibrationManager calMgr)
                {
                    calMgr.RollbackTransaction();
                }
                throw;
            }
        }

        public void ExportMap(MapDefinition def, string filePath)
        {
            var table = LoadMap(def);
            var sb = new StringBuilder();

            // Sütun ekseni (RPM) baslık satırı
            sb.Append("Load/RPM");
            for (int c = 0; c < def.Columns; c++)
            {
                sb.Append($",{table.XAxis.ConvertedValues[c].ToString("F0", CultureInfo.InvariantCulture)}");
            }
            sb.AppendLine();

            // Satır bazlı hücre verileri
            for (int r = 0; r < def.Rows; r++)
            {
                sb.Append(table.YAxis.ConvertedValues[r].ToString("F0", CultureInfo.InvariantCulture));
                for (int c = 0; c < def.Columns; c++)
                {
                    sb.Append($",{table.ConvertedCells[r, c].ToString("F2", CultureInfo.InvariantCulture)}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
            ApplicationLogger.Info("MapManager", $"{def.MapName} haritası dışa aktarıldı: {filePath}");
        }

        public void ImportMap(MapDefinition def, string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("İçe aktarılacak dosya bulunamadı.");

            var lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1) throw new InvalidDataException("Hatalı dosya yapısı.");

            double[,] values = new double[def.Rows, def.Columns];

            for (int r = 0; r < def.Rows; r++)
            {
                // İlk satır başlık satırı olduğu için r+1 okuyoruz
                if (r + 1 >= lines.Length) break;
                var tokens = lines[r + 1].Split(',');

                // token[0] satır başlığı (Load veya RPM), diğerleri hücre değerleri
                for (int c = 0; c < def.Columns; c++)
                {
                    if (c + 1 >= tokens.Length) break;
                    if (double.TryParse(tokens[c + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        values[r, c] = val;
                    }
                }
            }

            PasteRegion(def, 0, 0, values);
            ApplicationLogger.Info("MapManager", $"{def.MapName} haritası başarıyla içe aktarıldı: {filePath}");
        }
    }
}
