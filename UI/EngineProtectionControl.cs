using System;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Calibration.EngineProtection;

namespace HondaTuner.UI
{
    public class EngineProtectionControl : UserControl
    {
        private EngineProtectionService _service;
        private Timer _simTimer;

        // UI Bileşenleri - Limitler
        private TextBox _txtMaxOilTemp;
        private TextBox _txtMinFuelPress;
        private TextBox _txtFanTargetTemp;
        private TextBox _txtMaxEgt;
        private DataGridView _dgvOilPressCurve;

        // UI Bileşenleri - Termal
        private TextBox _txtIatSoakTemp;
        private TextBox _txtIatSoakRetard;
        private TextBox _txtIatBoostReduction;
        private TextBox _txtEgtRetard;
        private TextBox _txtEgtEnrichment;
        private TextBox _txtLimpRpm;

        // Simülatör Girdileri (Slaytlar / Değerler)
        private TrackBar _tbRpm;
        private TrackBar _tbEct;
        private TrackBar _tbIat;
        private TrackBar _tbOilTemp;
        private TrackBar _tbOilPress;
        private TrackBar _tbFuelPress;
        private TrackBar _tbBoost;
        private TrackBar _tbEgt;

        private Label _lblRpmVal;
        private Label _lblEctVal;
        private Label _lblIatVal;
        private Label _lblOilTempVal;
        private Label _lblOilPressVal;
        private Label _lblFuelPressVal;
        private Label _lblBoostVal;
        private Label _lblEgtVal;

        // Simülatör Çıktıları
        private Label _lblSafetyStatus;
        private Label _lblRpmLimit;
        private Label _lblTimingPull;
        private Label _lblEnrichment;
        private Label _lblFanState;
        private Label _lblAlarmMessage;

        // Renk Tasarımı
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public EngineProtectionControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;

            _service = new EngineProtectionService();
            _service.ProtectionAlarmTriggered += Service_ProtectionAlarmTriggered;

            InitializeLayout();
            LoadData();

            _simTimer = new Timer { Interval = 100 };
            _simTimer.Tick += SimTimer_Tick;
            _simTimer.Start();
        }

        private void InitializeLayout()
        {
            var tc = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tc);

            // Tab 1: Limits & Controls
            var tpLimits = new TabPage("🚨 Limit & Emniyet Ayarları");
            tpLimits.BackColor = BgPanel;
            InitializeLimitsTab(tpLimits);
            tc.TabPages.Add(tpLimits);

            // Tab 2: Thermal Correction
            var tpThermal = new TabPage("🌡️ Termal Düzeltmeler & IAT/EGT");
            tpThermal.BackColor = BgPanel;
            InitializeThermalTab(tpThermal);
            tc.TabPages.Add(tpThermal);

