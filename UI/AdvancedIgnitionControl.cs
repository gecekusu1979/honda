using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Calibration.Ignition;

namespace HondaTuner.UI
{
    public class AdvancedIgnitionControl : UserControl, ILocalizable
    {
        public void ApplyLocalization()
        {
            MainForm.ApplyRecursiveLocalization(this);
            RunSensorSimulation();
            RunCanSimulation();
            RunMbtSimulation();
        }
        private AdvancedIgnitionTables _tables;
        private MbtOptimizer _mbtOptimizer;
        private SensorCalibration _activeSensor;
        private CanSensorDecoder _canDecoder;

        // UI Bileşenleri
        private DataGridView _dgvCranking;
        private Label[] _lblCylOffsets;
        private TrackBar[] _tbCylOffsets;

        // Sensör UI
        private ComboBox _comboSensorPreset;
        private DataGridView _dgvSensorCurve;
        private TextBox _txtSimVoltage;
        private Label _lblSensorOutput;

        // CAN UI
        private TextBox _txtCanHex;
        private Label _lblCanOutput;
        private TextBox _txtCanFrameId;
        private TextBox _txtCanStartBit;
        private TextBox _txtCanBitLen;
        private CheckBox _chkCanBigEndian;
        private TextBox _txtCanScale;
        private TextBox _txtCanOffset;

        // MBT UI
        private TextBox _txtMbtRpm;
        private TextBox _txtMbtLoad;
        private TextBox _txtMbtOctane;
        private TextBox _txtMbtCurrentAdvance;
        private Label _lblMbtEst;
        private Label _lblMbtDiff;
        private Label _lblMbtAdvice;
        private Button _btnMbtApply;

        // Renk Paleti
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public AdvancedIgnitionControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;

            _tables = new AdvancedIgnitionTables();
            _mbtOptimizer = new MbtOptimizer();
            _activeSensor = SensorCalibration.CreateOemMapCalibration();
            _canDecoder = new CanSensorDecoder("EGT Sensor", 0x200, 16, 16, false, 0.25, 0.0, "°C");

            InitializeLayout();
            LoadData();
        }

        private void InitializeLayout()
        {
            var tcTables = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tcTables);

            // 1. Cranking & Cylinder Offsets Tab
            var tpOffsets = new TabPage("⚡ Çalıştırma & Silindir Düzeltmeleri");
            tpOffsets.BackColor = BgPanel;
            InitializeOffsetsTab(tpOffsets);
            tcTables.TabPages.Add(tpOffsets);

            // 2. Sensor Linearization Tab
            var tpSensor = new TabPage("🔌 Sensör Kalibrasyon Eğrisi");
            tpSensor.BackColor = BgPanel;
            InitializeSensorTab(tpSensor);
            tcTables.TabPages.Add(tpSensor);

            // 3. CAN Bus Tab
            var tpCan = new TabPage("📡 CAN Bus Kod Çözücü");
            tpCan.BackColor = BgPanel;
            InitializeCanTab(tpCan);
            tcTables.TabPages.Add(tpCan);

