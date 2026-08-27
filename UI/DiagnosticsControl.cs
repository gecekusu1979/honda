using System;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Calibration.Diagnostics;

namespace HondaTuner.UI
{
    public class DiagnosticsControl : UserControl
    {
        private DiagnosticsService _service;
        private Timer _trafficTimer;

        // UI Kontrolleri - Arayüz ve Öz-Test
        private ComboBox _cbProtocol;
        private TextBox _txtBaud;
        private TextBox _txtIp;
        private TextBox _txtPort;
        private RichTextBox _rtbConsole;

        // UI Kontrolleri - Freeze Frame
        private DataGridView _dgvFreezeFrames;
        private ComboBox _cbDtcSelector;

        // UI Kontrolleri - A2L
        private RichTextBox _rtbA2l;

        // Tasarım Renk Kodları
        private static readonly Color BgDark = Color.FromArgb(16, 20, 30);
        private static readonly Color BgPanel = Color.FromArgb(24, 28, 40);
        private static readonly Color AccentBlue = Color.FromArgb(0, 150, 255);
        private static readonly Color AccentRed = Color.FromArgb(231, 76, 60);
        private static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        private static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        private static readonly Color TextMuted = Color.FromArgb(140, 150, 170);

        public DiagnosticsControl()
        {
            Dock = DockStyle.Fill;
            BackColor = BgDark;

            _service = new DiagnosticsService();
            _service.TestLogAdded += (s, msg) => AppendConsoleLog(msg);

            InitializeLayout();
            LoadData();

            _trafficTimer = new Timer { Interval = 1500 };
            _trafficTimer.Tick += TrafficTimer_Tick;
            _trafficTimer.Start();
        }

        private void InitializeLayout()
        {
            var tc = new TabControl { Dock = DockStyle.Fill };
            Controls.Add(tc);

            // Tab 1: Protocol & Self Test
            var tpInterface = new TabPage("📶 Protokol & Donanım Arayüzleri");
            tpInterface.BackColor = BgPanel;
            InitializeInterfaceTab(tpInterface);
            tc.TabPages.Add(tpInterface);

            // Tab 2: Freeze Frames
            var tpFreeze = new TabPage("📷 Freeze Frame Günlükleri");
            tpFreeze.BackColor = BgPanel;
            InitializeFreezeTab(tpFreeze);
            tc.TabPages.Add(tpFreeze);

            // Tab 3: A2L
            var tpA2l = new TabPage("📝 Standartlar & A2L Export");
            tpA2l.BackColor = BgPanel;
            InitializeA2lTab(tpA2l);
            tc.TabPages.Add(tpA2l);
        }

        private void InitializeInterfaceTab(TabPage page)
        {
            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2, BackColor = BgDark };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            page.Controls.Add(tlp);

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlLeft, 0, 0);

            var lblTitle = new Label { Text = "📡 Dönüştürücü & Protokol Arayüzü", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(300, 24) };
            pnlLeft.Controls.Add(lblTitle);

            int startY = 48;
            int step = 35;

            var lblProto = new Label { Text = "Donanım Arayüzü:", ForeColor = TextPrimary, Location = new Point(12, startY + 2), Size = new Size(130, 20) };
            pnlLeft.Controls.Add(lblProto);