            // Tab 3: Simulator
            var tpSim = new TabPage("🎮 Güvenlik Koruma Simülatörü");
            tpSim.BackColor = BgPanel;
            InitializeSimTab(tpSim);
            tc.TabPages.Add(tpSim);
        }

        private void InitializeLimitsTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            page.Controls.Add(tlp);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🚨 Genel Güvenlik Limitleri", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 32;

            AddConfigRow(pnlLeft, "Max Yağ Sıcaklığı (°C):", _txtMaxOilTemp = new TextBox { Text = "125" }, startY);
            AddConfigRow(pnlLeft, "Min Yakıt Basıncı (Bar):", _txtMinFuelPress = new TextBox { Text = "2.8" }, startY + step);
            AddConfigRow(pnlLeft, "Radyatör Fan Sıcaklığı (°C):", _txtFanTargetTemp = new TextBox { Text = "92" }, startY + 2 * step);
            AddConfigRow(pnlLeft, "Maksimum EGT Sınırı (°C):", _txtMaxEgt = new TextBox { Text = "900" }, startY + 3 * step);

            _txtMaxOilTemp.TextChanged += ConfigTextChanged;
            _txtMinFuelPress.TextChanged += ConfigTextChanged;
            _txtFanTargetTemp.TextChanged += ConfigTextChanged;
            _txtMaxEgt.TextChanged += ConfigTextChanged;

            // Sağ Taraf - Yağ Basınç Eğrisi
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblCurve = new Label { Text = "📈 RPM vs Min Yağ Basıncı Sınır Eğrisi", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TextPrimary, Location = new Point(12, 12), Size = new Size(300, 20) };
            pnlRight.Controls.Add(lblCurve);

            _dgvOilPressCurve = CreateStyledGrid();
            _dgvOilPressCurve.Location = new Point(12, 42);
            _dgvOilPressCurve.Size = new Size(340, 220);
            _dgvOilPressCurve.CellValueChanged += DgvOilPressCurve_CellValueChanged;
            pnlRight.Controls.Add(_dgvOilPressCurve);
        }

        private void InitializeThermalTab(TabPage page)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = BgPanel };
            page.Controls.Add(pnl);

            var lblTitle = new Label { Text = "🌡️ Termal Yönetim & IAT Düzeltmeleri", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(20, 20), Size = new Size(300, 24) };
            pnl.Controls.Add(lblTitle);

            int startY = 60;
            int step = 32;

            AddConfigRow(pnl, "IAT Heat Soak Eşiği (°C):", _txtIatSoakTemp = new TextBox { Text = "55" }, startY);
            AddConfigRow(pnl, "IAT Avans Kısma Derecesi (°):", _txtIatSoakRetard = new TextBox { Text = "4.0" }, startY + step);
            AddConfigRow(pnl, "IAT Boost Kısma Derecesi (kPa):", _txtIatBoostReduction = new TextBox { Text = "20" }, startY + 2 * step);
            AddConfigRow(pnl, "EGT Avans Geri Çekme (°):", _txtEgtRetard = new TextBox { Text = "3.0" }, startY + 3 * step);
            AddConfigRow(pnl, "EGT Karışım Zenginleştirme (%):", _txtEgtEnrichment = new TextBox { Text = "15" }, startY + 4 * step);
            AddConfigRow(pnl, "Limp RPM Üst Devir Limiti:", _txtLimpRpm = new TextBox { Text = "3000" }, startY + 5 * step);

            _txtIatSoakTemp.TextChanged += ThermalTextChanged;
            _txtIatSoakRetard.TextChanged += ThermalTextChanged;
            _txtIatBoostReduction.TextChanged += ThermalTextChanged;
            _txtEgtRetard.TextChanged += ThermalTextChanged;
            _txtEgtEnrichment.TextChanged += ThermalTextChanged;
            _txtLimpRpm.TextChanged += ThermalTextChanged;
        }

        private void InitializeSimTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            page.Controls.Add(tlp);

            // Girdiler paneli
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel, AutoScroll = true };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🕹️ Simülasyon Sürüş Parametreleri", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 10), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 40;
            int step = 38;

            AddTrackBarRow(pnlLeft, "Rpm Devir (rpm):", out _tbRpm, out _lblRpmVal, 800, 8500, 3000, startY);
            AddTrackBarRow(pnlLeft, "Su Sıcaklığı ECT (°C):", out _tbEct, out _lblEctVal, 20, 120, 85, startY + step);
            AddTrackBarRow(pnlLeft, "Emme Sıcaklığı IAT (°C):", out _tbIat, out _lblIatVal, 10, 80, 35, startY + 2 * step);
            AddTrackBarRow(pnlLeft, "Yağ Sıcaklığı (°C):", out _tbOilTemp, out _lblOilTempVal, 20, 150, 95, startY + 3 * step);
            AddTrackBarRow(pnlLeft, "Yağ Basıncı (Bar):", out _tbOilPress, out _lblOilPressVal, 0, 80, 45, startY + 4 * step); // Değerler / 10.0 (0-8.0 bar)
            AddTrackBarRow(pnlLeft, "Yakıt Basıncı (Bar):", out _tbFuelPress, out _lblFuelPressVal, 10, 60, 35, startY + 5 * step); // Değer / 10.0
            AddTrackBarRow(pnlLeft, "Turbo Manifold (kPa):", out _tbBoost, out _lblBoostVal, 100, 260, 100, startY + 6 * step);
            AddTrackBarRow(pnlLeft, "Egzoz Sıcaklığı EGT (°C):", out _tbEgt, out _lblEgtVal, 250, 1050, 650, startY + 7 * step);

            // Çıktılar paneli
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblOutTitle = new Label { Text = "🛡️ Koruma Emniyet Durumları", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 10), Size = new Size(300, 24) };
            pnlRight.Controls.Add(lblOutTitle);

            _lblSafetyStatus = new Label
            {
                Text = "✅ SYSTEM SAFE",
                Location = new Point(12, 40),
                Size = new Size(240, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = AccentGreen,
                BackColor = Color.FromArgb(16, 40, 25)
            };
            pnlRight.Controls.Add(_lblSafetyStatus);

            int outY = 90;
            int outStep = 28;

            AddSimOutputRow(pnlRight, "Aktif Limit Devri:", _lblRpmLimit = new Label { Text = "8500 RPM" }, outY);
            AddSimOutputRow(pnlRight, "Toplam Avans Kısma:", _lblTimingPull = new Label { Text = "0.0°" }, outY + outStep);
            AddSimOutputRow(pnlRight, "EGT Yakıt Artışı:", _lblEnrichment = new Label { Text = "%0.0" }, outY + 2 * outStep);
            AddSimOutputRow(pnlRight, "Fan Rölesi Çıkışı:", _lblFanState = new Label { Text = "PASİF" }, outY + 3 * outStep);

            var btnReset = new Button
            {
                Text = "🔄 Alarmları Sıfırla / Koruma Reset",
                Location = new Point(12, outY + 4 * outStep + 10),
                Size = new Size(240, 25),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AccentGreen,
                BackColor = BgDark,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 43, 33);
            btnReset.Click += (s, e) =>
            {
                _service.ResetSafeties();
                _tbOilTemp.Value = 95;
                _tbOilPress.Value = 45; // 4.5 bar
                _tbFuelPress.Value = 35; // 3.5 bar
                _tbEgt.Value = 650;
            };
            pnlRight.Controls.Add(btnReset);

            _lblAlarmMessage = new Label
            {
                Text = "Özel koruma eşiklerinde bir problem algılanmadı.",
                Location = new Point(12, outY + 5 * outStep + 25),
                Size = new Size(240, 100),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = BgDark,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };
            pnlRight.Controls.Add(_lblAlarmMessage);
        }

        private void AddTrackBarRow(Panel pnl, string title, out TrackBar tb, out Label lblVal, int min, int max, int defval, int y)
        {
            var lblTitle = new Label
            {
                Text = title,
                ForeColor = TextMuted,
                Location = new Point(12, y + 2),
                Size = new Size(110, 16),
                Font = new Font("Segoe UI", 8f)
            };
            pnl.Controls.Add(lblTitle);

            tb = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = defval,
                TickStyle = TickStyle.None,
                Location = new Point(125, y - 4),
                Size = new Size(170, 24),
                Cursor = Cursors.Hand
            };
            pnl.Controls.Add(tb);

            lblVal = new Label
            {
                Text = defval.ToString(),
                ForeColor = TextPrimary,
                Location = new Point(300, y + 2),
                Size = new Size(50, 16),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            pnl.Controls.Add(lblVal);
        }

        private void AddSimOutputRow(Panel pnl, string title, Label valLbl, int y)
        {
            var lblTitle = new Label { Text = title, ForeColor = TextMuted, Location = new Point(12, y), Size = new Size(130, 20) };
            pnl.Controls.Add(lblTitle);

            valLbl.Location = new Point(145, y);
            valLbl.Size = new Size(110, 20);
            valLbl.ForeColor = TextPrimary;
            valLbl.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            pnl.Controls.Add(valLbl);
        }

        private void AddConfigRow(Panel pnl, string labelText, TextBox txt, int y)
        {
            var lbl = new Label { Text = labelText, ForeColor = TextPrimary, Location = new Point(20, y + 2), Size = new Size(180, 20), Font = new Font("Segoe UI", 8.5f) };
            pnl.Controls.Add(lbl);

            txt.Location = new Point(210, y);
            txt.Size = new Size(80, 20);
            txt.BackColor = BgDark;
            txt.ForeColor = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.TextAlign = HorizontalAlignment.Center;
            pnl.Controls.Add(txt);
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                BackgroundColor = BgPanel,
                ForeColor = TextPrimary,
                GridColor = Color.FromArgb(40, 48, 64),
                BorderStyle = BorderStyle.FixedSingle,
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
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.DefaultCellStyle.BackColor = BgPanel;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = AccentBlue;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;
            return dgv;
        }

        private void LoadData()
        {
            // RPM vs Min Yoğunluk Gridi
            _dgvOilPressCurve.Columns.Clear();
            _dgvOilPressCurve.Columns.Add("RPM", "Devir (RPM)");
            _dgvOilPressCurve.Columns[0].Width = 140;
            _dgvOilPressCurve.Columns[0].ReadOnly = true;
            _dgvOilPressCurve.Columns.Add("Press", "Min Basınç (Bar)");
            _dgvOilPressCurve.Columns[1].Width = 160;

            for (int i = 0; i < _service.Tables.OilPressRpmBins.Length; i++)
            {
                _dgvOilPressCurve.Rows.Add(
                    _service.Tables.OilPressRpmBins[i].ToString(),
                    _service.Tables.MinOilPressureCurve[i].ToString("F1")
                );
            }
        }

        private void ConfigTextChanged(object sender, EventArgs e)
        {
            _service.Tables.MaxOilTemp = GetDouble(_txtMaxOilTemp.Text);
            _service.Tables.MinFuelPressure = GetDouble(_txtMinFuelPress.Text);
            _service.Tables.FanTargetTemp = GetDouble(_txtFanTargetTemp.Text);
            _service.Tables.MaxEgtLimit = GetDouble(_txtMaxEgt.Text);
        }

        private void ThermalTextChanged(object sender, EventArgs e)
        {
            _service.Tables.IatHeatSoakRetardThreshold = GetDouble(_txtIatSoakTemp.Text);
            _service.Tables.IatHeatSoakRetard = GetDouble(_txtIatSoakRetard.Text);
            _service.Tables.IatBoostLimitReduction = GetDouble(_txtIatBoostReduction.Text);
            _service.Tables.EgtTimingPull = GetDouble(_txtEgtRetard.Text);
            _service.Tables.EgtFuelEnrichment = GetDouble(_txtEgtEnrichment.Text);
            _service.Tables.ThermalLimpModeRpm = GetDouble(_txtLimpRpm.Text);
        }

        private void DgvOilPressCurve_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 1) return;
            try
            {
                double val = Convert.ToDouble(_dgvOilPressCurve.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                _service.Tables.MinOilPressureCurve[e.RowIndex] = val;
            }
            catch { }
        }

        private void Service_ProtectionAlarmTriggered(object sender, string msg)
        {
            _lblAlarmMessage.Text = msg;
        }

        private void SimTimer_Tick(object sender, EventArgs e)
        {
            double rpm = _tbRpm.Value;
            double ect = _tbEct.Value;
            double iat = _tbIat.Value;
            double oilTemp = _tbOilTemp.Value;
            double oilPress = _tbOilPress.Value / 10.0;
            double fuelPress = _tbFuelPress.Value / 10.0;
            double boost = _tbBoost.Value;
            double egt = _tbEgt.Value;

            // Slayt etiketlerini güncelle
            _lblRpmVal.Text = $"{rpm} rpm";
            _lblEctVal.Text = $"{ect}°C";
            _lblIatVal.Text = $"{iat}°C";
            _lblOilTempVal.Text = $"{oilTemp}°C";
            _lblOilPressVal.Text = $"{oilPress.ToString("F1")} Bar";
            _lblFuelPressVal.Text = $"{fuelPress.ToString("F1")} Bar";
            _lblBoostVal.Text = $"{boost} kPa";
            _lblEgtVal.Text = $"{egt}°C";

            // Emniyet motorunu koştur
            _service.EvaluateSafety(rpm, ect, iat, oilTemp, oilPress, fuelPress, boost, egt, 0.1);

            // Çıktıları yansıt
            _lblRpmLimit.Text = $"{_service.ActiveRpmLimit} RPM";
            _lblTimingPull.Text = $"-{_service.ActiveTimingPull.ToString("F1")}°";
            _lblEnrichment.Text = $"%{_service.ActiveFuelEnrichmentPct.ToString("F0")}";
            _lblFanState.Text = _service.FanRelayState ? "🔥 ETKİN (Röle ON)" : "PASİF (Röle OFF)";
            _lblFanState.ForeColor = _service.FanRelayState ? AccentGreen : TextMuted;

            // Renkli Durum Bildirimi
            if (_service.IsFuelCutActive)
            {
                _lblSafetyStatus.Text = "🚨 FUEL CUT / EMERGENCY ALERT!";
                _lblSafetyStatus.ForeColor = Color.White;
                _lblSafetyStatus.BackColor = AccentRed;
            }
            else if (_service.IsThermalLimpModeActive)
            {
                _lblSafetyStatus.Text = "⚠️ ENGINE LIMP MODE ACTIVE";
                _lblSafetyStatus.ForeColor = Color.Black;
                _lblSafetyStatus.BackColor = Color.Orange;
            }
            else if (_service.IsPowerReductionActive)
            {
                _lblSafetyStatus.Text = "⚠️ POWER REDUCTION ACTIVE";
                _lblSafetyStatus.ForeColor = Color.Black;
                _lblSafetyStatus.BackColor = Color.Yellow;
            }
            else
            {
                _lblSafetyStatus.Text = "✅ SYSTEM SAFE";
                _lblSafetyStatus.ForeColor = AccentGreen;
                _lblSafetyStatus.BackColor = Color.FromArgb(16, 40, 25);
            }
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