            // 4. MBT Optimizer Tab
            var tpMbt = new TabPage("🧠 MBT Avans Önerici");
            tpMbt.BackColor = BgPanel;
            InitializeMbtTab(tpMbt);
            tcTables.TabPages.Add(tpMbt);
        }

        private void InitializeOffsetsTab(TabPage page)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = BgDark
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            page.Controls.Add(tlp);

            // Sol Panel - Cranking Timing Grid
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblCrankingTitle = new Label
            {
                Text = "🔑 Çalıştırma Anı Avans Haritası",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(12, 12),
                Size = new Size(300, 24)
            };
            pnlLeft.Controls.Add(lblCrankingTitle);

            _dgvCranking = CreateStyledGrid();
            _dgvCranking.Location = new Point(12, 45);
            _dgvCranking.Size = new Size(320, 240);
            _dgvCranking.CellValueChanged += DgvCranking_CellValueChanged;
            pnlLeft.Controls.Add(_dgvCranking);

            // Sağ Panel - Cylinder Offsets Sliders
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblCylTitle = new Label
            {
                Text = "🔥 Bireysel Silindir Avans Düzeltmeleri",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(12, 12),
                Size = new Size(300, 24)
            };
            pnlRight.Controls.Add(lblCylTitle);

            _tbCylOffsets = new TrackBar[4];
            _lblCylOffsets = new Label[4];

            int startY = 55;
            for (int i = 0; i < 4; i++)
            {
                int filterIndex = i;
                var lblCyl = new Label
                {
                    Text = $"Silindir {filterIndex + 1}:",
                    ForeColor = TextPrimary,
                    Location = new Point(12, startY + (filterIndex * 50)),
                    Size = new Size(80, 20),
                    Font = new Font("Segoe UI", 9f)
                };
                pnlRight.Controls.Add(lblCyl);

                _tbCylOffsets[filterIndex] = new TrackBar
                {
                    Minimum = -50, // -5.0 degrees
                    Maximum = 50,  // +5.0 degrees
                    Value = 0,
                    Location = new Point(100, startY + (filterIndex * 50) - 5),
                    Size = new Size(180, 35),
                    TickFrequency = 10
                };
                _tbCylOffsets[filterIndex].Scroll += (s, e) =>
                {
                    double val = _tbCylOffsets[filterIndex].Value / 10.0;
                    _lblCylOffsets[filterIndex].Text = $"{val:0.0}°";
                    _tables.CylinderOffsets[filterIndex] = val;
                };
                pnlRight.Controls.Add(_tbCylOffsets[filterIndex]);

                _lblCylOffsets[filterIndex] = new Label
                {
                    Text = "0.0°",
                    ForeColor = AccentGreen,
                    Location = new Point(290, startY + (filterIndex * 50)),
                    Size = new Size(60, 20),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                };
                pnlRight.Controls.Add(_lblCylOffsets[filterIndex]);
            }
        }

        private void InitializeSensorTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            page.Controls.Add(tlp);

            // Sol Panel - Grids
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblPreset = new Label { Text = "Sensör Tipi Kalibrasyon Eğrisi Seçin:", ForeColor = TextPrimary, Location = new Point(12, 14), Size = new Size(220, 20) };
            pnlLeft.Controls.Add(lblPreset);

            _comboSensorPreset = new ComboBox
            {
                Location = new Point(240, 12),
                Size = new Size(160, 23),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _comboSensorPreset.Items.AddRange(new string[] { "Honda OEM MAP", "AEM Oil Pressure", "GM Oil Temp" });
            _comboSensorPreset.SelectedIndex = 0;
            _comboSensorPreset.SelectedIndexChanged += ComboSensorPreset_SelectedIndexChanged;
            pnlLeft.Controls.Add(_comboSensorPreset);

            _dgvSensorCurve = CreateStyledGrid();
            _dgvSensorCurve.Location = new Point(12, 45);
            _dgvSensorCurve.Size = new Size(390, 240);
            _dgvSensorCurve.CellValueChanged += DgvSensorCurve_CellValueChanged;
            pnlLeft.Controls.Add(_dgvSensorCurve);

            // Sağ Panel - Simülasyon
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblSimTitle = new Label
            {
                Text = "🔌 Sinyal Linearizasyon Simülasyonu",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Location = new Point(12, 12),
                Size = new Size(300, 24)
            };
            pnlRight.Controls.Add(lblSimTitle);

            var lblVoltInput = new Label { Text = "Analog Voltaj Girişi (0.0V - 5.0V):", ForeColor = TextPrimary, Location = new Point(12, 60), Size = new Size(200, 20) };
            pnlRight.Controls.Add(lblVoltInput);

            _txtSimVoltage = new TextBox
            {
                Text = "2.5",
                Location = new Point(220, 58),
                Size = new Size(60, 22),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            _txtSimVoltage.TextChanged += TxtSimVoltage_TextChanged;
            pnlRight.Controls.Add(_txtSimVoltage);

            var lblResultHeader = new Label { Text = "Okunan Fiziksel Değer:", ForeColor = TextMuted, Location = new Point(12, 110), Size = new Size(200, 20) };
            pnlRight.Controls.Add(lblResultHeader);

            _lblSensorOutput = new Label
            {
                Text = "97.5 kPa",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = AccentGreen,
                Location = new Point(12, 135),
                Size = new Size(260, 40)
            };
            pnlRight.Controls.Add(_lblSensorOutput);
        }

        private void InitializeCanTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            page.Controls.Add(tlp);

            // Sol Panel - Config
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "📡 CAN Bus Çerçeve Çözümleme Tanımları", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 28;

            AddConfigRow(pnlLeft, "Frame ID (HEX):", _txtCanFrameId = new TextBox { Text = "200" }, startY);
            AddConfigRow(pnlLeft, "Başlangıç Biti (Start Bit):", _txtCanStartBit = new TextBox { Text = "16" }, startY + step);
            AddConfigRow(pnlLeft, "Bit Uzunluğu (Bit Len):", _txtCanBitLen = new TextBox { Text = "16" }, startY + 2 * step);
            AddConfigRow(pnlLeft, "Çarpan Katsayı (Scale):", _txtCanScale = new TextBox { Text = "0.25" }, startY + 3 * step);
            AddConfigRow(pnlLeft, "Kayma Katsayı (Offset):", _txtCanOffset = new TextBox { Text = "0.0" }, startY + 4 * step);

            _chkCanBigEndian = new CheckBox
            {
                Text = "Is Motorla Format (Big Endian)",
                Location = new Point(12, startY + 5 * step + 5),
                Size = new Size(280, 20),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f)
            };
            _chkCanBigEndian.CheckedChanged += CanParams_Changed;
            pnlLeft.Controls.Add(_chkCanBigEndian);

            _txtCanFrameId.TextChanged += CanParams_Changed;
            _txtCanStartBit.TextChanged += CanParams_Changed;
            _txtCanBitLen.TextChanged += CanParams_Changed;
            _txtCanScale.TextChanged += CanParams_Changed;
            _txtCanOffset.TextChanged += CanParams_Changed;

            // Sağ Panel - Live hex decoder simulation
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblSimTitle = new Label { Text = "📡 CAN Mesaj Paketi Canlı Simülasyonu", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlRight.Controls.Add(lblSimTitle);

            var lblHexInput = new Label { Text = "Simüle Edilen 8-Byte Çerçeve Mesaj (Hex):", ForeColor = TextPrimary, Location = new Point(12, 60), Size = new Size(320, 20) };
            pnlRight.Controls.Add(lblHexInput);

            _txtCanHex = new TextBox
            {
                Text = "00 00 E8 03 00 00 00 00", // E8 03 = 1000 in little-endian. 1000 * 0.25 = 250 °C
                Location = new Point(12, 85),
                Size = new Size(300, 24),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                Font = new Font("Courier New", 10f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtCanHex.TextChanged += TxtCanHex_TextChanged;
            pnlRight.Controls.Add(_txtCanHex);

            var lblOutTitle = new Label { Text = "Çözümlenen Sensör Çıktısı (EGT):", ForeColor = TextMuted, Location = new Point(12, 135), Size = new Size(300, 20) };
            pnlRight.Controls.Add(lblOutTitle);

            _lblCanOutput = new Label
            {
                Text = "250.0 °C",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = AccentGreen,
                Location = new Point(12, 160),
                Size = new Size(300, 45)
            };
            pnlRight.Controls.Add(_lblCanOutput);
        }

        private void AddConfigRow(Panel pnl, string labelText, TextBox txt, int y)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = TextPrimary,
                Location = new Point(12, y + 2),
                Size = new Size(160, 20),
                Font = new Font("Segoe UI", 9f)
            };
            pnl.Controls.Add(lbl);

            txt.Location = new Point(180, y);
            txt.Size = new Size(80, 22);
            txt.BackColor = BgDark;
            txt.ForeColor = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.TextAlign = HorizontalAlignment.Center;
            pnl.Controls.Add(txt);
        }

        private void InitializeMbtTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            page.Controls.Add(tlp);

            // Sol Panel - Inputs
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🧠 MBT Ateşleme Simülasyon Girdileri", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 32;

            AddConfigRow(pnlLeft, "Motor Devri (RPM):", _txtMbtRpm = new TextBox { Text = "3000" }, startY);
            AddConfigRow(pnlLeft, "Emme Manifold Yükü (kPa):", _txtMbtLoad = new TextBox { Text = "100.0" }, startY + step);
            AddConfigRow(pnlLeft, "Yakıt Oktan Oranı (RON):", _txtMbtOctane = new TextBox { Text = "95.0" }, startY + 2 * step);
            AddConfigRow(pnlLeft, "Mevcut Avans Değeri (°):", _txtMbtCurrentAdvance = new TextBox { Text = "18.0" }, startY + 3 * step);

            _txtMbtRpm.TextChanged += MbtInputs_Changed;
            _txtMbtLoad.TextChanged += MbtInputs_Changed;
            _txtMbtOctane.TextChanged += MbtInputs_Changed;
            _txtMbtCurrentAdvance.TextChanged += MbtInputs_Changed;

            // Sağ Panel - Recommendation wizard
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblRecTitle = new Label { Text = "🧠 Ateşleme Optimizasyon Kararı", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlRight.Controls.Add(lblRecTitle);

            int outY = 48;

            var lblEstMbt = new Label { Text = "Modellenen Teorik MBT Avansı:", ForeColor = TextMuted, Location = new Point(12, outY), Size = new Size(200, 20) };
            pnlRight.Controls.Add(lblEstMbt);

            _lblMbtEst = new Label { Text = "22.0° BTDC", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = AccentGreen, Location = new Point(12, outY + 20), Size = new Size(200, 26) };
            pnlRight.Controls.Add(_lblMbtEst);

            var lblDiff = new Label { Text = "Sapma (Current - MBT):", ForeColor = TextMuted, Location = new Point(12, outY + 55), Size = new Size(200, 20) };
            pnlRight.Controls.Add(lblDiff);

            _lblMbtDiff = new Label { Text = "-4.0°", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.Yellow, Location = new Point(12, outY + 75), Size = new Size(200, 22), Tag = "dynamic" };
            pnlRight.Controls.Add(_lblMbtDiff);

            _lblMbtAdvice = new Label
            {
                Text = "ℹ️ OPTİMİZASYON: Avans MBT'nin Çok Gerisinde. Güç Kazanmak İçin Avansı Artırın.",
                ForeColor = TextPrimary,
                Location = new Point(12, outY + 110),
                Size = new Size(340, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            pnlRight.Controls.Add(_lblMbtAdvice);

            _btnMbtApply = new Button
            {
                Text = "🔧 Avans Düzeltmesini Haritada Otomatik Ayarla",
                Location = new Point(12, outY + 170),
                Size = new Size(330, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnMbtApply.FlatAppearance.BorderSize = 0;
            _btnMbtApply.Click += BtnMbtApply_Click;
            pnlRight.Controls.Add(_btnMbtApply);
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                BackgroundColor = BgPanel,
                ForeColor = TextPrimary,
                GridColor = Color.FromArgb(40, 48, 64),
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 37, 52);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.DefaultCellStyle.BackColor = BgPanel;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AccentBlue;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private void LoadData()
        {
            // 1. Cranking Grid
            _dgvCranking.Columns.Clear();
            _dgvCranking.Columns.Add("ECT", "ECT Hararet (°C)");
            _dgvCranking.Columns.Add("Timing", "Ateşleme Avansı (°)");
            _dgvCranking.Columns[0].Width = 140;
            _dgvCranking.Columns[1].Width = 140;

            for (int i = 0; i < _tables.CrankingTimingEctBins.Length; i++)
            {
                _dgvCranking.Rows.Add(
                    _tables.CrankingTimingEctBins[i].ToString("F0"),
                    _tables.CrankingTimingAdvances[i].ToString("F1")
                );
            }

            // 2. Sensor Curve Grid
            LoadSensorPresetGrid();
        }

        private void LoadSensorPresetGrid()
        {
            _dgvSensorCurve.CellValueChanged -= DgvSensorCurve_CellValueChanged;
            _dgvSensorCurve.Columns.Clear();
            _dgvSensorCurve.Columns.Add("Volt", HondaTuner.Core.Localization.L.Get("Sensör Sinyali (Volt)"));
            _dgvSensorCurve.Columns.Add("Phys", $"{HondaTuner.Core.Localization.L.Get("Okunan Fiziksel Değer:")} ({_activeSensor.Unit})");
            _dgvSensorCurve.Columns[0].Width = 180;
            _dgvSensorCurve.Columns[1].Width = 180;

            for (int i = 0; i < _activeSensor.Voltages.Length; i++)
            {
                _dgvSensorCurve.Rows.Add(
                    _activeSensor.Voltages[i].ToString("F2"),
                    _activeSensor.PhysicalValues[i].ToString("F1")
                );
            }
            _dgvSensorCurve.CellValueChanged += DgvSensorCurve_CellValueChanged;

            RunSensorSimulation();
        }

        private void DgvCranking_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvCranking.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                if (e.ColumnIndex == 0)
                    _tables.CrankingTimingEctBins[e.RowIndex] = val;
                else
                    _tables.CrankingTimingAdvances[e.RowIndex] = val;
            }
            catch (Exception ex) { Debug.WriteLine($"[AdvancedIgnitionControl] Cranking hücresi parse hatası: {ex.Message}"); }
        }

        private void DgvSensorCurve_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvSensorCurve.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                if (e.ColumnIndex == 0)
                    _activeSensor.Voltages[e.RowIndex] = val;
                else
                    _activeSensor.PhysicalValues[e.RowIndex] = val;
                RunSensorSimulation();
            }
            catch (Exception ex) { Debug.WriteLine($"[AdvancedIgnitionControl] SensorCurve hücresi parse hatası: {ex.Message}"); }
        }

        private void ComboSensorPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_comboSensorPreset.SelectedIndex == 0)
                _activeSensor = SensorCalibration.CreateOemMapCalibration();
            else if (_comboSensorPreset.SelectedIndex == 1)
                _activeSensor = SensorCalibration.CreateOilPressureCalibration();
            else
                _activeSensor = SensorCalibration.CreateOilTempCalibration();

            LoadSensorPresetGrid();
        }

        private void TxtSimVoltage_TextChanged(object sender, EventArgs e)
        {
            RunSensorSimulation();
        }

        private void RunSensorSimulation()
        {
            if (_activeSensor == null || _lblSensorOutput == null) return;
            double v = GetDouble(_txtSimVoltage.Text);
            double physVal = _activeSensor.Linearize(v);
            _lblSensorOutput.Text = $"{physVal.ToString("F1")} {_activeSensor.Unit}";
        }

        private void CanParams_Changed(object sender, EventArgs e)
        {
            UpdateCanDecoder();
        }

        private void UpdateCanDecoder()
        {
            try
            {
                _canDecoder.FrameId = Convert.ToUInt32(_txtCanFrameId.Text, 16);
                _canDecoder.StartBit = Convert.ToInt32(_txtCanStartBit.Text);
                _canDecoder.BitLength = Convert.ToInt32(_txtCanBitLen.Text);
                _canDecoder.IsBigEndian = _chkCanBigEndian.Checked;
                _canDecoder.Scale = GetDouble(_txtCanScale.Text);
                _canDecoder.Offset = GetDouble(_txtCanOffset.Text);

                RunCanSimulation();
            }
            catch (Exception ex) { Debug.WriteLine($"[AdvancedIgnitionControl] CAN decoder parametre hatası: {ex.Message}"); }
        }

        private void TxtCanHex_TextChanged(object sender, EventArgs e)
        {
            RunCanSimulation();
        }

        private void RunCanSimulation()
        {
            if (_lblCanOutput == null) return;
            try
            {
                string hex = _txtCanHex.Text.Replace(" ", "").Trim();
                if (hex.Length < 16) return;

                byte[] bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }

                double val = _canDecoder.Decode(bytes);
                _lblCanOutput.Text = $"{val.ToString("F1")} {_canDecoder.Unit}";
            }
            catch
            {
                _lblCanOutput.Text = HondaTuner.Core.Localization.L.Get("diag_can_error");
            }
        }

        private void MbtInputs_Changed(object sender, EventArgs e)
        {
            RunMbtSimulation();
        }

        private void RunMbtSimulation()
        {
            if (_lblMbtEst == null) return;

            double rpm = GetDouble(_txtMbtRpm.Text);
            double load = GetDouble(_txtMbtLoad.Text);
            double octane = GetDouble(_txtMbtOctane.Text);
            double currentAdv = GetDouble(_txtMbtCurrentAdvance.Text);

            double mbt = _mbtOptimizer.EstimateMbt(rpm, load, octane);
            double diff = _mbtOptimizer.CalculateDeviation(currentAdv, mbt);

            _lblMbtEst.Text = $"{mbt.ToString("F1")}° BTDC";

            if (diff > 0)
            {
                _lblMbtDiff.Text = string.Format(HondaTuner.Core.Localization.L.Get("mbt_above"), diff);
                _lblMbtDiff.ForeColor = AccentRed;
            }
            else
            {
                _lblMbtDiff.Text = string.Format(HondaTuner.Core.Localization.L.Get("mbt_retarded"), diff);
                _lblMbtDiff.ForeColor = Color.Yellow;
            }

            _lblMbtAdvice.Text = _mbtOptimizer.GetKnockProximityStatus(currentAdv, mbt, octane);
        }

        private void BtnMbtApply_Click(object sender, EventArgs e)
        {
            double rpm = GetDouble(_txtMbtRpm.Text);
            double load = GetDouble(_txtMbtLoad.Text);
            double octane = GetDouble(_txtMbtOctane.Text);
            double mbt = _mbtOptimizer.EstimateMbt(rpm, load, octane);

            _txtMbtCurrentAdvance.Text = mbt.ToString("F1");
            MessageBox.Show(
                string.Format(HondaTuner.Core.Localization.L.Get("mbt_apply_msg_fmt"), mbt),
                HondaTuner.Core.Localization.L.Get("mbt_apply_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private double GetDouble(string text)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res))
                return res;
            if (double.TryParse(text, out double resLocal))
                return resLocal;
            return 0.0;
        }
    }
}
