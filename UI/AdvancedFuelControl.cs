using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using HondaTuner.Calibration.Fuel;

namespace HondaTuner.UI
{
    public class AdvancedFuelControl : UserControl, ILocalizable
    {
        public void ApplyLocalization()
        {
            MainForm.ApplyRecursiveLocalization(this);
        }
        private AdvancedFuelService _fuelService;
        private double _transientFuelAccumulator = 0.0;
        private Timer _transientTimer;

        // UI Bileşenleri
        private DataGridView _dgvAlphaN;
        private DataGridView _dgvMafScale;
        private DataGridView _dgvColdStart;

        // Simülatör Girdileri
        private TextBox _txtSimRpm;
        private TextBox _txtSimPulseWidth;
        private TextBox _txtSimEct;
        private TextBox _txtSimActPressure;
        private TextBox _txtSimTarPressure;
        private TextBox _txtSimDtps;
        private CheckBox _chkSimAlphaN;

        // Simülatör Çıktıları
        private Label _lblFinalPulseWidth;
        private Label _lblFinalDutyCycle;
        private Label _lblTransientAccum;
        private Label _lblShortPulseAdder;
        private Label _lblAlarmStatus;
        private Button _btnBlastThrottle;

        // Renk Paleti
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public AdvancedFuelControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;
            _fuelService = new AdvancedFuelService();
            _fuelService.InjectorSaturationAlarm += FuelService_InjectorSaturationAlarm;

            InitializeLayout();

            _transientTimer = new Timer();
            _transientTimer.Interval = 100; // 100 ms
            _transientTimer.Tick += TransientTimer_Tick;

            LoadServiceValues();
        }