            _cbProtocol = new ComboBox { Location = new Point(150, startY), Size = new Size(120, 20), BackColor = BgDark, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            _cbProtocol.Items.AddRange(new object[] { "OBD1", "ISO9141", "CAN_BUS", "J2534" });
            _cbProtocol.SelectedIndex = 0;
            _cbProtocol.SelectedIndexChanged += ProtocolChanged;
            pnlLeft.Controls.Add(_cbProtocol);

            var lblBaud = new Label { Text = "Datalog Baud Rate:", ForeColor = TextPrimary, Location = new Point(12, startY + step + 2), Size = new Size(130, 20) };
            pnlLeft.Controls.Add(lblBaud);

            _txtBaud = new TextBox { Text = "38400", Location = new Point(150, startY + step), Size = new Size(120, 20), BackColor = BgDark, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };
            _txtBaud.TextChanged += ConfigChanged;
            pnlLeft.Controls.Add(_txtBaud);

            var lblIp = new Label { Text = "WiFi IP Address:", ForeColor = TextPrimary, Location = new Point(12, startY + 2 * step + 2), Size = new Size(130, 20) };
            pnlLeft.Controls.Add(lblIp);

            _txtIp = new TextBox { Text = "192.168.1.10", Location = new Point(150, startY + 2 * step), Size = new Size(120, 20), BackColor = BgDark, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };
            _txtIp.TextChanged += ConfigChanged;
            pnlLeft.Controls.Add(_txtIp);

            var lblPort = new Label { Text = "WiFi Port:", ForeColor = TextPrimary, Location = new Point(12, startY + 3 * step + 2), Size = new Size(130, 20) };
            pnlLeft.Controls.Add(lblPort);

            _txtPort = new TextBox { Text = "8080", Location = new Point(150, startY + 3 * step), Size = new Size(120, 20), BackColor = BgDark, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };
            _txtPort.TextChanged += ConfigChanged;
            pnlLeft.Controls.Add(_txtPort);

            var btnSelfTest = new Button
            {
                Text = "⚡ ECU Diagnostic Self-Test Başlat",
                Location = new Point(12, startY + 4 * step + 15),
                Size = new Size(258, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AccentGreen,
                BackColor = BgDark,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelfTest.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 43, 33);
            btnSelfTest.Click += BtnSelfTest_Click;
            pnlLeft.Controls.Add(btnSelfTest);

            // Sağ Taraf - Konsol
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            tlp.Controls.Add(pnlRight, 1, 0);

            var lblConsole = new Label { Text = "🖥️ Canlı İletişim & Hata Tanı Konsolu", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = TextPrimary, Location = new Point(12, 12), Size = new Size(300, 20) };
            pnlRight.Controls.Add(lblConsole);

            _rtbConsole = new RichTextBox
            {
                Location = new Point(12, 38),
                Size = new Size(350, 240),
                BackColor = BgDark,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlRight.Controls.Add(_rtbConsole);
        }

        private void InitializeFreezeTab(TabPage page)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            page.Controls.Add(pnl);

            var lblTitle = new Label { Text = "📷 Hata Kodu Dondurulmuş Veri Çerçeveleri (Freeze Frames)", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(450, 24) };
            pnl.Controls.Add(lblTitle);

            var lblDtc = new Label { Text = "Arıza Kodu Seç:", ForeColor = TextPrimary, Location = new Point(12, 45), Size = new Size(90, 20) };
            pnl.Controls.Add(lblDtc);

            _cbDtcSelector = new ComboBox { Location = new Point(110, 42), Size = new Size(110, 20), BackColor = BgDark, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat };
            _cbDtcSelector.Items.AddRange(new object[] { "P0130 (O2 Sensor)", "P0105 (MAP Sensor)", "P0325 (Knock Sensor)", "P1259 (VTEC System)" });
            _cbDtcSelector.SelectedIndex = 0;
            pnl.Controls.Add(_cbDtcSelector);

            var btnTrigger = new Button
            {
                Text = "💥 Hata Tetikle (Freeze Frame)",
                Location = new Point(230, 40),
                Size = new Size(180, 23),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AccentRed,
                BackColor = BgDark,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTrigger.Click += BtnTrigger_Click;
            pnl.Controls.Add(btnTrigger);

            _dgvFreezeFrames = CreateStyledGrid();
            _dgvFreezeFrames.Location = new Point(12, 75);
            _dgvFreezeFrames.Size = new Size(680, 190);
            _dgvFreezeFrames.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnl.Controls.Add(_dgvFreezeFrames);
        }

        private void InitializeA2lTab(TabPage page)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12), BackColor = BgPanel };
            page.Controls.Add(pnl);

            var lblTitle = new Label { Text = "📝 ASAM MCD-2 MC (A2L) Kalibrasyon Standart Tanımları", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = AccentBlue, Location = new Point(12, 12), Size = new Size(450, 24) };
            pnl.Controls.Add(lblTitle);

