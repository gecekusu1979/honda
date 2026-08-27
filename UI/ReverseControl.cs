using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HondaTuner.Core;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.ReverseEngineering;

namespace HondaTuner.UI
{
    public class ReverseControl : UserControl
    {
        private IRomService _romService;
        private List<MapCandidate> _candidates = new List<MapCandidate>();
        private MapCandidate _selectedCandidate;
        private AxisMatchResult _selectedAxes;

        // UI Bileşenleri
        private DataGridView _dgvCandidates;
        private TextBox _txtSearch;
        private Button _btnScan;
        private Button _btnExtractAxes;
        private Button _btnDecompile;
        private Button _btnAdopt;

        private Label _lblRpmAxis;
        private Label _lblLoadAxis;
        private RichTextBox _rtbDecompiler;
        private ComboBox _cbRoutines;
        private TextBox _txtRoutineAddress;

        // Renk Paleti (Koyu Tema)
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public ReverseControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;
            InitializeLayout();
        }

        public void BindRomService(IRomService romService)
        {
            _romService = romService;
            ResetUI();
        }

        private void ResetUI()
        {
            _candidates.Clear();
            _selectedCandidate = null;
            _selectedAxes = null;
            _dgvCandidates.Rows.Clear();
            _rtbDecompiler.Clear();
            _lblRpmAxis.Text = "RPM Ekseni: Seçilmedi";
            _lblLoadAxis.Text = "Load Ekseni: Seçilmedi";
            _btnExtractAxes.Enabled = false;
            _btnAdopt.Enabled = false;
        }