        private void InitializeLayout()
        {
            var tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = BgDark
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f)); // Tablolar
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f)); // Simülatör

            // 1. SOL PANEL (SEKMELİ TABLO EDİTÖRLERİ)
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tlpMain.Controls.Add(pnlLeft, 0, 0);

            var tcTables = new TabControl { Dock = DockStyle.Fill };
            pnlLeft.Controls.Add(tcTables);

            // Tab 1: Alpha-N
            var tpAlphaN = new TabPage("⛽ Alpha-N VE");
            tpAlphaN.BackColor = BgPanel;
            _dgvAlphaN = CreateStyledGrid();
            _dgvAlphaN.Dock = DockStyle.Fill;
            _dgvAlphaN.CellValueChanged += DgvAlphaN_CellValueChanged;
            tpAlphaN.Controls.Add(_dgvAlphaN);
            tcTables.TabPages.Add(tpAlphaN);

            // Tab 2: MAF Scale
            var tpMaf = new TabPage("🔌 MAF Ölçeği");
            tpMaf.BackColor = BgPanel;
            _dgvMafScale = CreateStyledGrid();
            _dgvMafScale.Dock = DockStyle.Fill;
            _dgvMafScale.CellValueChanged += DgvMafScale_CellValueChanged;
            tpMaf.Controls.Add(_dgvMafScale);
            tcTables.TabPages.Add(tpMaf);

            // Tab 3: Cold Start
            var tpCold = new TabPage("🌡️ Soğuk Çalışma & Düzeltmeler");
            tpCold.BackColor = BgPanel;
            _dgvColdStart = CreateStyledGrid();
            _dgvColdStart.Dock = DockStyle.Fill;
            _dgvColdStart.CellValueChanged += DgvColdStart_CellValueChanged;
            tpCold.Controls.Add(_dgvColdStart);
            tcTables.TabPages.Add(tpCold);

            // 2. SAĞ PANEL (İNJEKTÖR SİMÜLATÖRÜ)
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            tlpMain.Controls.Add(pnlRight, 1, 0);

            var gbSimulator = new GroupBox
            {
                Text = "⚡ Canlı Enjektör & Düzeltme Simülatörü",
                Dock = DockStyle.Fill,
                ForeColor = AccentBlue,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Padding = new Padding(12),
                BackColor = BgPanel
            };
            pnlRight.Controls.Add(gbSimulator);

            int startY = 32;
            int stepY = 32;

            // Girişler
            AddLabel(gbSimulator, "Motor Devri (RPM):", 12, startY);
            _txtSimRpm = AddTextBox(gbSimulator, "2000", 170, startY, 70);

            AddLabel(gbSimulator, "Taban Yakıt Süresi (ms):", 12, startY + stepY);
            _txtSimPulseWidth = AddTextBox(gbSimulator, "4.0", 170, startY + stepY, 70);

            AddLabel(gbSimulator, "Motor Sıcaklık (°C ECT):", 12, startY + 2 * stepY);
            _txtSimEct = AddTextBox(gbSimulator, "80", 170, startY + 2 * stepY, 70);

            AddLabel(gbSimulator, "Yakıt Basıncı (psi - Aktif):", 12, startY + 3 * stepY);
            _txtSimActPressure = AddTextBox(gbSimulator, "43.5", 170, startY + 3 * stepY, 70);

            AddLabel(gbSimulator, "Hedef Yakıt Basıncı (psi):", 12, startY + 4 * stepY);
            _txtSimTarPressure = AddTextBox(gbSimulator, "43.5", 170, startY + 4 * stepY, 70);

            AddLabel(gbSimulator, "Gaz Değişim Hızı (dTPS %/s):", 12, startY + 5 * stepY);
            _txtSimDtps = AddTextBox(gbSimulator, "0.0", 170, startY + 5 * stepY, 70);

            _chkSimAlphaN = new CheckBox
            {
                Text = "Alpha-N Yakıt Modunu Kullan (TPS vs RPM)",
                Location = new Point(12, startY + 6 * stepY),
                Size = new Size(320, 22),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f)
            };
            _chkSimAlphaN.CheckedChanged += Inputs_Changed;
            gbSimulator.Controls.Add(_chkSimAlphaN);

            // Blast Throttle
            _btnBlastThrottle = new Button
            {
                Text = "💥 Gaz Pedalına Hızlıca Bas (Throttle Step Sim)",
                Location = new Point(12, startY + 7 * stepY),
                Size = new Size(330, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBlastThrottle.FlatAppearance.BorderSize = 0;
            _btnBlastThrottle.Click += BtnBlastThrottle_Click;
            gbSimulator.Controls.Add(_btnBlastThrottle);

            // Çıktı Bölgesi (Separator)
            var pnlSeparator = new Panel
            {
                Location = new Point(12, startY + 8 * stepY + 10),
                Size = new Size(330, 2),
                BackColor = Color.FromArgb(60, 70, 90)
            };
            gbSimulator.Controls.Add(pnlSeparator);

            int outY = startY + 8 * stepY + 20;

            // Çıktılar
            AddLabel(gbSimulator, "Kısa Enjeksiyon Eklemesi (adder):", 12, outY, TextMuted);
            _lblShortPulseAdder = AddOutLabel(gbSimulator, "0.00 ms", 240, outY, TextPrimary);

            AddLabel(gbSimulator, "Geçici Yakıt Havuzu (acc):", 12, outY + 22, TextMuted);
            _lblTransientAccum = AddOutLabel(gbSimulator, "0.00 ms", 240, outY + 22, TextPrimary);

            AddLabel(gbSimulator, "Nihai Enjeksiyon Süresi (PW):", 12, outY + 44, TextMuted);
            _lblFinalPulseWidth = AddOutLabel(gbSimulator, "4.00 ms", 240, outY + 44, AccentGreen, true);

            AddLabel(gbSimulator, "Enjektör Görev Döngüsü (Duty):", 12, outY + 66, TextMuted);
            _lblFinalDutyCycle = AddOutLabel(gbSimulator, "%13.3", 240, outY + 66, AccentGreen, true);

            _lblAlarmStatus = new Label
            {
                Text = HondaTuner.Core.Localization.L.Get("duty_cycle_safe"),
                Location = new Point(12, outY + 95),
                Size = new Size(330, 24),
                ForeColor = AccentGreen,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(16, 40, 25)
            };
            gbSimulator.Controls.Add(_lblAlarmStatus);

            // Değişiklik tetikleme olayları
            _txtSimRpm.TextChanged += Inputs_Changed;
            _txtSimPulseWidth.TextChanged += Inputs_Changed;
            _txtSimEct.TextChanged += Inputs_Changed;
            _txtSimActPressure.TextChanged += Inputs_Changed;
            _txtSimTarPressure.TextChanged += Inputs_Changed;
            _txtSimDtps.TextChanged += Inputs_Changed;

            Controls.Add(tlpMain);
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

        private void AddLabel(Control container, string text, int x, int y, Color? color = null)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color ?? TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            container.Controls.Add(lbl);
        }

        private TextBox AddTextBox(Control container, string defaultVal, int x, int y, int width)
        {
            var txt = new TextBox
            {
                Text = defaultVal,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                TextAlign = HorizontalAlignment.Center
            };
            container.Controls.Add(txt);
            return txt;
        }

        private Label AddOutLabel(Control container, string text, int x, int y, Color color, bool bold = false)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color,
                Font = new Font("Segoe UI", 9f, bold ? FontStyle.Bold : FontStyle.Regular)
            };
            container.Controls.Add(lbl);
            return lbl;
        }

        private void LoadServiceValues()
        {
            // 1. Alpha-N Grid Yükle (Cols = RPM Bins, Rows = TPS Bins)
            _dgvAlphaN.Columns.Clear();
            _dgvAlphaN.Columns.Add("TPS", "TPS %");
            _dgvAlphaN.Columns[0].Width = 55;
            _dgvAlphaN.Columns[0].ReadOnly = true;

            for (int c = 0; c < _fuelService.Tables.AlphaNRpmBins.Length; c++)
            {
                string rpmLabel = _fuelService.Tables.AlphaNRpmBins[c].ToString();
                _dgvAlphaN.Columns.Add($"Col{c}", rpmLabel);
                _dgvAlphaN.Columns[c + 1].Width = 48;
            }

            for (int r = 0; r < _fuelService.Tables.AlphaNTpsBins.Length; r++)
            {
                var rowData = new object[_fuelService.Tables.AlphaNRpmBins.Length + 1];
                rowData[0] = _fuelService.Tables.AlphaNTpsBins[r].ToString();
                for (int c = 0; c < _fuelService.Tables.AlphaNRpmBins.Length; c++)
                {
                    rowData[c + 1] = Math.Round(_fuelService.Tables.AlphaNVolumetricEfficiency[r, c], 1);
                }
                _dgvAlphaN.Rows.Add(rowData);
            }

            // 2. MAF Scale Grid Yükle
            _dgvMafScale.Columns.Clear();
            _dgvMafScale.Columns.Add("Index", "Kademe");
            _dgvMafScale.Columns.Add("Volt", "MAF Sensör (Volt)");
            _dgvMafScale.Columns.Add("Flow", "Hava Debisi (g/s)");
            _dgvMafScale.Columns[0].Width = 60;
            _dgvMafScale.Columns[0].ReadOnly = true;
            _dgvMafScale.Columns[1].Width = 140;
            _dgvMafScale.Columns[2].Width = 140;

            for (int i = 0; i < _fuelService.Tables.MafVoltages.Length; i++)
            {
                _dgvMafScale.Rows.Add(
                    i,
                    _fuelService.Tables.MafVoltages[i].ToString("F2"),
                    _fuelService.Tables.MafFlowRates[i].ToString("F1")
                );
            }

            // 3. Cold Start Grid Yükle
            _dgvColdStart.Columns.Clear();
            _dgvColdStart.Columns.Add("Index", "Kademe");
            _dgvColdStart.Columns.Add("ECT", "Hararet (°C ECT)");
            _dgvColdStart.Columns.Add("Mult", "Sıcaklık Yakıt Çarpanı");
            _dgvColdStart.Columns[0].Width = 60;
            _dgvColdStart.Columns[0].ReadOnly = true;
            _dgvColdStart.Columns[1].Width = 140;
            _dgvColdStart.Columns[2].Width = 140;

            for (int i = 0; i < _fuelService.Tables.ColdStartEctBins.Length; i++)
            {
                _dgvColdStart.Rows.Add(
                    i,
                    _fuelService.Tables.ColdStartEctBins[i].ToString("F0"),
                    _fuelService.Tables.ColdStartMultipliers[i].ToString("F2")
                );
            }
        }

        private void DgvAlphaN_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvAlphaN.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                _fuelService.Tables.AlphaNVolumetricEfficiency[e.RowIndex, e.ColumnIndex - 1] = val;
                RunSimulation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvancedFuelControl] AlphaN hücre güncellenemedi: {ex.Message}");
            }
        }

        private void DgvMafScale_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvMafScale.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                if (e.ColumnIndex == 1)
                    _fuelService.Tables.MafVoltages[e.RowIndex] = val;
                else if (e.ColumnIndex == 2)
                    _fuelService.Tables.MafFlowRates[e.RowIndex] = val;
                RunSimulation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvancedFuelControl] MafScale hücre güncellenemedi: {ex.Message}");
            }
        }

        private void DgvColdStart_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
            try
            {
                double val = Convert.ToDouble(_dgvColdStart.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                if (e.ColumnIndex == 1)
                    _fuelService.Tables.ColdStartEctBins[e.RowIndex] = val;
                else if (e.ColumnIndex == 2)
                    _fuelService.Tables.ColdStartMultipliers[e.RowIndex] = val;
                RunSimulation();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdvancedFuelControl] ColdStart hücre güncellenemedi: {ex.Message}");
            }
        }

        private void Inputs_Changed(object sender, EventArgs e)
        {
            RunSimulation();
        }

        private void BtnBlastThrottle_Click(object sender, EventArgs e)
        {
            _transientFuelAccumulator = 0.0;
            _txtSimDtps.Text = FormatDtps(120.0);
            _transientTimer.Start();
        }

        private void TransientTimer_Tick(object sender, EventArgs e)
        {
            double dTPS = GetDouble(_txtSimDtps.Text);

            // dTPS hızla sönümlenir; küçük/bozuk değerler UI'a taşmadan sıfırlanır.
            dTPS = double.IsFinite(dTPS) ? Math.Max(0.0, dTPS - 30.0) : 0.0;
            if (dTPS < 0.1)
            {
                dTPS = 0.0;
            }

            _txtSimDtps.Text = FormatDtps(dTPS);

            RunSimulation();

            if (dTPS <= 0.0)
            {
                _transientTimer.Stop();
            }
        }

        private void FuelService_InjectorSaturationAlarm(object sender, double dutyCycle)
        {
            _lblAlarmStatus.Text = string.Format(HondaTuner.Core.Localization.L.Get("alarm_injector_saturation"), Math.Round(dutyCycle, 1));
            _lblAlarmStatus.BackColor = Color.FromArgb(60, 20, 20);
            _lblAlarmStatus.ForeColor = AccentRed;
            _lblFinalDutyCycle.ForeColor = AccentRed;
        }

        private void RunSimulation()
        {
            double rpm = GetDouble(_txtSimRpm.Text);
            double basePw = GetDouble(_txtSimPulseWidth.Text);
            double ect = GetDouble(_txtSimEct.Text);
            double actPrs = GetDouble(_txtSimActPressure.Text);
            double tarPrs = GetDouble(_txtSimTarPressure.Text);
            double dTPS = GetDouble(_txtSimDtps.Text);
            bool useAlphaN = _chkSimAlphaN.Checked;

            // Alarm arayüzünü başlangıçta güvenli sıfırla
            _lblAlarmStatus.Text = HondaTuner.Core.Localization.L.Get("duty_cycle_safe");
            _lblAlarmStatus.BackColor = Color.FromArgb(16, 40, 25);
            _lblAlarmStatus.ForeColor = AccentGreen;
            _lblFinalDutyCycle.ForeColor = AccentGreen;

            double finalPulseWidth = _fuelService.CalculatePulseWidth(
                basePw,
                rpm,
                15.0, // TPS varsayılan
                ect,
                100.0, // MAP varsayılan
                actPrs,
                tarPrs,
                dTPS,
                useAlphaN,
                ref _transientFuelAccumulator
            );

            double shortPulseAdder = _fuelService.CalculateShortPulseAdder(finalPulseWidth);
            _lblShortPulseAdder.Text = $"{shortPulseAdder.ToString("F3")} ms";
            _lblTransientAccum.Text = $"{_transientFuelAccumulator.ToString("F3")} ms";
            _lblFinalPulseWidth.Text = $"{finalPulseWidth.ToString("F2")} ms";

            double dutyCycle = (rpm * finalPulseWidth) / 1200.0;
            _lblFinalDutyCycle.Text = $"%{Math.Round(dutyCycle, 1)}";
        }

        private double GetDouble(string text)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double res))
                return res;
            if (double.TryParse(text, out double resLocal))
                return resLocal;
            return 0.0;
        }

        private static string FormatDtps(double value)
        {
            if (!double.IsFinite(value) || value < 0.1)
                value = 0.0;
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_transientTimer != null)
                {
                    _transientTimer.Tick -= TransientTimer_Tick;
                    _transientTimer.Stop();
                    _transientTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