            var btnExport = new Button
            {
                Text = "💾 A2L Harita Dosyası (.a2l) İhraç Et",
                Location = new Point(12, 42),
                Size = new Size(240, 25),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AccentBlue,
                BackColor = BgDark,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExport.Click += BtnExport_Click;
            pnl.Controls.Add(btnExport);

            _rtbA2l = new RichTextBox
            {
                Location = new Point(12, 75),
                Size = new Size(680, 190),
                BackColor = BgDark,
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnl.Controls.Add(_rtbA2l);
        }

        private void ProtocolChanged(object sender, EventArgs e)
        {
            _service.Tables.SelectedProtocol = _cbProtocol.SelectedItem.ToString();
            AppendConsoleLog($"[SYSTEM] İletişim protokolü değiştirildi: {_service.Tables.SelectedProtocol}");
        }

        private void ConfigChanged(object sender, EventArgs e)
        {
            int.TryParse(_txtBaud.Text, out int b);
            _service.Tables.DataloggingBaudRate = b;

            _service.Tables.WifiIpAddress = _txtIp.Text;
            int.TryParse(_txtPort.Text, out int p);
            _service.Tables.WifiPort = p;
        }

        private void BtnSelfTest_Click(object sender, EventArgs e)
        {
            AppendConsoleLog("[SYSTEM] Cihaz öz-teşhis taraması başlatılıyor...");
            string report = _service.RunEcuSelfTest();
            AppendConsoleLog(report);
        }

        private void BtnTrigger_Click(object sender, EventArgs e)
        {
            string rawCode = _cbDtcSelector.SelectedItem.ToString();
            string code = rawCode.Split(' ')[0]; // Sadece 'P0130' alır

            // Simüle edilmiş sürüş değerleriyle hata tetikle
            _service.TriggerDtc(code, 4500, 88.0, 42.0, 85.0, 160.0);
            RefreshFreezeFrameGrid();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            string a2l = _service.GenerateA2L();
            _rtbA2l.Text = a2l;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "A2L Files (*.a2l)|*.a2l|All Files (*.*)|*.*";
                sfd.FileName = "P28_HondaTuner.a2l";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(sfd.FileName, a2l);
                    MessageBox.Show("A2L dosyası başarıyla kaydedildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void LoadData()
        {
            _rtbA2l.Text = _service.GenerateA2L();

            // Freeze Frame Grid Sütunları
            _dgvFreezeFrames.Columns.Clear();
            _dgvFreezeFrames.Columns.Add("Dtc", "Arıza Kodu");
            _dgvFreezeFrames.Columns.Add("Time", "Tetiklenme Saati");
            _dgvFreezeFrames.Columns.Add("Rpm", "Motor Devri (RPM)");
            _dgvFreezeFrames.Columns.Add("Ect", "Su Sıcaklığı (°C)");
            _dgvFreezeFrames.Columns.Add("Iat", "Intake Sıcaklığı (°C)");
            _dgvFreezeFrames.Columns.Add("Speed", "Hız (km/h)");
            _dgvFreezeFrames.Columns.Add("Boost", "Boost MAP (kPa)");

            foreach (var col in _dgvFreezeFrames.Columns)
            {
                ((DataGridViewColumn)col).ReadOnly = true;
                ((DataGridViewColumn)col).Width = 92;
            }
            _dgvFreezeFrames.Columns[1].Width = 120; // Time daha geniştir
        }

        private void RefreshFreezeFrameGrid()
        {
            _dgvFreezeFrames.Rows.Clear();
            foreach (var frame in _service.SavedFreezeFrames)
            {
                _dgvFreezeFrames.Rows.Add(
                    frame.DtcCode,
                    frame.Timestamp.ToString("HH:mm:ss.fff"),
                    frame.Rpm.ToString("F0"),
                    frame.Ect.ToString("F1"),
                    frame.Iat.ToString("F1"),
                    frame.VehicleSpeed.ToString("F1"),
                    frame.Boost.ToString("F0")
                );
            }
        }

        private void TrafficTimer_Tick(object sender, EventArgs e)
        {
            string traffic = _service.SimulateProtocolTraffic();
            AppendConsoleLog(traffic);
        }

        private void AppendConsoleLog(string msg)
        {
            if (_rtbConsole.IsDisposed) return;
            if (_rtbConsole.TextLength > 10000) _rtbConsole.Clear();
            _rtbConsole.AppendText(msg + Environment.NewLine);
            _rtbConsole.SelectionStart = _rtbConsole.Text.Length;
            _rtbConsole.ScrollToCaret();
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trafficTimer != null)
                {
                    _trafficTimer.Tick -= TrafficTimer_Tick;
                    _trafficTimer.Stop();
                    _trafficTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
