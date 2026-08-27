using System;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Calibration.DynoLogs;

namespace HondaTuner.UI
{
    public class DynoLogsControl : UserControl
    {
        private DynoLogsService _service;
        private Timer _watchdogTimer;
        private double _simTime = 0.0;

        // UI Kontrolleri - Dyno
        private TextBox _txtWeight;
        private TextBox _txtLoss;
        private ComboBox _cbCorrection;
        private TrackBar _tbBoost;
        private Label _lblBoostVal;
        private DataGridView _dgvDynoCurve;
        private Label _lblMaxPower;

        // UI Kontrolleri - Performans
        private TextBox _txtTyreSize;
        private TextBox _txtGearRatio;
        private TextBox _txtFinalDrive;
        private Label _lbl0to100;
        private Label _lbl100to200;
        private Label _lblShiftMs;

        // UI Kontrolleri - Versiyon
        private ComboBox _cbBranches;
        private TextBox _txtNewBranch;
        private TextBox _txtCommitMsg;
        private RichTextBox _rtbCommitHistory;

        // UI Kontrolleri - Watchdog
        private DataGridView _dgvWatchdog;

        // Renk Tasarımı
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public DynoLogsControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;

            _service = new DynoLogsService();

            InitializeLayout();
            LoadData();

            _watchdogTimer = new Timer { Interval = 200 };
            _watchdogTimer.Tick += WatchdogTimer_Tick;
            _watchdogTimer.Start();
        }

        private void InitializeLayout()
        {
            var tc = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tc);

            // Tab 1: Virtual Dyno
            var tpDyno = new TabPage("📊 Virtual Dyno & Güç Analizörü");
            tpDyno.BackColor = BgPanel;
            InitializeDynoTab(tpDyno);
            tc.TabPages.Add(tpDyno);

            // Tab 2: Track Logs & Performance
            var tpPerformance = new TabPage("⏱️ Pist Sürüş & Performans");
            tpPerformance.BackColor = BgPanel;
            InitializePerformanceTab(tpPerformance);
            tc.TabPages.Add(tpPerformance);

