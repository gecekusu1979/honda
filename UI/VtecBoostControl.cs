using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Calibration.VtecBoost;

namespace HondaTuner.UI
{
    public class VtecBoostControl : UserControl, ILocalizable
    {
        public void ApplyLocalization()
        {
            MainForm.ApplyRecursiveLocalization(this);
        }
        private BoostControlService _service;
        private Timer _simTimer;
        private double _simTargetBoost = 100.0;
        private double _simActualBoost = 100.0;

        // UI Bileşenleri
        private TextBox _txtVtecMinRpm;
        private TextBox _txtVtecMinSpeed;
        private CheckBox[] _chkGearRestrictions;

        private DataGridView _dgvBoostTargets;
        private DataGridView _dgvWgDuties;

        // Simulator
        private TextBox _txtSimRpm;
        private TextBox _txtSimSpeed;
        private TextBox _txtSimGear;
        private CheckBox _chkSimScramble;
        private Label _lblTargetBoost;
        private Label _lblActualBoost;
        private Label _lblWgDuty;
        private Label _lblVtecState;
        private Label _lblAlarmStatus;

        // Renk Paleti
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public VtecBoostControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;

            _service = new BoostControlService();
            _service.WgFailureAlarm += Service_WgFailureAlarm;

            InitializeLayout();
            LoadData();

            _simTimer = new Timer();
            _simTimer.Interval = 100;
            _simTimer.Tick += SimTimer_Tick;
            _simTimer.Start();
        }

        private void InitializeLayout()
        {
            var tcTables = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tcTables);

            // Tab 1: VTEC Settings
            var tpVtec = new TabPage("🏁 VTEC Solenoid Limitleri");
            tpVtec.BackColor = BgPanel;
            InitializeVtecTab(tpVtec);
            tcTables.TabPages.Add(tpVtec);

            // Tab 2: Boost Target Map
            var tpBoost = new TabPage("📈 Target Boost (RPM vs Gear)");
            tpBoost.BackColor = BgPanel;
            _dgvBoostTargets = CreateStyledGrid();
            _dgvBoostTargets.Dock = DockStyle.Fill;
            _dgvBoostTargets.CellValueChanged += DgvBoostTargets_CellValueChanged;
            tpBoost.Controls.Add(_dgvBoostTargets);
            tcTables.TabPages.Add(tpBoost);

            // Tab 3: Wg Solenoid Base Duty Map
            var tpWg = new TabPage("🔌 Base WG Solenoid Duty");
            tpWg.BackColor = BgPanel;
            _dgvWgDuties = CreateStyledGrid();
            _dgvWgDuties.Dock = DockStyle.Fill;
            _dgvWgDuties.CellValueChanged += DgvWgDuties_CellValueChanged;
            tpWg.Controls.Add(_dgvWgDuties);
            tcTables.TabPages.Add(tpWg);