        private void InitializeLayout()
        {
            // Panel yerleşimi: Üst toolbar, sol liste, sağ decompiler / eksen paneli
            var tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 2,
                BackColor = BgDark
            };
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // Üst Toolbar
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Alt Çalışma Alanı
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); // Sol liste
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f)); // Sağ detay/decompiler

            // 1. ÜST TOOLBAR
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 27, 38),
                Margin = new Padding(0)
            };
            tlpMain.Controls.Add(pnlToolbar, 0, 0);
            tlpMain.SetColumnSpan(pnlToolbar, 2);

            _btnScan = new Button
            {
                Text = "🔍 ROM'u Analiz Et & Tara",
                Location = new Point(12, 10),
                Size = new Size(180, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnScan.FlatAppearance.BorderSize = 0;
            _btnScan.Click += BtnScan_Click;
            pnlToolbar.Controls.Add(_btnScan);

            var lblSearch = new Label
            {
                Text = "Filtrele:",
                ForeColor = TextMuted,
                Location = new Point(210, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f)
            };
            pnlToolbar.Controls.Add(lblSearch);

            _txtSearch = new TextBox
            {
                Location = new Point(265, 12),
                Size = new Size(180, 24),
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };
            _txtSearch.TextChanged += TxtSearch_TextChanged;
            pnlToolbar.Controls.Add(_txtSearch);

            // 2. SOL PANEL (LİSTE)
            var pnlLeftHeap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 6, 6, 12)
            };
            tlpMain.Controls.Add(pnlLeftHeap, 0, 1);

            _dgvCandidates = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = BgPanel,
                ForeColor = TextPrimary,
                GridColor = Color.FromArgb(40, 48, 64),
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowTemplate = { Height = 28 }
            };
            _dgvCandidates.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 37, 52);
            _dgvCandidates.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            _dgvCandidates.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _dgvCandidates.DefaultCellStyle.BackColor = BgPanel;
            _dgvCandidates.DefaultCellStyle.ForeColor = TextPrimary;
            _dgvCandidates.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, 50, 75);
            _dgvCandidates.DefaultCellStyle.SelectionForeColor = Color.White;
            _dgvCandidates.EnableHeadersVisualStyles = false;

            _dgvCandidates.Columns.Add("Offset", "Offset");
            _dgvCandidates.Columns.Add("Type", "Harita Tipi");
            _dgvCandidates.Columns.Add("Dims", "Boyutlar");
            _dgvCandidates.Columns.Add("Confidence", "Güvenilirlik");
            _dgvCandidates.Columns.Add("Desc", "Açıklama");

            _dgvCandidates.Columns[0].Width = 70;
            _dgvCandidates.Columns[1].Width = 90;
            _dgvCandidates.Columns[2].Width = 75;
            _dgvCandidates.Columns[3].Width = 85;
            _dgvCandidates.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _dgvCandidates.SelectionChanged += DgvCandidates_SelectionChanged;
            pnlLeftHeap.Controls.Add(_dgvCandidates);

            // 3. SAĞ PANEL (DETAY & DECOMPILER)
            var pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 6, 12, 12)
            };
            tlpMain.Controls.Add(pnlRight, 1, 1);

            var tlpRightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            tlpRightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f)); // Eksen Bilgileri / Tarama
            tlpRightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Decompiler Çıktısı
            tlpRightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));  // Aksiyon Butonu
            pnlRight.Controls.Add(tlpRightLayout);

            // 3a. Eksen Kartı
            var gbAxes = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8)
            };
            tlpRightLayout.Controls.Add(gbAxes, 0, 0);

            var lblAxesTitle = new Label
            {
                Text = "Eksen Analiz Motoru",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(8, 6),
                AutoSize = true
            };
            gbAxes.Controls.Add(lblAxesTitle);

            _lblRpmAxis = new Label
            {
                Text = "RPM Ekseni: Seçilmedi",
                ForeColor = TextPrimary,
                Location = new Point(8, 28),
                Width = 170,
                Font = new Font("Segoe UI", 8.5f)
            };
            gbAxes.Controls.Add(_lblRpmAxis);

            _lblLoadAxis = new Label
            {
                Text = "Load Ekseni: Seçilmedi",
                ForeColor = TextPrimary,
                Location = new Point(8, 48),
                Width = 170,
                Font = new Font("Segoe UI", 8.5f)
            };
            gbAxes.Controls.Add(_lblLoadAxis);

            _btnExtractAxes = new Button
            {
                Text = "Eksen Taraması Yap",
                Location = new Point(310, 24),
                Size = new Size(130, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 75),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnExtractAxes.FlatAppearance.BorderSize = 0;
            _btnExtractAxes.Click += BtnExtractAxes_Click;
            gbAxes.Controls.Add(_btnExtractAxes);

            // 3b. Decompiler Grubu
            var pnlDecompileGroup = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Padding = new Padding(8),
                Margin = new Padding(0, 0, 0, 8)
            };
            tlpRightLayout.Controls.Add(pnlDecompileGroup, 0, 1);

            var lblDecTitle = new Label
            {
                Text = "Decompiler & Register Trace Akışı",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(8, 6),
                AutoSize = true
            };
            pnlDecompileGroup.Controls.Add(lblDecTitle);

            var lblRoutSel = new Label
            {
                Text = "Rutin:",
                ForeColor = TextMuted,
                Location = new Point(8, 30),
                AutoSize = true
            };
            pnlDecompileGroup.Controls.Add(lblRoutSel);

            _cbRoutines = new ComboBox
            {
                Location = new Point(50, 26),
                Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgDark,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            _cbRoutines.Items.AddRange(new object[] { "VTEC Yönetimi", "Devir Kesici", "Checksum Kontrolü" });
            _cbRoutines.SelectedIndex = 0;
            pnlDecompileGroup.Controls.Add(_cbRoutines);

            var lblDecAddress = new Label
            {
                Text = "Adres (Hex):",
                ForeColor = TextMuted,
                Location = new Point(8, 62),
                AutoSize = true
            };
            pnlDecompileGroup.Controls.Add(lblDecAddress);

            _txtRoutineAddress = new TextBox
            {
                Location = new Point(90, 58),
                Size = new Size(60, 24),
                Text = "1FC0",
                BackColor = BgDark,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            pnlDecompileGroup.Controls.Add(_txtRoutineAddress);

            _btnDecompile = new Button
            {
                Text = "Decompile",
                Location = new Point(160, 57),
                Size = new Size(95, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 55, 75),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDecompile.FlatAppearance.BorderSize = 0;
            _btnDecompile.Click += BtnDecompile_Click;
            pnlDecompileGroup.Controls.Add(_btnDecompile);

            _rtbDecompiler = new RichTextBox
            {
                Location = new Point(8, 88),
                Width = 430,
                Height = 202,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(12, 15, 22),
                ForeColor = Color.FromArgb(170, 230, 180), // Terminal Yeşili
                Font = new Font("Consolas", 9f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            pnlDecompileGroup.Controls.Add(_rtbDecompiler);

            // 3c. Alttaki aksiyon butonu
            var pnlAdoptAction = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Margin = new Padding(0)
            };
            tlpRightLayout.Controls.Add(pnlAdoptAction, 0, 2);

            _btnAdopt = new Button
            {
                Text = "📥 Seçilen Haritayı ECU Profiline Entegre Et",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnAdopt.FlatAppearance.BorderSize = 0;
            _btnAdopt.Click += BtnAdopt_Click;
            pnlAdoptAction.Controls.Add(_btnAdopt);

            Controls.Add(tlpMain);
        }

        private void BtnScan_Click(object sender, EventArgs e)
        {
            if (_romService == null || !_romService.IsLoaded)
            {
                MessageBox.Show("Öncelikle bir ROM dosyası yüklemelisiniz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[] rom = _romService.GetBuffer();
            _candidates = MapSearchHelper.Search(rom);
            RefreshGrid();

            string headerInfo = RomAnalyzer.AnalyzeHeader(rom);
            _rtbDecompiler.Text = headerInfo + "\n\nHarita taraması tamamlandı! Soldaki listeden incelemek istediğiniz aday haritayı seçin.";
        }

        private void RefreshGrid()
        {
            _dgvCandidates.Rows.Clear();
            string filter = _txtSearch.Text.ToLower();
            foreach (var cand in _candidates)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    if (!cand.MapType.ToLower().Contains(filter) && !cand.Description.ToLower().Contains(filter))
                        continue;
                }

                _dgvCandidates.Rows.Add(
                    $"0x{cand.Offset:X4}",
                    cand.MapType,
                    $"{cand.Rows} x {cand.Cols}",
                    $"%{Math.Round(cand.Confidence * 100, 1)}",
                    cand.Description
                );
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void DgvCandidates_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvCandidates.SelectedRows.Count == 0)
            {
                _selectedCandidate = null;
                _btnExtractAxes.Enabled = false;
                _btnAdopt.Enabled = false;
                return;
            }

            string offsetText = _dgvCandidates.SelectedRows[0].Cells[0].Value?.ToString() ?? string.Empty;
            offsetText = offsetText.Replace("0x", "").Replace("0X", "").Trim();
            if (!int.TryParse(offsetText, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int selectedOffset))
            {
                _selectedCandidate = null;
                _btnExtractAxes.Enabled = false;
                _btnAdopt.Enabled = false;
                return;
            }
            _selectedCandidate = _candidates.FirstOrDefault(c => c.Offset == selectedOffset);

            if (_selectedCandidate != null)
            {
                _lblRpmAxis.Text = "RPM Ekseni: Aranmadı";
                _lblLoadAxis.Text = "Load Ekseni: Aranmadı";
                _selectedAxes = null;
                _btnExtractAxes.Enabled = true;
                _btnAdopt.Enabled = true;
            }
        }

        private void BtnExtractAxes_Click(object sender, EventArgs e)
        {
            if (_romService == null || _selectedCandidate == null) return;

            byte[] rom = _romService.GetBuffer();
            var res = AxisExtractor.ExtractAxes(rom, _selectedCandidate);
            _selectedAxes = res;

            if (res.Success)
            {
                _lblRpmAxis.Text = $"RPM Ekseni: 0x{res.RpmAxisOffset:X4} [Min:{res.RpmAxisValues.Min()} Max:{res.RpmAxisValues.Max()}]";
                _lblLoadAxis.Text = $"Load Ekseni: 0x{res.LoadAxisOffset:X4} [Min:{res.LoadAxisValues.Min()} kPa Max:{res.LoadAxisValues.Max()} kPa]";

                _rtbDecompiler.Text = $"=== EKSEN HARİTALAMA SONUÇLARI ===\n" +
                                     $"Harita Konumu: 0x{_selectedCandidate.Offset:X4}\n" +
                                     $"Eksen Güvenilirlik Skoru: %{Math.Round(res.Confidence * 100, 1)}\n\n" +
                                     $"[RPM Eksen Değerleri ({_selectedCandidate.Cols} Sütun)]:\n" +
                                     string.Join(", ", res.RpmAxisValues) + "\n\n" +
                                     $"[Load Eksen Değerleri ({_selectedCandidate.Rows} Satır)]:\n" +
                                     string.Join(", ", res.LoadAxisValues);
            }
            else
            {
                _lblRpmAxis.Text = "RPM Ekseni: Bulunamadı";
                _lblLoadAxis.Text = "Load Ekseni: Bulunamadı";
                MessageBox.Show("Monoton olarak artış gösteren uygun eksen adresleri tespit edilemedi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDecompile_Click(object sender, EventArgs e)
        {
            if (_romService == null || !_romService.IsLoaded) return;

            int parseAddress;
            try
            {
                parseAddress = Convert.ToInt32(_txtRoutineAddress.Text, 16);
            }
            catch
            {
                MessageBox.Show("Adres geçersiz! Lütfen 16'lık (Hex) formatta girin (örn: 1FC0).", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string routineType = _cbRoutines.SelectedItem.ToString();
            byte[] rom = _romService.GetBuffer();
            string decomp = RomAnalyzer.DecompileRoutine(rom, parseAddress, routineType);
            _rtbDecompiler.Text = decomp;
        }

        private void BtnAdopt_Click(object sender, EventArgs e)
        {
            if (_romService == null || _selectedCandidate == null) return;

            var profile = _romService.Profile;
            if (profile == null) return;

            string mapName = $"MapScan_0x{_selectedCandidate.Offset:X4}";

            // Aynı harita daha önce eklenmiş mi kontrol et
            if (profile.Maps.Any(m => m.Offset == _selectedCandidate.Offset))
            {
                MessageBox.Show("Bu adresteki harita zaten ECU profiline eklenmiş durumda!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mapDef = new Calibration.Maps.MapDefinition
            {
                MapName = mapName,
                Offset = _selectedCandidate.Offset,
                Rows = _selectedCandidate.Rows,
                Columns = _selectedCandidate.Cols,
                EcuCompatibility = profile.Name,
                DataType = "Byte",
                ByteOrder = "LittleEndian",
                ScaleFactor = _selectedCandidate.MapType == "Fuel" ? 1.0 : 0.25,
                OffsetValue = 0.0,
                Unit = _selectedCandidate.MapType == "Fuel" ? "ms" : "deg"
            };

            profile.Maps.Add(mapDef);

            MessageBox.Show(
                $"Harita '{mapName}' başarıyla ECU profiline enjekte edildi!\n" +
                $"Arama Adresi: 0x{_selectedCandidate.Offset:X4}\n\n" +
                $"Artık harita sekmelerinden bu haritayı canlı olarak seçip düzenleyebilirsiniz.",
                "Profil Entegrasyonu Başarılı",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