            // Tab 3: Git Branch & Watchdog
            var tpGitWatch = new TabPage("🌿 Versiyon & RAM Watchdog");
            tpGitWatch.BackColor = BgPanel;
            InitializeGitWatchTab(tpGitWatch);
            tc.TabPages.Add(tpGitWatch);
        }

        private void InitializeDynoTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            page.Controls.Add(tlp);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🏎️ Virtual Dyno Parametreleri", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 32;

            AddConfigRow(pnlLeft, "Araç Ağırlığı (Kg):", _txtWeight = new TextBox { Text = "1100" }, startY);
            AddConfigRow(pnlLeft, "Aktarma Kaybı (%):", _txtLoss = new TextBox { Text = "15.0" }, startY + step);

            var lblCF = new Label { Text = "Düzeltme Standardı:", ForeColor = TextPrimary, Location = new Point(20, startY + 2 * step + 2), Size = new Size(180, 20), Font = new Font("Segoe UI", 8.5f) };
            pnlLeft.Controls.Add(lblCF);

            _cbCorrection = new ComboBox { Location = new Point(210, startY + 2 * step), Size = new Size(80, 20), BackColor = BgDark, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            _cbCorrection.Items.AddRange(new object[] { "SAE", "DIN", "NONE" });
            _cbCorrection.SelectedIndex = 0;
            _cbCorrection.SelectedIndexChanged += DynoConfigChanged;
            pnlLeft.Controls.Add(_cbCorrection);

            _txtWeight.TextChanged += DynoConfigChanged;
            _txtLoss.TextChanged += DynoConfigChanged;

            // Boost
            var lblBoost = new Label { Text = "Simüle Manifold Basıncı (Boost):", ForeColor = TextMuted, Location = new Point(20, startY + 3 * step + 10), Size = new Size(200, 16), Font = new Font("Segoe UI", 8f) };
            pnlLeft.Controls.Add(lblBoost);

            _tbBoost = new TrackBar { Minimum = 100, Maximum = 250, Value = 150, TickStyle = TickStyle.None, Location = new Point(20, startY + 4 * step + 5), Size = new Size(220, 24), Cursor = Cursors.Hand };
            _tbBoost.Scroll += (s, e) => _lblBoostVal.Text = $"{_tbBoost.Value} kPa";
            pnlLeft.Controls.Add(_tbBoost);

            _lblBoostVal = new Label { Text = "150 kPa", ForeColor = TextPrimary, Location = new Point(245, startY + 4 * step + 7), Size = new Size(50, 16), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            pnlLeft.Controls.Add(_lblBoostVal);

            var btnRunDyno = new Button
            {
                Text = "⚡ Sanal Dyno Testini Çalıştır",
                Location = new Point(20, startY + 5 * step + 20),
                Size = new Size(270, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AccentGreen,
                BackColor = BgDark,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRunDyno.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 43, 33);
            btnRunDyno.Click += BtnRunDyno_Click;
            pnlLeft.Controls.Add(btnRunDyno);

            _lblMaxPower = new Label
            {
                Text = "Azami Güç: -- HP @ -- RPM | Azami Tork: -- Nm",
                Location = new Point(20, startY + 6 * step + 30),
                Size = new Size(270, 45),
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(35, 35, 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlLeft.Controls.Add(_lblMaxPower);

            // Sağ Taraf - Dyno Eğrisi
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblCurve = new Label { Text = "📈 Sanal Güç / Tork Çıktı Tablosu", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TextPrimary, Location = new Point(12, 12), Size = new Size(300, 20) };
            pnlRight.Controls.Add(lblCurve);

            _dgvDynoCurve = CreateStyledGrid();
            _dgvDynoCurve.Location = new Point(12, 42);
            _dgvDynoCurve.Size = new Size(350, 240);
            _dgvDynoCurve.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlRight.Controls.Add(_dgvDynoCurve);
        }

        private void InitializePerformanceTab(TabPage page)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = BgPanel };
            page.Controls.Add(pnl);

            var lblTitle = new Label { Text = "⏱️ Pist Performansı & Vites Geçiş Ölçer", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(20, 20), Size = new Size(350, 24) };
            pnl.Controls.Add(lblTitle);

            int startY = 60;
            int step = 35;

            AddConfigRow(pnl, "Lastik Çapı (İnç):", _txtTyreSize = new TextBox { Text = "23.0" }, startY);
            AddConfigRow(pnl, "Şanzıman Vites Oranı:", _txtGearRatio = new TextBox { Text = "1.520" }, startY + step);
            AddConfigRow(pnl, "Ayna Mahruti Oranı:", _txtFinalDrive = new TextBox { Text = "4.260" }, startY + 2 * step);

            _txtTyreSize.TextChanged += PerformanceConfigChanged;
            _txtGearRatio.TextChanged += PerformanceConfigChanged;
            _txtFinalDrive.TextChanged += PerformanceConfigChanged;

            var pnlStats = new Panel { Location = new Point(20, startY + 3 * step + 20), Size = new Size(400, 120), BackColor = BgDark, BorderStyle = BorderStyle.FixedSingle };
            pnl.Controls.Add(pnlStats);

            var lblT1 = new Label { Text = "🚀 0 - 100 km/h Hızlanma:", ForeColor = TextMuted, Location = new Point(15, 20), Size = new Size(180, 20) };
            pnlStats.Controls.Add(lblT1);
            _lbl0to100 = new Label { Text = "-- saniye", ForeColor = AccentGreen, Location = new Point(200, 20), Size = new Size(150, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlStats.Controls.Add(_lbl0to100);

            var lblT2 = new Label { Text = "✈️ 100 - 200 km/h Hızlanma:", ForeColor = TextMuted, Location = new Point(15, 50), Size = new Size(180, 20) };
            pnlStats.Controls.Add(lblT2);
            _lbl100to200 = new Label { Text = "-- saniye", ForeColor = AccentGreen, Location = new Point(200, 50), Size = new Size(150, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlStats.Controls.Add(_lbl100to200);

            var lblT3 = new Label { Text = "🔌 Vites Geçiş Yavaşlaması:", ForeColor = TextMuted, Location = new Point(15, 80), Size = new Size(180, 20) };
            pnlStats.Controls.Add(lblT3);
            _lblShiftMs = new Label { Text = "-- ms (Clutch drop delay)", ForeColor = AccentRed, Location = new Point(200, 80), Size = new Size(180, 20), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            pnlStats.Controls.Add(_lblShiftMs);
        }

        private void InitializeGitWatchTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            page.Controls.Add(tlp);

            // Sol Taraf - Git-Style Branching
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🌿 Kalibrasyon Sürüm Kontrolü (Branching)", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 10), Size = new Size(300, 20) };
            pnlLeft.Controls.Add(lblTitle);

            var lblBranch = new Label { Text = "Aktif Dal (Branch):", ForeColor = TextPrimary, Location = new Point(12, 38), Size = new Size(100, 20) };
            pnlLeft.Controls.Add(lblBranch);

            _cbBranches = new ComboBox { Location = new Point(115, 35), Size = new Size(100, 20), BackColor = BgDark, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            _cbBranches.Items.AddRange(new object[] { "main", "stage1_turbo", "valet_mode" });
            _cbBranches.SelectedIndex = 0;
            _cbBranches.SelectedIndexChanged += BranchSelectedIndexChanged;
            pnlLeft.Controls.Add(_cbBranches);

            var lblNewBranch = new Label { Text = "Yeni Dal Oluştur:", ForeColor = TextPrimary, Location = new Point(12, 68), Size = new Size(100, 20) };
            pnlLeft.Controls.Add(lblNewBranch);

            _txtNewBranch = new TextBox { Location = new Point(115, 65), Size = new Size(100, 20), BackColor = BgDark, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            pnlLeft.Controls.Add(_txtNewBranch);

            var btnNewBranch = new Button { Text = "➕ Dal Aç", Location = new Point(220, 64), Size = new Size(60, 22), FlatStyle = FlatStyle.Flat, ForeColor = AccentBlue, BackColor = BgDark, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnNewBranch.Click += BtnNewBranch_Click;
            pnlLeft.Controls.Add(btnNewBranch);

            var lblCommit = new Label { Text = "Hafıza Commit Açıklaması:", ForeColor = TextPrimary, Location = new Point(12, 98), Size = new Size(100, 36) };
            pnlLeft.Controls.Add(lblCommit);

            _txtCommitMsg = new TextBox { Text = "Optimum fuel timing", Location = new Point(115, 95), Size = new Size(100, 20), BackColor = BgDark, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            pnlLeft.Controls.Add(_txtCommitMsg);

            var btnCommit = new Button { Text = "💾 Commit", Location = new Point(220, 94), Size = new Size(60, 22), FlatStyle = FlatStyle.Flat, ForeColor = AccentGreen, BackColor = BgDark, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCommit.Click += BtnCommit_Click;
            pnlLeft.Controls.Add(btnCommit);

            _rtbCommitHistory = new RichTextBox
            {
                Location = new Point(12, 138),
                Size = new Size(270, 140),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 8f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlLeft.Controls.Add(_rtbCommitHistory);

            // Sağ Taraf - RAM Watchdog
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblWatch = new Label { Text = "🔎 RAM Değer Watchdog (MCU Mercek)", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TextPrimary, Location = new Point(12, 10), Size = new Size(300, 20) };
            pnlRight.Controls.Add(lblWatch);

            _dgvWatchdog = CreateStyledGrid();
            _dgvWatchdog.Location = new Point(12, 38);
            _dgvWatchdog.Size = new Size(270, 240);
            _dgvWatchdog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlRight.Controls.Add(_dgvWatchdog);
        }

        private void BranchSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cbBranches.SelectedItem == null) return;
            string src = _service.ActiveBranch;
            string target = _cbBranches.SelectedItem.ToString();
            if (src != target)
            {
                _service.MergeBranch(src, target);
                RefreshCommitHistoryLog();
            }
        }

        private void BtnNewBranch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtNewBranch.Text)) return;
            string name = _txtNewBranch.Text.Trim();
            _service.CreateBranch(name);
            _cbBranches.Items.Add(name);
            _cbBranches.SelectedItem = name;
            _txtNewBranch.Clear();
            RefreshCommitHistoryLog();
        }

        private void BtnCommit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtCommitMsg.Text)) return;
            _service.CommitChange(_txtCommitMsg.Text.Trim());
            _txtCommitMsg.Clear();
            RefreshCommitHistoryLog();
        }

        private void RefreshCommitHistoryLog()
        {
            _rtbCommitHistory.Clear();
            foreach (var h in _service.GitMergeHistory)
            {
                _rtbCommitHistory.AppendText(h + Environment.NewLine);
            }
        }

        private void BtnRunDyno_Click(object sender, EventArgs e)
        {
            double boost = _tbBoost.Value;

            // Dyno modelini simüle et
            _service.RunVirtualDynoSim(boost);

            // Grid yansıt
            _dgvDynoCurve.Rows.Clear();
            double maxHp = 0;
            double maxHpRpm = 0;
            double maxTorque = 0;

            foreach (var p in _service.CurrentDynoPoints)
            {
                _dgvDynoCurve.Rows.Add(
                    p.Rpm.ToString("F0"),
                    p.Whp.ToString("F1"),
                    p.EngineHp.ToString("F1"),
                    p.TorqueNm.ToString("F1")
                );

                if (p.EngineHp > maxHp)
                {
                    maxHp = p.EngineHp;
                    maxHpRpm = p.Rpm;
                }
                if (p.TorqueNm > maxTorque)
                {
                    maxTorque = p.TorqueNm;
                }
            }

            _lblMaxPower.Text = $"Azami Krank Gücü: {maxHp} HP @ {maxHpRpm} RPM\nAzami Krank Torku: {maxTorque} Nm";

            // Performans yol sürelerini de yansıt
            var perf = _service.EstimatePerformanceTimes(boost);
            _lbl0to100.Text = $"{perf.Time0To100} saniye";
            _lbl100to200.Text = $"{perf.Time100To200} saniye";
            _lblShiftMs.Text = $"{perf.ShiftGapMs} ms (Clutch drop delay)";
        }

        private void DynoConfigChanged(object sender, EventArgs e)
        {
            if (_txtWeight == null || _txtLoss == null) return;
            _service.Tables.VehicleWeightKg = GetDouble(_txtWeight.Text, _service.Tables.VehicleWeightKg);
            _service.Tables.DrivetrainLossPct = GetDouble(_txtLoss.Text, _service.Tables.DrivetrainLossPct);
            _service.Tables.CorrectionFactorType = _cbCorrection.SelectedItem?.ToString() ?? "SAE";
        }

        private void PerformanceConfigChanged(object sender, EventArgs e)
        {
            if (_txtTyreSize == null || _txtGearRatio == null || _txtFinalDrive == null) return;
            _service.Tables.TyreDiameterInches = GetDouble(_txtTyreSize.Text, _service.Tables.TyreDiameterInches);
            _service.Tables.SelectedGearRatio = GetDouble(_txtGearRatio.Text, _service.Tables.SelectedGearRatio);
            _service.Tables.FinalDriveRatio = GetDouble(_txtFinalDrive.Text, _service.Tables.FinalDriveRatio);
        }

        private void LoadData()
        {
            _dgvDynoCurve.Columns.Clear();
            _dgvDynoCurve.Columns.Add("RPM", "Devir (RPM)");
            _dgvDynoCurve.Columns.Add("Whp", "WHP (Teker)");
            _dgvDynoCurve.Columns.Add("EngineHp", "Engine HP");
            _dgvDynoCurve.Columns.Add("Torque", "Tork (Nm)");

            foreach (var col in _dgvDynoCurve.Columns)
            {
                ((DataGridViewColumn)col).ReadOnly = true;
                ((DataGridViewColumn)col).Width = 84;
            }

            // Watchdog Grid Sütunları
            _dgvWatchdog.Columns.Clear();
            _dgvWatchdog.Columns.Add("Var", "Değişken");
            _dgvWatchdog.Columns[0].Width = 120;
            _dgvWatchdog.Columns.Add("Val", "Canlı Değer");
            _dgvWatchdog.Columns[1].Width = 140;

            foreach (var v in _service.Tables.RamWatchlist)
            {
                _dgvWatchdog.Rows.Add(v, "--");
            }

            // İlk simülasyonu başlat
            BtnRunDyno_Click(null, null);

            // Başlangıç commit logu
            _service.CommitChange("Initial base calibration setup stock");
            RefreshCommitHistoryLog();
        }

        private void WatchdogTimer_Tick(object sender, EventArgs e)
        {
            _simTime += 0.2;
            var values = _service.GetWatchdogValues(_simTime);

            int idx = 0;
            foreach (var kvp in values)
            {
                if (idx < _dgvWatchdog.Rows.Count)
                {
                    _dgvWatchdog.Rows[idx].Cells[0].Value = kvp.Key;
                    _dgvWatchdog.Rows[idx].Cells[1].Value = kvp.Value;
                }
                idx++;
            }
        }

        private double GetDouble(string text, double fallback = 0.0)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res))
                return res;
            if (double.TryParse(text, out double resLocal))
                return resLocal;
            return fallback;
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

        private void AddConfigRow(Panel pnl, string labelText, Control ctrl, int y)
        {
            var lbl = new Label { Text = labelText, ForeColor = TextPrimary, Location = new Point(20, y + 2), Size = new Size(180, 20), Font = new Font("Segoe UI", 8.5f) };
            ctrl.Location = new Point(210, y);
            ctrl.Size = new Size(80, 20);
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(ctrl);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watchdogTimer?.Stop();
                _watchdogTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