            // Tab 4: Simulator
            var tpSim = new TabPage("🕹️ Dynamic Solenoid Simülatör");
            tpSim.BackColor = BgPanel;
            InitializeSimTab(tpSim);
            tcTables.TabPages.Add(tpSim);
        }

        private void InitializeVtecTab(TabPage page)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            page.Controls.Add(pnl);

            var lblTitle = new Label { Text = "🏁 VTEC Geçiş Koşulları", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(20, 20), Size = new Size(300, 24) };
            pnl.Controls.Add(lblTitle);

            AddConfigRow(pnl, "VTEC Minimum Devir (RPM):", _txtVtecMinRpm = new TextBox { Text = "4800" }, 60);
            AddConfigRow(pnl, "VTEC Minimum Hız (km/h):", _txtVtecMinSpeed = new TextBox { Text = "20" }, 92);

            _txtVtecMinRpm.TextChanged += VtecLimits_Changed;
            _txtVtecMinSpeed.TextChanged += VtecLimits_Changed;

            var lblGears = new Label { Text = "VTEC Engellenen Vites Seçenekleri (Gear Lockout out):", ForeColor = TextPrimary, Location = new Point(20, 140), Size = new Size(340, 20) };
            pnl.Controls.Add(lblGears);

            _chkGearRestrictions = new CheckBox[6];
            for (int i = 0; i < 6; i++)
            {
                int filterIndex = i;
                _chkGearRestrictions[filterIndex] = new CheckBox
                {
                    Text = $"{filterIndex + 1}. Vites",
                    Location = new Point(20 + (filterIndex * 85), 165),
                    Size = new Size(80, 22),
                    ForeColor = TextPrimary,
                    Font = new Font("Segoe UI", 9f)
                };
                _chkGearRestrictions[filterIndex].CheckedChanged += (s, e) =>
                {
                    _service.Tables.VtecGearRestrictions[filterIndex] = _chkGearRestrictions[filterIndex].Checked;
                };
                pnl.Controls.Add(_chkGearRestrictions[filterIndex]);
            }
            _chkGearRestrictions[0].Checked = true; // 1. Viteste varsayılan engelli
        }

        private void AddConfigRow(Panel pnl, string labelText, TextBox txt, int y)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = TextPrimary,
                Location = new Point(20, y + 2),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9f)
            };
            pnl.Controls.Add(lbl);

            txt.Location = new Point(230, y);
            txt.Size = new Size(80, 22);
            txt.BackColor = BgDark;
            txt.ForeColor = TextPrimary;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.TextAlign = HorizontalAlignment.Center;
            pnl.Controls.Add(txt);
        }

        private void InitializeSimTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            page.Controls.Add(tlp);

            // Sol Panel - Sim Inputs
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "🕹️ Sürüş Simülatör Girdileri", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 32;

            AddConfigRow(pnlLeft, "Motor Devri (RPM):", _txtSimRpm = new TextBox { Text = "3000" }, startY);
            AddConfigRow(pnlLeft, "Araç Hızı (km/h):", _txtSimSpeed = new TextBox { Text = "60" }, startY + step);
            AddConfigRow(pnlLeft, "Aktif Vites (Gear):", _txtSimGear = new TextBox { Text = "3" }, startY + 2 * step);

            _chkSimScramble = new CheckBox
            {
                Text = "⚡ Scramble Boost Düğmesi (Geçici Avans / Boost)",
                Location = new Point(20, startY + 3 * step + 5),
                Size = new Size(320, 22),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f)
            };
            _chkSimScramble.CheckedChanged += (s, e) =>
            {
                if (_chkSimScramble.Checked)
                {
                    _service.TriggerScramble();
                }
            };
            pnlLeft.Controls.Add(_chkSimScramble);

            var btnLeak = new Button
            {
                Text = "⚠️ Kaçak / Wastegate Hortum Yırtılması Simülasyonu",
                Location = new Point(20, startY + 4 * step + 15),
                Size = new Size(330, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLeak.FlatAppearance.BorderSize = 0;
            btnLeak.Click += (s, e) =>
            {
                // Simülasyonda kaçak yaratmak için gerçek boost'u 105 kPa'da dondururuz
                _simActualBoost = 105.0;
            };
            pnlLeft.Controls.Add(btnLeak);

            // Sağ Panel - Sim Outputs
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblOutTitle = new Label { Text = "🕹️ Solenoid & PID Kontrol Çıktıları", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlRight.Controls.Add(lblOutTitle);

            int outY = 48;
            int outStep = 32;

            AddSimOutputRow(pnlRight, "Hedef Turbo Basıncı:", _lblTargetBoost = new Label { Text = "150 kPa" }, outY);
            AddSimOutputRow(pnlRight, "Aktif Turbo Basıncı:", _lblActualBoost = new Label { Text = "100 kPa" }, outY + outStep);
            AddSimOutputRow(pnlRight, "Wastegate Solenoid Duty:", _lblWgDuty = new Label { Text = "%20.0" }, outY + 2 * outStep);

            var lblVtecTitle = new Label { Text = "VTEC Valf Sinyali (Solenoid):", ForeColor = TextMuted, Location = new Point(12, outY + 3 * outStep), Size = new Size(160, 20) };
            pnlRight.Controls.Add(lblVtecTitle);

            _lblVtecState = new Label
            {
                Text = HondaTuner.Core.Localization.L.Get("vtec_inactive"),
                Location = new Point(180, outY + 3 * outStep),
                Size = new Size(160, 20),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            pnlRight.Controls.Add(_lblVtecState);

            _lblAlarmStatus = new Label
            {
                Text = HondaTuner.Core.Localization.L.Get("wg_system_safe"),
                Location = new Point(12, outY + 4 * outStep + 20),
                Size = new Size(330, 24),
                ForeColor = AccentGreen,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(16, 40, 25)
            };
            pnlRight.Controls.Add(_lblAlarmStatus);
        }

        private void AddSimOutputRow(Panel pnl, string title, Label valLbl, int y)
        {
            var lblTitle = new Label
            {
                Text = title,
                ForeColor = TextMuted,
                Location = new Point(12, y),
                Size = new Size(160, 20)
            };
            pnl.Controls.Add(lblTitle);

            valLbl.Location = new Point(180, y);
            valLbl.Size = new Size(160, 20);
            valLbl.ForeColor = TextPrimary;
            valLbl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            pnl.Controls.Add(valLbl);
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
            // 1. Target Boost Grid (Base: cols = Gears 1-5, rows = RPM Bins)
            _dgvBoostTargets.Columns.Clear();
            _dgvBoostTargets.Columns.Add("RPM", "Devir (RPM)");
            _dgvBoostTargets.Columns[0].Width = 80;
            _dgvBoostTargets.Columns[0].ReadOnly = true;

            for (int g = 1; g <= 5; g++)
            {
                _dgvBoostTargets.Columns.Add($"Vites{g}", $"{g}. Vites (kPa)");
                _dgvBoostTargets.Columns[g].Width = 70;
            }

            for (int r = 0; r < _service.Tables.BoostRpmBins.Length; r++)
            {
                var row = new object[6];
                row[0] = _service.Tables.BoostRpmBins[r].ToString();
                for (int c = 0; c < 5; c++)
                {
                    row[c + 1] = Math.Round(_service.Tables.BoostTargets[r, c], 0);
                }
                _dgvBoostTargets.Rows.Add(row);
            }

            // 2. Wg base Grid (Base: cols = WgBoostBins, rows = RPM Bins)
            _dgvWgDuties.Columns.Clear();
            _dgvWgDuties.Columns.Add("RPM", "Devir (RPM)");
            _dgvWgDuties.Columns[0].Width = 80;
            _dgvWgDuties.Columns[0].ReadOnly = true;

            for (int b = 0; b < _service.Tables.WgBoostBins.Length; b++)
            {
                _dgvWgDuties.Columns.Add($"Boost{b}", $"{_service.Tables.WgBoostBins[b]} kPa (%)");
                _dgvWgDuties.Columns[b + 1].Width = 80;
            }

            for (int r = 0; r < _service.Tables.BoostRpmBins.Length; r++)
            {
                var row = new object[_service.Tables.WgBoostBins.Length + 1];
                row[0] = _service.Tables.BoostRpmBins[r].ToString();
                for (int c = 0; c < _service.Tables.WgBoostBins.Length; c++)
                {
                    row[c + 1] = Math.Round(_service.Tables.BaseWgDuty[r, c], 1);
                }
                _dgvWgDuties.Rows.Add(row);
            }
        }

        private void VtecLimits_Changed(object sender, EventArgs e)
        {
            double minR = GetDouble(_txtVtecMinRpm.Text, _service.Tables.VtecMinRpm);
            double minS = GetDouble(_txtVtecMinSpeed.Text, _service.Tables.VtecMinSpeed);
            _service.Tables.VtecMinRpm = minR;
            _service.Tables.VtecMinSpeed = minS;
        }

        private void DgvBoostTargets_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvBoostTargets.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                _service.Tables.BoostTargets[e.RowIndex, e.ColumnIndex - 1] = val;
            }
            catch (Exception ex) { Debug.WriteLine($"[VtecBoostControl] BoostTargets hücresi parse hatası: {ex.Message}"); }
        }

        private void DgvWgDuties_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvWgDuties.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                _service.Tables.BaseWgDuty[e.RowIndex, e.ColumnIndex - 1] = val;
            }
            catch (Exception ex) { Debug.WriteLine($"[VtecBoostControl] WgDuties hücresi parse hatası: {ex.Message}"); }
        }

        private void Service_WgFailureAlarm(object sender, string alarmMsg)
        {
            _lblAlarmStatus.Text = alarmMsg;
            _lblAlarmStatus.BackColor = Color.FromArgb(60, 20, 20);
            _lblAlarmStatus.ForeColor = AccentRed;
        }

        private void SimTimer_Tick(object sender, EventArgs e)
        {
            _service.UpdateTimers(0.1);

            double rpm = GetDouble(_txtSimRpm.Text);
            double speed = GetDouble(_txtSimSpeed.Text);
            int gear = (int)GetDouble(_txtSimGear.Text);

            // VTEC Durumu
            bool vtec = _service.IsVtecActive(rpm, speed, gear);
            if (vtec)
            {
                _lblVtecState.Text = HondaTuner.Core.Localization.L.Get("vtec_active");
                _lblVtecState.ForeColor = AccentGreen;
            }
            else
            {
                _lblVtecState.Text = HondaTuner.Core.Localization.L.Get("vtec_inactive");
                _lblVtecState.ForeColor = TextMuted;
            }

            // Target Boost
            _simTargetBoost = _service.GetTargetBoost(rpm, gear);
            _lblTargetBoost.Text = $"{_simTargetBoost.ToString("F0")} kPa";

            // Normal simülasyonda actual boost target boost'a yavaş yavaş yaklaşır
            if (_simActualBoost != 105.0) // Kaçak simülasyonu tetiklenmemişse:
            {
                _simActualBoost += (_simTargetBoost - _simActualBoost) * 0.15;
            }
            _lblActualBoost.Text = $"{_simActualBoost.ToString("F0")} kPa";

            // WG doluluk hesabı
            double duty = _service.CalculateWgDuty(_simTargetBoost, _simActualBoost, rpm, 0.1);
            _lblWgDuty.Text = $"%{duty.ToString("F1")}";

            // Alarm durumu
            if (_service.WgHighDutyTimer < 0.1)
            {
                _lblAlarmStatus.Text = HondaTuner.Core.Localization.L.Get("wg_system_safe");
                _lblAlarmStatus.BackColor = Color.FromArgb(16, 40, 25);
                _lblAlarmStatus.ForeColor = AccentGreen;
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_simTimer != null)
                {
                    _simTimer.Tick -= SimTimer_Tick;
                    _simTimer.Stop();
                    _simTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
