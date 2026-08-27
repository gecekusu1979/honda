using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Windows.Forms;
using HondaTuner.Core;
using HondaTuner.Core.Config;
using HondaTuner.Core.Rom;
using HondaTuner.Core.Algorithms;

namespace HondaTuner.UI
{
    public class MainForm : Form
    {
        // ── Renk Paleti ─────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(13, 17, 23);
        private static readonly Color BgPanel = Color.FromArgb(22, 27, 34);
        private static readonly Color BgCard = Color.FromArgb(33, 38, 45);
        private static readonly Color AccentRed = Color.FromArgb(233, 69, 96);
        private static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
        private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
        private static readonly Color VtecGreen = Color.FromArgb(63, 185, 80);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);

        // ── Bileşenler ───────────────────────────────────────────
        private readonly RomParser _parser = new RomParser();
        private readonly RomBackupManager _backupMgr = new RomBackupManager();
        private List<EcuProfile> _loadedProfiles = new List<EcuProfile>();
        private EcuProfile _activeProfile = EcuProfiles.P28;
        private VehicleEntry _activeVehicle;

        private MapGridControl _fuelGrid;
        private MapGridControl _ignGrid;
        private DiffView _diffView;

        // Özel sekme sistemi (TabControl yerine — her zaman görünür)
        private Panel _tabBar;           // sekme butonları şeridi
        private Panel _contentArea;      // içerik alanı
        private Panel[] _tabPages;       // sayfalar (index = sekme numarası)
        private Button[] _tabButtons;    // sekme butonları
        private int _activeTabIndex = 0;
        private Panel _diffPage;         // diff sayfası referansı için

        private StatusStrip _status;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripStatusLabel _checksumLabel;
        private ToolStripStatusLabel _profileLabel;

        // VTEC / Limitler Paneli
        private Panel _vtecPanel;
        private NumericUpDown _vtecRpmSpinner;
        private NumericUpDown _revLimitSpinner;
        private NumericUpDown _speedLimitSpinner;
        private NumericUpDown _vtecLoadSpinner;
        private NumericUpDown _injDeadTimeSpinner;
        private Label _vtecRpmLabel;
        private Label _headerVehicleLabel;
        private bool _isDirty;

        // Stock haritalar
        private byte[,] _stockFuelMap;
        private byte[,] _stockIgnMap;

        // M1 — Telemetri
        private TelemetryDashboard _telemetryDash;
        private DatalogManager _datalogMgr;
        private ComboBox _comPortCombo;
        private Button _btnConnect;
        private Button _btnSimulate;
        private Button _btnDisconnect;
        private ToolStripStatusLabel _datalogStatusLabel;

        // M4 — 3D Grafikler
        private SurfaceChart3D _fuelChart3D;
        private SurfaceChart3D _ignChart3D;

        // M5 — 3D Parça Görüntüleyici
        private PartViewer3D _partViewer;

        // Tuning Asistani
        private ComboBox _goalCombo;
        private NumericUpDown _injectorCcSpinner;
        private NumericUpDown _mapSensorSpinner;
        private NumericUpDown _targetAfrSpinner;
        private NumericUpDown _measuredAfrSpinner;
        private NumericUpDown _widebandRpmSpinner;
        private NumericUpDown _widebandLoadSpinner;
        private NumericUpDown _widebandRadiusSpinner;
        private TextBox _assistantNotes;
        private Panel _panelNotes;
        private Panel _panelAdvanced;
        private Panel _panelWizards;
        private Panel _rightPanel;

        // Yama Merkezi Bileşenleri
        private ListBox _patchList;
        private TextBox _patchDetailsBox;
        private Button _btnApplyPatch;
        private Button _btnRollbackPatch;
        private ListView _patchAuditListView;
        private Panel _panelPatches;

        // Closed Loop AutoTune (Phase 8) components
        private HondaTuner.Core.AutoTune.IAutoTuneEngine _autoTuneEngine;
        private ComboBox _atModeCombo;
        private ComboBox _atProfileCombo;
        private Button _btnAtStart;
        private Button _btnAtPause;
        private Button _btnAtStop;
        private Label _lblAtStatus;
        private Label _lblAtUser;
        private Label _lblAtEcu;
        private Label _lblAtSafety;
        private Label _lblAtKnock;
        private Label _lblAtEct;
        private Label _lblAtQuality;
        private ListView _atDecisionsListView;
        private long _telemetrySequence = 0;

        // Gelişmiş Ayarlar (Launch / DTC)
        private CheckBox _chkLaunchControlActive;
        private NumericUpDown _numLaunchControlRpm;
        private NumericUpDown _numLaunchControlSpeed;
        private CheckBox _chkDtcKnock;
        private CheckBox _chkDtcVtec;
        private CheckBox _chkDtcO2;
        private CheckBox _chkDtcEld;

        // Sihirbazlar
        private NumericUpDown _numOldInjector;
        private NumericUpDown _numNewInjector;

        // RTP Emulator (Phase 9) components
        private readonly HondaTuner.Core.Rtp.IRtpCalibrationEngine _rtpEngine;
        private Button _btnRtpConnect;
        private Button _btnRtpDisconnect;
        private CheckBox _chkRtpSyncEnabled;
        private Button _btnRtpFullSync;
        private Label _lblRtpState;
        private Label _lblRtpQueueDepth;
        private Label _lblRtpAvgLatency;
        private Label _lblRtpFailureCount;
        private Label _lblRtpDroppedWrites;
        private ComboBox _comboNewMapSensor;
        private MetadataControl _metadataControl;
        private ReverseControl _reverseControl;
        private AdvancedFuelControl _advancedFuelControl;
        private AdvancedIgnitionControl _advancedIgnitionControl;
        private VtecBoostControl _vtecBoostControl;
        private EngineProtectionControl _engineProtectionControl;
        private DiagnosticsControl _diagnosticsControl;
        private DynoLogsControl _dynoLogsControl;

        // ── Donanım Kontrol (Phase 10) ───────────────────────────
        private HondaTuner.Hardware.EEPROM.Ch341aProgrammer _programmer;
        private HondaTuner.Hardware.OBD.DtcManager _dtcManager;
        private HondaTuner.Hardware.Emulator.OstrichEmulator _ostrich;
        private ToolStripStatusLabel _progStatusLabel;
        private ToolStripStatusLabel _emuStatusLabel;

        // Hardware Kontrol UI bileşenleri
        private ComboBox _chipTypeCombo;
        private Label _progStateLabel;
        private Button _btnProgConnect, _btnProgDisconnect;
        private Button _btnProgRead, _btnProgWrite, _btnProgErase, _btnProgVerify;
        private ProgressBar _progProgressBar;
        private RichTextBox _progLog;
        private DataGridView _dtcGrid;
        private HondaTuner.Hardware.OBD.IObdConnection _obdConn;
        private ComboBox _dtcPortCombo;

        public MainForm()
        {
            Text = "HondaTuner";
            Size = new Size(1100, 780);
            MinimumSize = new Size(860, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = SystemIcons.Application;
            BackColor = BgDark;

            // V2 JSON profiles loading
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(exeDir, "Database");
            if (!Directory.Exists(dbPath))
            {
                dbPath = Path.Combine(exeDir, "..", "..", "..", "Database");
            }
            _loadedProfiles = EcuDatabaseManager.LoadProfilesFromDirectory(dbPath);

            _datalogMgr = new DatalogManager();
            _datalogMgr.DataReceived += OnTelemetryDataReceived;

            // Resolve AutoTune Closed-Loop Engine
            _autoTuneEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.AutoTune.IAutoTuneEngine>();
            if (_autoTuneEngine != null)
            {
                _autoTuneEngine.OnDomainEvent += OnAutoTuneDomainEvent;
            }

            // Resolve RTP Engine
            _rtpEngine = Core.Container.ServiceContainer.Resolve<HondaTuner.Core.Rtp.IRtpCalibrationEngine>();
            if (_rtpEngine != null)
            {
                _rtpEngine.OnRtpDomainEvent += OnRtpDomainEvent;
            }

            // ── Donanım Kontrol başlat ───────────────────────────
            _programmer = new HondaTuner.Hardware.EEPROM.Ch341aProgrammer();
            _programmer.StateChanged += OnProgrammerStateChanged;
            _programmer.ProgressChanged += (s, pct) =>
            {
                if (InvokeRequired) BeginInvoke((Action)(() => { if (_progProgressBar != null) _progProgressBar.Value = Math.Min(100, Math.Max(0, pct)); }));
                else if (_progProgressBar != null) _progProgressBar.Value = Math.Min(100, Math.Max(0, pct));
            };
            _programmer.OperationCompleted += (s, msg) =>
            {
                if (InvokeRequired) BeginInvoke((Action)(() => AppendProgLog("✅ " + msg)));
                else AppendProgLog("✅ " + msg);
            };
            _dtcManager = new HondaTuner.Hardware.OBD.DtcManager();
            _ostrich = new HondaTuner.Hardware.Emulator.OstrichEmulator();
            _ostrich.StateChanged += OnOstrichStateChanged;

            BuildMenu();
            BuildHeader();
            BuildStatusBar();
            BuildVtecPanel();
            BuildTabs();            // FILL — EN SON

            SetStatus("ROM yüklenmedi.  Dosya → Aç ile başlayın.");
            UpdateProfileUI();
            UpdateAssistantDefaults();
        }

        // ── Gradient Header ──────────────────────────────────────

        private void BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 72 };
            header.Paint += (s, e) => PaintHeader(e.Graphics, header);

            var hLogo = new Label
            {
                Text = "H",
                Font = new Font("Arial", 28f, FontStyle.Bold),
                ForeColor = AccentRed,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 22),
            };

            var titleLabel = new Label
            {
                Text = "HondaTuner",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TextPrimary,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(76, 20),
            };

            var subLabel = new Label
            {
                Text = "OBD1 ROM Editor  |  pgmfi.org verified offsets",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(78, 50),
            };

            _headerVehicleLabel = new Label
            {
                Text = "Araç seçilmedi",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = AccentBlue,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(320, 32),
            };

            var btnVehicle = new Button
            {
                Text = "🚗  Araç Seç",
                Size = new Size(130, 32),
                Location = new Point(header.Width - 160, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
            };
            btnVehicle.FlatAppearance.BorderColor = AccentRed;
            btnVehicle.FlatAppearance.BorderSize = 1;
            btnVehicle.Click += OnSelectVehicle;

            header.Controls.Add(hLogo);
            header.Controls.Add(titleLabel);
            header.Controls.Add(subLabel);
            header.Controls.Add(_headerVehicleLabel);
            header.Controls.Add(btnVehicle);
            Controls.Add(header);
        }

        private void PaintHeader(Graphics g, Panel p)
        {
            using var brush = new LinearGradientBrush(
                p.ClientRectangle,
                Color.FromArgb(26, 26, 46), Color.FromArgb(15, 52, 96),
                LinearGradientMode.Horizontal);
            g.FillRectangle(brush, p.ClientRectangle);

            var fadeRect = new Rectangle(p.Width - 200, 0, 200, p.Height);
            using var fade = new LinearGradientBrush(fadeRect,
                Color.Transparent, Color.FromArgb(60, 13, 17, 23),
                LinearGradientMode.Horizontal);
            g.FillRectangle(fade, fadeRect);

            using var pen = new Pen(AccentRed, 2);
            g.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        // ── Menü ─────────────────────────────────────────────────

        private void BuildMenu()
        {
            var menu = new MenuStrip
            {
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = TextPrimary,
                Renderer = new DarkMenuRenderer(),
            };

            var fileMenu = new ToolStripMenuItem("Dosya") { ForeColor = TextPrimary };
            fileMenu.DropDownItems.Add(MenuItem("Aç…", Keys.Control | Keys.O, OnOpen));
            fileMenu.DropDownItems.Add(MenuItem("Kaydet", Keys.Control | Keys.S, OnSave));
            fileMenu.DropDownItems.Add(MenuItem("Farklı Kaydet…", Keys.None, OnSaveAs));
            fileMenu.DropDownItems.Add(MenuItem("Geri Al (Undo)", Keys.Control | Keys.Z, OnUndoEdit));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(MenuItem("Çıkış", Keys.Alt | Keys.F4, (s, e) => Close()));

            var toolMenu = new ToolStripMenuItem("Araçlar") { ForeColor = TextPrimary };
            toolMenu.DropDownItems.Add(MenuItem("Araç / ECU Seç…", Keys.F2, OnSelectVehicle));
            toolMenu.DropDownItems.Add(new ToolStripSeparator());
            toolMenu.DropDownItems.Add(MenuItem("Tuning Asistanı: Basemap Uygula", Keys.Control | Keys.B, OnApplyAssistantBasemap));
            toolMenu.DropDownItems.Add(MenuItem("Wideband AFR Düzeltmesi", Keys.Control | Keys.W, OnApplyWidebandCorrection));
            toolMenu.DropDownItems.Add(new ToolStripSeparator());
            toolMenu.DropDownItems.Add(MenuItem("Checksum Doğrula", Keys.F5, OnVerifyChecksum));
            toolMenu.DropDownItems.Add(MenuItem("Stock'a Döndür", Keys.None, OnResetToStock));
            toolMenu.DropDownItems.Add(new ToolStripSeparator());

            var profileMenu = new ToolStripMenuItem("ECU Profili Seç") { ForeColor = TextPrimary };
            var listToUse = (_loadedProfiles != null && _loadedProfiles.Count > 0) ? _loadedProfiles : new List<EcuProfile>(EcuProfiles.All);
            foreach (var profile in listToUse)
            {
                var p = profile;
                var item = new ToolStripMenuItem(p.Name) { ForeColor = TextPrimary, Checked = p == _activeProfile };
                item.Click += (s, e) => ApplyProfile(p, (ToolStripMenuItem)s);
                profileMenu.DropDownItems.Add(item);
            }
            toolMenu.DropDownItems.Add(profileMenu);

            menu.Items.Add(fileMenu);
            menu.Items.Add(toolMenu);
            Controls.Add(menu);
            MainMenuStrip = menu;
        }

        private ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text) { ForeColor = TextPrimary, ShortcutKeys = shortcut };
            item.Click += handler;
            return item;
        }

        // ── Sekmeler ─────────────────────────────────────────────

        private void BuildTabs()
        {
            // ───────────────────────────────────────────────
            // 1. ÖZEL SEKME BARLARI
            // ───────────────────────────────────────────────
            var tabLabels = new[]
            {
                "⛽ Yakıt",
                "⚡ Ateşleme",
                "🧠 Tuning Asistanı",
                "🔍 Diff",
                "📊 Telemetri",
                "🔩 3D Parça",
                "🚀 AutoTune",
                "✏️ Proje & Pinout",
                "🔍 Analiz & Decompiler",
                "🚀 Advanced Fuel",
                "⚡ Advanced Ignition",
                "🏁 VTEC & Boost",
                "🛡️ Engine Protection",
                "📶 Diagnostics & A2L",
                "📊 Dyno, Logs & Branching",
                "🔌 Donanım Kontrol",
            };

            _tabBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(18, 22, 30),
            };
            _tabBar.Paint += (s, e) =>
            {
                using var pen = new Pen(AccentRed, 2);
                e.Graphics.DrawLine(pen, 0, _tabBar.Height - 1,
                    _tabBar.Width, _tabBar.Height - 1);
            };

            _tabButtons = new Button[tabLabels.Length];

            for (int i = 0; i < tabLabels.Length; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Text = tabLabels[i],
                    Height = 38,
                    Width = 148,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(22, 27, 34),
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 9f),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleCenter,
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 38, 45);
                btn.Click += (s, e) => SelectTab(idx);
                _tabButtons[i] = btn;
                _tabBar.Controls.Add(btn);
            }
            RelayoutTabButtons();
            _tabBar.Resize += (s, e) => RelayoutTabButtons();

            // ───────────────────────────────────────────────
            // 2. İÇERİK ALANI
            // ───────────────────────────────────────────────
            _contentArea = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };
            _tabPages = new Panel[tabLabels.Length];

            for (int i = 0; i < tabLabels.Length; i++)
            {
                _tabPages[i] = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = BgDark,
                    Visible = false,
                };
                _contentArea.Controls.Add(_tabPages[i]);
            }

            // ───────────────────────────────────────────────
            // 3. SAYFA İÇERİKLERİ
            // ───────────────────────────────────────────────

            // -- [0] Yakıt Haritası
            _fuelChart3D = new SurfaceChart3D
            {
                Dock = DockStyle.Right,
                Width = 260,
                MinimumSize = new Size(180, 0)
            };
            _fuelGrid = new MapGridControl { Dock = DockStyle.Fill };
            _fuelGrid.LinkedChart3D = _fuelChart3D;
            _fuelGrid.DataChanged += (s, e) => MarkDirty();
            _tabPages[0].Controls.Add(_fuelGrid);
            _tabPages[0].Controls.Add(_fuelChart3D);

            // -- [1] Ateşleme Haritası
            _ignChart3D = new SurfaceChart3D
            {
                Dock = DockStyle.Right,
                Width = 260,
                MinimumSize = new Size(180, 0)
            };
            _ignGrid = new MapGridControl { Dock = DockStyle.Fill };
            _ignGrid.LinkedChart3D = _ignChart3D;
            _ignGrid.DataChanged += (s, e) => MarkDirty();
            _tabPages[1].Controls.Add(_ignGrid);
            _tabPages[1].Controls.Add(_ignChart3D);

            // -- [2] Tuning Asistanı
            BuildAssistantPage(_tabPages[2]);

            // -- [3] Diff
            _diffView = new DiffView { Dock = DockStyle.Fill };
            _tabPages[3].Controls.Add(_diffView);
            _diffPage = _tabPages[3];

            // -- [4] Telemetri
            BuildTelemetryPage(_tabPages[4]);

            // -- [5] 3D Parça Görüntüleyici
            BuildPartPage(_tabPages[5]);

            // -- [6] AutoTune
            BuildAutoTunePage(_tabPages[6]);

            // -- [7] Proje & Pinout
            var romService = Core.Container.ServiceContainer.Resolve<Core.Interfaces.IRomService>();
            _metadataControl = new MetadataControl();
            _metadataControl.BindRomService(romService);
            _tabPages[7].Controls.Add(_metadataControl);

            // -- [8] Analiz & Decompiler
            _reverseControl = new ReverseControl();
            _reverseControl.BindRomService(romService);
            _tabPages[8].Controls.Add(_reverseControl);

            // -- [9] Advanced Fuel
            _advancedFuelControl = new AdvancedFuelControl();
            _tabPages[9].Controls.Add(_advancedFuelControl);

            // -- [10] Advanced Ignition
            _advancedIgnitionControl = new AdvancedIgnitionControl();
            _tabPages[10].Controls.Add(_advancedIgnitionControl);

            // -- [11] VTEC & Boost Control
            _vtecBoostControl = new VtecBoostControl();
            _tabPages[11].Controls.Add(_vtecBoostControl);

            // -- [12] Engine Protection & Thermal Management
            _engineProtectionControl = new EngineProtectionControl();
            _tabPages[12].Controls.Add(_engineProtectionControl);

            // -- [13] Diagnostics & A2L
            _diagnosticsControl = new DiagnosticsControl();
            _tabPages[13].Controls.Add(_diagnosticsControl);

            // -- [14] Dyno, Logs & Branching
            _dynoLogsControl = new DynoLogsControl();
            _tabPages[14].Controls.Add(_dynoLogsControl);

            // -- [15] Donanım Kontrol
            BuildHardwareControlPage(_tabPages[15]);

            // ───────────────────────────────────────────────
            // 4. FORMA EKLE (sıra önemli)
            // ───────────────────────────────────────────────
            Controls.Add(_contentArea);   // Fill — önce
            Controls.Add(_tabBar);        // Top  — sonra (Fill'in üstüne yerleşir)
            _contentArea.BringToFront();

            SelectTab(0);
        }

        // ── Donanım Kontrol Sayfası ──────────────────────────────

        private void BuildHardwareControlPage(Panel tab)
        {
            tab.Padding = new Padding(10);
            tab.BackColor = BgDark;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = BgDark
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));

            // ── Sol Panel: CH341A EEPROM Programlayıcı ──────────
            var leftPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = BgPanel,
                Padding = new Padding(14, 10, 14, 10)
            };

            var progTitle = new Label
            {
                Text = "CH341A EEPROM Programlayıcı",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentBlue,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            leftPanel.Controls.Add(progTitle);

            // Çip Tipi
            var chipPanel = new Panel { Width = 380, Height = 30, Margin = new Padding(0, 0, 0, 6) };
            var chipLabel = new Label
            {
                Text = "Çip Tipi:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(0, 6)
            };
            _chipTypeCombo = new ComboBox
            {
                Location = new Point(70, 2),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f)
            };
            _chipTypeCombo.Items.AddRange(new object[] { "SST27SF512", "27C256", "29C256", "AM29F010" });
            _chipTypeCombo.SelectedIndex = 0;
            _chipTypeCombo.SelectedIndexChanged += (s, e) =>
            {
                if (_programmer != null && _chipTypeCombo != null)
                    _programmer.ChipType = _chipTypeCombo.SelectedItem?.ToString() ?? "SST27SF512";
            };
            chipPanel.Controls.Add(chipLabel);
            chipPanel.Controls.Add(_chipTypeCombo);
            leftPanel.Controls.Add(chipPanel);

            // Durum etiketi
            _progStateLabel = new Label
            {
                Text = "● Bağlı Değil",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            leftPanel.Controls.Add(_progStateLabel);

            // Bağlan / Kes butonları
            var connBtnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
            _btnProgConnect = MakeHwButton("Bağlan", 90, Color.FromArgb(40, 167, 69));
            _btnProgDisconnect = MakeHwButton("Bağlantıyı Kes", 130, Color.FromArgb(185, 28, 28));
            _btnProgDisconnect.Enabled = false;
            _btnProgConnect.Margin = new Padding(0, 0, 8, 0);
            _btnProgConnect.Click += OnProgConnect;
            _btnProgDisconnect.Click += OnProgDisconnect;
            connBtnPanel.Controls.Add(_btnProgConnect);
            connBtnPanel.Controls.Add(_btnProgDisconnect);
            leftPanel.Controls.Add(connBtnPanel);

            // İşlem butonları
            var opBtnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 8), WrapContents = true, MaximumSize = new Size(390, 200) };
            _btnProgRead = MakeHwButton("Çipten Oku", 115, Color.FromArgb(13, 110, 190));
            _btnProgWrite = MakeHwButton("Çipe Yaz", 100, Color.FromArgb(185, 28, 28));
            _btnProgErase = MakeHwButton("Çipi Sil", 90, Color.FromArgb(80, 80, 90));
            _btnProgVerify = MakeHwButton("Çipi Doğrula", 115, Color.FromArgb(30, 120, 60));
            foreach (var btn in new[] { _btnProgRead, _btnProgWrite, _btnProgErase, _btnProgVerify })
            {
                btn.Enabled = false;
                btn.Margin = new Padding(0, 0, 6, 6);
            }
            _btnProgRead.Click += OnProgReadChip;
            _btnProgWrite.Click += OnProgWriteChip;
            _btnProgErase.Click += OnProgEraseChip;
            _btnProgVerify.Click += OnProgVerifyChip;
            opBtnPanel.Controls.Add(_btnProgRead);
            opBtnPanel.Controls.Add(_btnProgWrite);
            opBtnPanel.Controls.Add(_btnProgErase);
            opBtnPanel.Controls.Add(_btnProgVerify);
            leftPanel.Controls.Add(opBtnPanel);

            // Progress Bar
            var progLabelBar = new Label { Text = "İlerleme:", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            leftPanel.Controls.Add(progLabelBar);
            _progProgressBar = new ProgressBar
            {
                Width = 370,
                Height = 18,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0, 0, 0, 8)
            };
            leftPanel.Controls.Add(_progProgressBar);

            // Log Konsolu
            var logLabel = new Label { Text = "İşlem Kaydı:", Font = new Font("Segoe UI", 8.5f), ForeColor = TextMuted, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            leftPanel.Controls.Add(logLabel);
            _progLog = new RichTextBox
            {
                Width = 370,
                Height = 220,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 15, 20),
                ForeColor = VtecGreen,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 0)
            };
            leftPanel.Controls.Add(_progLog);
            AppendProgLog("CH341A Programlayıcı hazır. 'Bağlan' butonuna basın.");

            // ── Sağ Panel: Canlı OBD1 DTC ───────────────────────
            var rightPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = BgPanel,
                Padding = new Padding(14, 10, 14, 10),
                Margin = new Padding(8, 0, 0, 0)
            };

            var dtcTitle = new Label
            {
                Text = "Canlı OBD1 Arıza Kodları (DTC)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentRed,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            rightPanel.Controls.Add(dtcTitle);

            var dtcInfo = new Label
            {
                Text = "OBD1 seri portu seçin, bağlantı kurun, sonra kodu okuyun.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            rightPanel.Controls.Add(dtcInfo);

            // OBD Port seçimi
            var obdPortRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
            var obdPortLabel = new Label { Text = "Port:", Font = new Font("Segoe UI", 9f), ForeColor = TextMuted, AutoSize = true, Margin = new Padding(0, 5, 6, 0) };
            _dtcPortCombo = new ComboBox
            {
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f)
            };
            foreach (var p in System.IO.Ports.SerialPort.GetPortNames())
                _dtcPortCombo.Items.Add(p);
            if (_dtcPortCombo.Items.Count > 0) _dtcPortCombo.SelectedIndex = 0;
            var btnObdConnect = MakeButton("Bağlan", new Point(0, 0), 72, VtecGreen);
            btnObdConnect.Margin = new Padding(6, 0, 6, 0);
            btnObdConnect.Click += (s, ex) =>
            {
                if (_dtcPortCombo.SelectedItem == null)
                {
                    MessageBox.Show("Lütfen bir seri port seçin.", "Port Seçin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    _obdConn?.Disconnect();
                    var conn = new HondaTuner.Hardware.OBD.RealObd1Connection();
                    conn.Open(_dtcPortCombo.SelectedItem.ToString(), EcuConstants.Obd1BaudRate);
                    _obdConn = conn;
                    SetStatus($"OBD Bağlantısı: {_dtcPortCombo.SelectedItem} açıldı.");
                }
                catch (Exception exConn)
                {
                    MessageBox.Show($"OBD bağlantı hatası:\n{exConn.Message}", "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            var btnObdRefreshPorts = MakeButton("Yenile", new Point(0, 0), 54, AccentBlue);
            btnObdRefreshPorts.Margin = new Padding(0, 0, 0, 0);
            btnObdRefreshPorts.Click += (s, ex) =>
            {
                _dtcPortCombo.Items.Clear();
                foreach (var p in System.IO.Ports.SerialPort.GetPortNames())
                    _dtcPortCombo.Items.Add(p);
                if (_dtcPortCombo.Items.Count > 0) _dtcPortCombo.SelectedIndex = 0;
            };
            obdPortRow.Controls.Add(obdPortLabel);
            obdPortRow.Controls.Add(_dtcPortCombo);
            obdPortRow.Controls.Add(btnObdConnect);
            obdPortRow.Controls.Add(btnObdRefreshPorts);
            rightPanel.Controls.Add(obdPortRow);

            var dtcBtnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
            var btnReadDtc = MakeButton("Arıza Kodlarını Oku", new Point(0, 0), 175, AccentRed);
            var btnClearDtc = MakeButton("Kodları Temizle", new Point(0, 0), 145, Color.FromArgb(255, 200, 0));
            btnReadDtc.Margin = new Padding(0, 0, 8, 0);
            btnReadDtc.Click += OnReadDtcsLive;
            btnClearDtc.Click += OnClearDtcsLive;
            dtcBtnPanel.Controls.Add(btnReadDtc);
            dtcBtnPanel.Controls.Add(btnClearDtc);
            rightPanel.Controls.Add(dtcBtnPanel);

            // DTC DataGridView
            _dtcGrid = new DataGridView
            {
                Width = 380,
                Height = 420,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.FromArgb(12, 15, 20),
                GridColor = Border,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(22, 27, 34),
                    ForeColor = TextPrimary,
                    SelectionBackColor = Color.FromArgb(40, 88, 166, 255),
                    SelectionForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = BgCard,
                    ForeColor = AccentBlue,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    SelectionBackColor = BgCard
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 28,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0)
            };
            _dtcGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Kod", HeaderText = "Kod", Width = 80 });
            _dtcGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Aciklama", HeaderText = "Açıklama", Width = 280, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            rightPanel.Controls.Add(_dtcGrid);

            // Assemble
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            tab.Controls.Add(mainLayout);
        }

        // ── Hardware Event Handlers ──────────────────────────────

        private void OnProgrammerStateChanged(object sender, HondaTuner.Core.Interfaces.ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke((Action)(() => OnProgrammerStateChanged(sender, e))); return; }

            // Durum etiketi rengi
            switch (e.NewState)
            {
                case HondaTuner.Core.Interfaces.ConnectionState.Connected:
                    _progStateLabel.Text = "● Bağlandı";
                    _progStateLabel.ForeColor = VtecGreen;
                    _progStatusLabel.Text = "🔌 PROG: ONLINE";
                    _progStatusLabel.ForeColor = VtecGreen;
                    _btnProgConnect.Enabled = false;
                    _btnProgDisconnect.Enabled = true;
                    foreach (var b in new[] { _btnProgRead, _btnProgWrite, _btnProgErase, _btnProgVerify })
                        b.Enabled = true;
                    break;
                case HondaTuner.Core.Interfaces.ConnectionState.Connecting:
                    _progStateLabel.Text = "● Bağlanıyor...";
                    _progStateLabel.ForeColor = AccentBlue;
                    _progStatusLabel.Text = "🔌 PROG: BAĞLANIYOR";
                    _progStatusLabel.ForeColor = AccentBlue;
                    break;
                case HondaTuner.Core.Interfaces.ConnectionState.Error:
                    _progStateLabel.Text = "● Hata";
                    _progStateLabel.ForeColor = AccentRed;
                    _progStatusLabel.Text = "🔌 PROG: HATA";
                    _progStatusLabel.ForeColor = AccentRed;
                    _btnProgConnect.Enabled = true;
                    _btnProgDisconnect.Enabled = false;
                    foreach (var b in new[] { _btnProgRead, _btnProgWrite, _btnProgErase, _btnProgVerify })
                        b.Enabled = false;
                    break;
                default:  // Disconnected / TimedOut
                    _progStateLabel.Text = "● Bağlı Değil";
                    _progStateLabel.ForeColor = TextMuted;
                    _progStatusLabel.Text = "🔌 PROG: OFFLINE";
                    _progStatusLabel.ForeColor = TextMuted;
                    _btnProgConnect.Enabled = true;
                    _btnProgDisconnect.Enabled = false;
                    foreach (var b in new[] { _btnProgRead, _btnProgWrite, _btnProgErase, _btnProgVerify })
                        b.Enabled = false;
                    break;
            }
            AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Durum: {e.NewState} — {e.Message}");
        }

        private void OnOstrichStateChanged(object sender, HondaTuner.Core.Interfaces.ConnectionStateChangedEventArgs e)
        {
            if (InvokeRequired) { BeginInvoke((Action)(() => OnOstrichStateChanged(sender, e))); return; }

            switch (e.NewState)
            {
                case HondaTuner.Core.Interfaces.ConnectionState.Connected:
                    _emuStatusLabel.Text = "🎮 EMU: ONLINE";
                    _emuStatusLabel.ForeColor = VtecGreen;
                    break;
                case HondaTuner.Core.Interfaces.ConnectionState.Connecting:
                    _emuStatusLabel.Text = "🎮 EMU: BAĞLANIYOR";
                    _emuStatusLabel.ForeColor = AccentBlue;
                    break;
                case HondaTuner.Core.Interfaces.ConnectionState.Error:
                    _emuStatusLabel.Text = "🎮 EMU: HATA";
                    _emuStatusLabel.ForeColor = AccentRed;
                    break;
                default:
                    _emuStatusLabel.Text = "🎮 EMU: OFFLINE";
                    _emuStatusLabel.ForeColor = TextMuted;
                    break;
            }
        }

        private void AppendProgLog(string message)
        {
            if (_progLog == null) return;
            if (InvokeRequired) { BeginInvoke((Action)(() => AppendProgLog(message))); return; }
            _progLog.AppendText(message + Environment.NewLine);
            _progLog.ScrollToCaret();
        }

        private void OnProgConnect(object sender, EventArgs e)
        {
            try
            {
                if (_chipTypeCombo != null)
                    _programmer.ChipType = _chipTypeCombo.SelectedItem?.ToString() ?? "SST27SF512";
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Bağlanıyor... (Çip: {_programmer.ChipType})");
                System.Threading.Tasks.Task.Run(() =>
                {
                    try { _programmer.Connect(); }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"CH341A bağlantı hatası:\n{ex.Message}", "Bağlantı Hatası",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgDisconnect(object sender, EventArgs e)
        {
            try
            {
                _programmer.Disconnect();
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Bağlantı kesildi.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı kesme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgReadChip(object sender, EventArgs e)
        {
            try
            {
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Çip okunuyor...");
                _progProgressBar.Value = 0;
                int romLen = _activeProfile != null ? _activeProfile.RomSize : EcuConstants.DefaultRomSize;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        byte[] data = _programmer.ReadChip(romLen);
                        BeginInvoke((Action)(() =>
                        {
                            AppendProgLog($"[{DateTime.Now:HH:mm:ss}] ✅ {data.Length} bayt okundu.");
                            SetStatus($"CH341A: {data.Length} bayt okundu.");
                        }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"Okuma hatası:\n{ex.Message}", "Okuma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Okuma başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgWriteChip(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }
            if (MessageBox.Show("Aktif ROM dosyası çipe yazılacak. Yedek otomatik alınacak. Devam?",
                "Çipe Yaz", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                byte[] romData = _parser.GetRomBuffer();
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Çipe yazılıyor ({romData.Length} bayt)...");
                _progProgressBar.Value = 0;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _programmer.WriteChip(romData);
                        BeginInvoke((Action)(() => { AppendProgLog($"[{DateTime.Now:HH:mm:ss}] ✅ Yazma tamamlandı."); SetStatus("CH341A: Yazma tamamlandı."); }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"Yazma hatası:\n{ex.Message}", "Yazma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazma başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgEraseChip(object sender, EventArgs e)
        {
            if (MessageBox.Show("Çip tamamen silinecek! Bu işlem geri alınamaz. Devam?",
                "Çipi Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Çip siliniyor...");
                _progProgressBar.Value = 0;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _programmer.EraseChip();
                        BeginInvoke((Action)(() => { AppendProgLog($"[{DateTime.Now:HH:mm:ss}] ✅ Silme tamamlandı."); SetStatus("CH341A: Silme tamamlandı."); }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"Silme hatası:\n{ex.Message}", "Silme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Silme başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnProgVerifyChip(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }
            try
            {
                byte[] expected = _parser.GetRomBuffer();
                AppendProgLog($"[{DateTime.Now:HH:mm:ss}] Çip doğrulanıyor ({expected.Length} bayt karşılaştırılıyor)...");
                _progProgressBar.Value = 0;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        bool ok = _programmer.VerifyChip(expected);
                        BeginInvoke((Action)(() =>
                        {
                            string msg = ok ? "✅ Doğrulama başarılı — çip ROM ile eşleşiyor." : "❌ Doğrulama BAŞARISIZ — çip verisi farklı!";
                            AppendProgLog($"[{DateTime.Now:HH:mm:ss}] {msg}");
                            SetStatus($"CH341A Doğrulama: {(ok ? "BAŞARILI" : "BAŞARISIZ")}");
                        }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"Doğrulama hatası:\n{ex.Message}", "Doğrulama Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Doğrulama başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReadDtcsLive(object sender, EventArgs e)
        {
            if (_obdConn == null)
            {
                MessageBox.Show(
                    "OBD1 bağlantısı kurulmamış.\nLütfen sağ paneldeki Port seçiciden 'Bağlan' butonuna basın.",
                    "OBD Bağlantısı Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                _dtcGrid.Rows.Clear();
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var dtcList = _dtcManager.ReadDtcsLive(_obdConn);
                        BeginInvoke((Action)(() =>
                        {
                            _dtcGrid.Rows.Clear();
                            if (dtcList == null || dtcList.Count == 0)
                            {
                                _dtcGrid.Rows.Add("—", "Arıza kodu bulunamadı.");
                            }
                            else
                            {
                                foreach (var dtc in dtcList)
                                    _dtcGrid.Rows.Add($"P{dtc.Code:D4}", dtc.Description);
                            }
                            SetStatus($"DTC: {dtcList?.Count ?? 0} arıza kodu okundu.");
                        }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"DTC okuma hatası:\n{ex.Message}", "DTC Okuma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DTC okuma başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnClearDtcsLive(object sender, EventArgs e)
        {
            if (_obdConn == null)
            {
                MessageBox.Show(
                    "OBD1 bağlantısı kurulmamış.\nLütfen sağ paneldeki Port seçiciden 'Bağlan' butonuna basın.",
                    "OBD Bağlantısı Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("ECU'daki tüm arıza kodları temizlenecek. Devam?",
                "Arıza Kodlarını Temizle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _dtcManager.ClearDtcsLive(_obdConn);
                        BeginInvoke((Action)(() =>
                        {
                            _dtcGrid.Rows.Clear();
                            _dtcGrid.Rows.Add("—", "Arıza kodları temizlendi.");
                            SetStatus("DTC: Arıza kodları ECU'dan temizlendi.");
                        }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                            MessageBox.Show($"DTC temizleme hatası:\n{ex.Message}", "DTC Temizleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"DTC temizleme başlatma hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Bölüm Sonu ───────────────────────────────────────────

        private void BuildPartPage(Panel tab)
        {
            tab.Padding = new Padding(0);

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = BgDark
            };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f)); // Row 0: Header
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Row 1: Left Menu + 3D Canvas
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f)); // Row 2: Note & Hint (32 + 18)

            // Header şeridi
            var hdr = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 34,
                BackColor = Color.FromArgb(22, 28, 40),
                Margin = new Padding(0)
            };
            hdr.Paint += (s, e) =>
            {
                var p = (Panel)s;
                if (p.Width <= 0 || p.Height <= 0) return;
                using var brush = new LinearGradientBrush(p.ClientRectangle,
                    Color.FromArgb(22, 28, 40), Color.FromArgb(16, 20, 30), 0f);
                e.Graphics.FillRectangle(brush, p.ClientRectangle);
                using var pen = new Pen(Color.FromArgb(40, AccentBlue), 1);
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            };

            var btnFull = new Button
            {
                Text = "⛶",
                Size = new Size(24, 24),
                Location = new Point(6, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 48, 60),
                ForeColor = AccentBlue,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnFull.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(btnFull, "Tam Ekran");

            var btnReset = new Button
            {
                Text = "↺",
                Size = new Size(24, 24),
                Location = new Point(36, 5),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 48, 60),
                ForeColor = AccentBlue,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(btnReset, "Kamerayı Sıfırla");

            hdr.Controls.Add(btnFull);
            hdr.Controls.Add(btnReset);

            // Alt panel
            var footer = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                BackColor = BgDark,
                Margin = new Padding(0)
            };

            // Not etiketi
            var noteLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextMuted,
                BackColor = BgPanel,
                Font = new Font("Segoe UI", 7.5f),
                Padding = new Padding(8, 0, 0, 0),
                Text = "  " + PartViewer3D.PartNotes[0]
            };

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(100, 110, 125),
                BackColor = BgDark,
                Font = new Font("Segoe UI", 7f),
                Text = "Sol: döndür  |  Sağ: kaydır  |  Tekerlek: zoom"
            };

            footer.Controls.Add(hint);
            footer.Controls.Add(noteLabel);

            // Central layout panel
            var workPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            var sidePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 210,
                BackColor = Color.FromArgb(20, 24, 32),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8, 8, 8, 8),
                Margin = new Padding(0),
                AutoScroll = true
            };

            _partViewer = new PartViewer3D { Dock = DockStyle.Fill, Margin = new Padding(0) };

            workPanel.Controls.Add(_partViewer); // Dock=Fill goes last
            workPanel.Controls.Add(sidePanel);   // Dock=Left goes first

            // Title
            var lblTitle = new Label
            {
                Text = "3D MODEL SEÇİMİ",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = AccentBlue,
                Width = 178,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 8)
            };
            sidePanel.Controls.Add(lblTitle);

            // Button Ecu
            var btnEcu = new Button
            {
                Text = "🧠  ECU Ana Kartı",
                Height = 36,
                Width = 178,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 4)
            };
            btnEcu.FlatAppearance.BorderSize = 0;
            btnEcu.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 38, 45);
            sidePanel.Controls.Add(btnEcu);

            // Sub-parts panel
            var ecuPartsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 178,
                Height = 150,
                Padding = new Padding(12, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 8),
                Visible = true
            };
            sidePanel.Controls.Add(ecuPartsPanel);

            var subPartTypes = new PartViewer3D.PartType[]
            {
                PartViewer3D.PartType.EepromChip,
                PartViewer3D.PartType.Obd1Connector,
                PartViewer3D.PartType.MapSensor,
                PartViewer3D.PartType.Injector,
                PartViewer3D.PartType.Distributor
            };

            var subPartNames = new string[]
            {
                "💾  EEPROM Çip",
                "🔌  OBD1 Konnektör",
                "🌡️  MAP Sensörü",
                "⛽  Enjektör",
                "⚙️  Distribütör"
            };

            var subPartButtons = new Button[5];

            // Separator between categories
            var sep = new Panel
            {
                Width = 178,
                Height = 1,
                BackColor = Color.FromArgb(40, 50, 70),
                Margin = new Padding(0, 4, 0, 8)
            };

            // Button Motor
            var btnMotor = new Button
            {
                Text = "🔩  B16 FWD Motor",
                Height = 36,
                Width = 178,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0),
                Margin = new Padding(0, 0, 0, 4)
            };
            btnMotor.FlatAppearance.BorderSize = 0;
            btnMotor.FlatAppearance.MouseOverBackColor = Color.FromArgb(33, 38, 45);

            Action<PartViewer3D.PartType> updateHighlights = (part) =>
            {
                bool isEcuGroup = part != PartViewer3D.PartType.B16Engine;

                // Dynamic back/selection breadcrumb text
                btnEcu.Text = (part == PartViewer3D.PartType.B16Engine || part != PartViewer3D.PartType.EcuBoard) ? "🧠  ECU (Geri)" : "🧠  ECU Ana Kartı";

                // Style Ecu Board button
                bool isEcuBoard = part == PartViewer3D.PartType.EcuBoard;
                btnEcu.BackColor = isEcuBoard ? Color.FromArgb(35, 45, 60) : Color.FromArgb(22, 27, 34);
                btnEcu.ForeColor = isEcuBoard ? AccentBlue : TextPrimary;

                // Style B16 button
                bool isB16 = part == PartViewer3D.PartType.B16Engine;
                btnMotor.BackColor = isB16 ? Color.FromArgb(35, 45, 60) : Color.FromArgb(22, 27, 34);
                btnMotor.ForeColor = isB16 ? AccentBlue : TextPrimary;

                // Sub-parts details visibility
                ecuPartsPanel.Visible = isEcuGroup;

                // Highlight active sub-parts
                for (int i = 0; i < subPartTypes.Length; i++)
                {
                    bool active = subPartTypes[i] == part;
                    subPartButtons[i].BackColor = active ? Color.FromArgb(30, 36, 46) : Color.Transparent;
                    subPartButtons[i].ForeColor = active ? AccentBlue : TextMuted;
                    subPartButtons[i].Font = new Font("Segoe UI", 8f, active ? FontStyle.Bold : FontStyle.Regular);
                }
            };

            Action<PartViewer3D.PartType> setActivePart = (part) =>
            {
                _partViewer.SetPart(part);
                noteLabel.Text = "  " + PartViewer3D.PartNotes[(int)part];
                updateHighlights(part);
            };

            for (int i = 0; i < subPartNames.Length; i++)
            {
                int subIdx = i;
                var sbtn = new Button
                {
                    Text = subPartNames[i],
                    Height = 28,
                    Width = 152,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 8f),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 0, 0, 0),
                    Margin = new Padding(0, 0, 0, 2)
                };
                sbtn.FlatAppearance.BorderSize = 0;
                sbtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 33, 40);

                var partType = subPartTypes[i];
                sbtn.Click += (s, e) => setActivePart(partType);

                subPartButtons[i] = sbtn;
                ecuPartsPanel.Controls.Add(sbtn);
            }

            btnEcu.Click += (s, e) => setActivePart(PartViewer3D.PartType.EcuBoard);
            btnMotor.Click += (s, e) => setActivePart(PartViewer3D.PartType.B16Engine);

            sidePanel.Controls.Add(sep);
            sidePanel.Controls.Add(btnMotor);

            // Title for modifications
            var lblModTitle = new Label
            {
                Text = "🔧 PROJE PARÇALARI",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = AccentRed,
                Width = 178,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 16, 0, 8)
            };
            sidePanel.Controls.Add(lblModTitle);

            var modsScrollPanel = new Panel
            {
                Width = 178,
                Height = 220,
                AutoScroll = true,
                BackColor = Color.FromArgb(16, 20, 28),
                Padding = new Padding(4),
                Margin = new Padding(0)
            };
            sidePanel.Controls.Add(modsScrollPanel);

            var modsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Width = 152,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(2)
            };
            modsScrollPanel.Controls.Add(modsFlow);

            var modsData = new Dictionary<string, string>
            {
                { "ECU", "Hondata S300, Neptune RTP, Crome Pro, HTS" },
                { "Emme", "Cold Air Intake, Skunk2 Pro, K&N filtre" },
                { "Gaz Kelebeği", "B16/B18 62 mm throttle body" },
                { "Emme Manifoldu", "D16Y8 veya Skunk2" },
                { "Egzoz", "4-2-1 Header, 2.25\" düz hat" },
                { "Egzantrik", "Delta Cam, Bisimoto Stage 1" },
                { "Yakıt", "Walbro 255, büyük enjektör" },
                { "Ateşleme", "NGK Iridium, MSD" },
                { "Volan", "Hafifletilmiş Volan" },
                { "Debriyaj", "Exedy Stage 1" },
                { "Süspansiyon", "BC Racing, Tein, D2" },
                { "Fren", "Integra DC2 veya Civic VTi disk" },
                { "Turbo", "TD04, GT2554R, GT2860" }
            };

            foreach (var kvp in modsData)
            {
                var lblCat = new Label
                {
                    Text = kvp.Key,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = AccentBlue,
                    Width = 146,
                    Height = 16,
                    Margin = new Padding(0, 6, 0, 1)
                };
                var lblDesc = new Label
                {
                    Text = kvp.Value,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                    ForeColor = TextPrimary,
                    Width = 146,
                    Height = 30,
                    Margin = new Padding(0, 0, 0, 4)
                };
                modsFlow.Controls.Add(lblCat);
                modsFlow.Controls.Add(lblDesc);
            }

            // Default selection
            setActivePart(PartViewer3D.PartType.EcuBoard);

            btnReset.Click += (s, e) => _partViewer.ResetCamera();
            btnFull.Click += (s, e) => _partViewer.ShowFullscreen();

            tlp.Controls.Add(hdr, 0, 0);
            tlp.Controls.Add(workPanel, 0, 1);
            tlp.Controls.Add(footer, 0, 2);

            tab.Controls.Add(tlp);
        }


        private void RelayoutTabButtons()
        {
            if (_tabButtons == null || _tabButtons.Length == 0) return;

            int buttonWidth = 142; // Uygun genişlik
            int buttonHeight = 34;
            int padding = 2;

            int cols = Math.Max(1, _tabBar.Width / (buttonWidth + padding));
            int rows = (int)Math.Ceiling((double)_tabButtons.Length / cols);

            _tabBar.Height = rows * (buttonHeight + padding) + padding;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                _tabButtons[i].Location = new Point(c * (buttonWidth + padding) + padding, r * (buttonHeight + padding) + padding);
                _tabButtons[i].Width = buttonWidth;
                _tabButtons[i].Height = buttonHeight;
            }
        }

        private void SelectTab(int index)
        {
            _activeTabIndex = index;
            for (int i = 0; i < _tabPages.Length; i++)
            {
                _tabPages[i].Visible = (i == index);
                SetTabButtonActive(_tabButtons[i], i == index);
            }
            if (index == 3 && _parser.IsLoaded)
                RefreshDiff();
        }

        private void SetTabButtonActive(Button btn, bool active)
        {
            if (active)
            {
                btn.BackColor = BgDark;
                btn.ForeColor = TextPrimary;
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                btn.FlatAppearance.BorderSize = 0;
                // Alt kenarda kırmızı vurgu çizgisi
                btn.Paint += DrawActiveTabLine;
            }
            else
            {
                btn.BackColor = Color.FromArgb(22, 27, 34);
                btn.ForeColor = TextMuted;
                btn.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
                btn.Paint -= DrawActiveTabLine;
            }
            btn.Refresh();
        }

        private void DrawActiveTabLine(object s, PaintEventArgs e)
        {
            using var pen = new Pen(AccentRed, 3);
            var b = (Button)s;
            e.Graphics.DrawLine(pen, 0, b.Height - 2, b.Width, b.Height - 2);
        }

        private void BuildTelemetryPage(Panel tab)
        {
            tab.Padding = new Padding(0);

            // Bağlantı paneli
            var connPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(22, 27, 34),
            };
            connPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, connPanel.Height - 1, connPanel.Width, connPanel.Height - 1);
            };

            var portLabel = MakeLabel("COM Port:", new Font("Segoe UI", 9f), TextMuted, new Point(12, 17));

            _comPortCombo = new ComboBox
            {
                Location = new Point(80, 13),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
            };
            // COM portları listele
            foreach (var p in SerialPort.GetPortNames())
                _comPortCombo.Items.Add(p);
            if (_comPortCombo.Items.Count > 0)
                _comPortCombo.SelectedIndex = 0;

            _btnConnect = MakeButton("🔌 Canlı Bağlan", new Point(190, 11), 130, AccentBlue);
            _btnSimulate = MakeButton("🎮 Simülasyon Başlat", new Point(328, 11), 160, VtecGreen);
            _btnDisconnect = MakeButton("⏹ Bağlantıyı Kes", new Point(496, 11), 140, AccentRed);
            _btnDisconnect.Enabled = false;

            _btnConnect.Click += OnDatalogConnect;
            _btnSimulate.Click += OnDatalogSimulate;
            _btnDisconnect.Click += OnDatalogDisconnect;

            connPanel.Controls.AddRange(new Control[]
            { portLabel, _comPortCombo, _btnConnect, _btnSimulate, _btnDisconnect });

            _telemetryDash = new TelemetryDashboard { Dock = DockStyle.Fill };

            tab.Controls.Add(_telemetryDash);
            tab.Controls.Add(connPanel);
        }

        private void BuildAssistantPage(Panel tab)
        {
            tab.Padding = new Padding(12);

            var left = new Panel
            {
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = BgPanel,
                Padding = new Padding(14),
                AutoScroll = true
            };

            var right = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(12),
            };

            int y = 10;
            AddAssistantLabel(left, "Gorunum Secenegi", y);
            y += 20;

            var cmbView = new ComboBox
            {
                Location = new Point(14, y),
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            cmbView.Items.AddRange(new object[] { "Asistan Kilavuzu", "Gelismis Ayarlar", "Sihirbazlar", "Gelismis Patch Merkezi" });
            cmbView.SelectedIndex = 0;
            left.Controls.Add(cmbView);
            y += 36;

            AddAssistantLabel(left, "Basemap hedefi", y);
            y += 20;

            _goalCombo = new ComboBox
            {
                Location = new Point(14, y),
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
            };
            _goalCombo.Items.AddRange(new object[]
            {
                TuningAssistant.DescribeGoal(TuningGoal.IesVtecStreet),
                TuningAssistant.DescribeGoal(TuningGoal.NaturallyAspirated),
                TuningAssistant.DescribeGoal(TuningGoal.TurboSafeBase),
                TuningAssistant.DescribeGoal(TuningGoal.Economy),
                TuningAssistant.DescribeGoal(TuningGoal.StockStreet),
            });
            _goalCombo.SelectedIndex = 0;
            left.Controls.Add(_goalCombo);
            y += 36;

            _injectorCcSpinner = AddAssistantSpinner(left, "Enjektör cc", 180, 1000, 10, 240, ref y);
            _mapSensorSpinner = AddAssistantSpinner(left, "MAP sensörü bar", 1, 4, 1, 1, ref y);
            _targetAfrSpinner = AddAssistantSpinner(left, "Power AFR hedefi", 10, 16, (decimal)0.1, (decimal)12.8, ref y, 1);

            var btnApply = MakeButton("Basemap Uygula", new Point(14, y + 4), 150, AccentBlue);
            var btnPreview = MakeButton("Notları Yenile", new Point(174, y + 4), 150, VtecGreen);
            var btnStage1 = MakeButton("⚡ Stage 1 Map", new Point(14, y + 44), 310, AccentRed);
            btnApply.Click += OnApplyAssistantBasemap;
            btnPreview.Click += (s, e) => UpdateAssistantDefaults();
            btnStage1.Click += OnApplyStage1Basemap;
            new ToolTip().SetToolTip(btnStage1, "Stage 1 Basemap: VTEC RPM + Rev Limit + Hız + Yakıt/Ateşleme haritasını ROM'a yazar");
            left.Controls.Add(btnApply);
            left.Controls.Add(btnPreview);
            left.Controls.Add(btnStage1);
            y += 84;

            AddAssistantLabel(left, "Wideband yakıt düzeltme", y);
            y += 24;
            _measuredAfrSpinner = AddAssistantSpinner(left, "Ölçülen AFR", 9, 19, (decimal)0.1, (decimal)14.0, ref y, 1);
            _widebandRpmSpinner = AddAssistantSpinner(left, "RPM", 500, 9500, 100, 4500, ref y);
            _widebandLoadSpinner = AddAssistantSpinner(left, "Load kPa", 20, 250, 5, 100, ref y);
            _widebandRadiusSpinner = AddAssistantSpinner(left, "Etki alanı", 0, 4, 1, 1, ref y);

            var btnAfr = MakeButton("AFR Düzelt", new Point(14, y + 4), 150, AccentRed);
            btnAfr.Click += OnApplyWidebandCorrection;
            left.Controls.Add(btnAfr);

            // Content Panels (Dock = Fill)
            var panelNotes = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Visible = true
            };

            var panelAdvanced = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Padding = new Padding(12),
                Visible = false
            };

            var panelWizards = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Padding = new Padding(12),
                Visible = false
            };

            var panelPatches = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                Padding = new Padding(12),
                Visible = false
            };

            // tab1: notes textbox
            _assistantNotes = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f),
            };
            _assistantNotes.Click += (s, e) => UpdateAssistantDefaults();
            panelNotes.Controls.Add(_assistantNotes);

            // tab2: advanced controls
            BuildAdvancedTuningControls(panelAdvanced);

            // tab3: wizards controls
            BuildCalibrationWizards(panelWizards);

            // tab4: patches controls
            BuildPatchCenter(panelPatches);

            cmbView.SelectedIndexChanged += (s, e) =>
            {
                int idx = cmbView.SelectedIndex;
                panelNotes.Visible = (idx == 0);
                panelAdvanced.Visible = (idx == 1);
                panelWizards.Visible = (idx == 2);
                panelPatches.Visible = (idx == 3);
                if (idx == 3)
                {
                    RefreshPatchUi();
                }
            };

            _panelNotes = panelNotes;
            _panelAdvanced = panelAdvanced;
            _panelWizards = panelWizards;
            _panelPatches = panelPatches;
            _rightPanel = right;

            right.Controls.Add(panelNotes);
            right.Controls.Add(panelAdvanced);
            right.Controls.Add(panelWizards);
            right.Controls.Add(panelPatches);

            tab.Controls.Add(right);
            tab.Controls.Add(left);
        }

        private void BuildPatchCenter(Panel panel)
        {
            panel.Padding = new Padding(12);

            var layoutMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgPanel
            };
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.Controls.Add(layoutMain);

            // Sol panel: Yama listesi
            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgPanel, Padding = new Padding(8) };
            var lblPatches = MakeLabel("Kullanılabilir Yamalar", new Font("Segoe UI", 9f, FontStyle.Bold), TextPrimary, new Point(8, 8));
            leftPanel.Controls.Add(lblPatches);

            _patchList = new ListBox
            {
                Location = new Point(8, 32),
                Size = new Size(204, 280),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _patchList.SelectedIndexChanged += OnPatchSelectionChanged;
            leftPanel.Controls.Add(_patchList);

            layoutMain.Controls.Add(leftPanel, 0, 0);

            // Sağ panel: Detaylar ve log
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgPanel, Padding = new Padding(8) };

            var lblDetails = MakeLabel("Yama Detayları ve Önizleme", new Font("Segoe UI", 9f, FontStyle.Bold), TextPrimary, new Point(8, 8));
            rightPanel.Controls.Add(lblDetails);

            _patchDetailsBox = new TextBox
            {
                Location = new Point(8, 32),
                Size = new Size(216, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle
            };
            rightPanel.Controls.Add(_patchDetailsBox);

            _btnApplyPatch = MakeButton("Yamayı Uygula", new Point(8, 160), 95, AccentBlue);
            _btnApplyPatch.Click += OnApplyPatchClick;
            rightPanel.Controls.Add(_btnApplyPatch);

            _btnRollbackPatch = MakeButton("Geri Al (Rollback)", new Point(110, 160), 110, AccentRed);
            _btnRollbackPatch.Click += OnRollbackPatchClick;
            rightPanel.Controls.Add(_btnRollbackPatch);

            var lblAudit = MakeLabel("Yama Log Kayıtları", new Font("Segoe UI", 9f, FontStyle.Bold), TextPrimary, new Point(8, 196));
            rightPanel.Controls.Add(lblAudit);

            _patchAuditListView = new ListView
            {
                Location = new Point(8, 220),
                Size = new Size(216, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8.5f),
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _patchAuditListView.Columns.Add("Zaman", 75);
            _patchAuditListView.Columns.Add("Yama ID", 80);
            _patchAuditListView.Columns.Add("Sonuç", 120);
            rightPanel.Controls.Add(_patchAuditListView);

            layoutMain.Controls.Add(rightPanel, 1, 0);
        }

        private void OnPatchSelectionChanged(object sender, EventArgs e)
        {
            var patchEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Patch.IPatchEngine>();
            var profile = _parser?.Profile ?? EcuProfiles.P28;
            var patches = patchEngine.GetAvailablePatches(profile);

            if (_patchList.SelectedIndex < 0 || _patchList.SelectedIndex >= patches.Count)
            {
                _patchDetailsBox.Text = "Lütfen listeden bir yama seçin.";
                _btnApplyPatch.Enabled = false;
                _btnRollbackPatch.Enabled = false;
                return;
            }

            var patch = patches[_patchList.SelectedIndex];
            var buffer = _parser?.GetRomBuffer();
            bool isApplied = patchEngine.IsPatchApplied(patch.PatchId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"ID: {patch.PatchId}");
            sb.AppendLine($"Adı: {patch.Name}");
            sb.AppendLine($"Açıklama: {patch.Description}");
            sb.AppendLine($"Kategori: {patch.Category}");
            sb.AppendLine($"Güvenlik Seviyesi: {patch.SafetyLevel}");
            sb.AppendLine($"Durum: {(isApplied ? "UYGULANDI" : "Uygulanmadı")}");

            if (buffer != null)
            {
                var preview = patchEngine.PreviewPatch(buffer, patch.PatchId, profile);
                sb.AppendLine($"Adres (Offset): 0x{preview.Offset:X4}");
                sb.AppendLine($"Bayt Değişimi: {preview.ByteDifference} byte");
                if (preview.Warnings.Count > 0)
                {
                    sb.AppendLine("Uyarılar:");
                    foreach (var w in preview.Warnings)
                    {
                        sb.AppendLine($"  - {w}");
                    }
                }
            }

            _patchDetailsBox.Text = sb.ToString();
            _btnApplyPatch.Enabled = !isApplied && (buffer != null);
            _btnRollbackPatch.Enabled = isApplied && (buffer != null);
        }

        private void RefreshPatchUi()
        {
            if (_patchList == null) return;

            int lastIndex = _patchList.SelectedIndex;
            _patchList.Items.Clear();
            var patchEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Patch.IPatchEngine>();
            var profile = _parser?.Profile ?? EcuProfiles.P28;
            var patches = patchEngine.GetAvailablePatches(profile);

            foreach (var p in patches)
            {
                bool isApplied = patchEngine.IsPatchApplied(p.PatchId);
                _patchList.Items.Add($"{(isApplied ? "[X] " : "[ ] ")}{p.Name}");
            }

            if (patches.Count > 0)
            {
                if (lastIndex >= 0 && lastIndex < patches.Count)
                    _patchList.SelectedIndex = lastIndex;
                else
                    _patchList.SelectedIndex = 0;
            }
            else
            {
                _patchDetailsBox.Text = "Bu ECU profili için kullanılabilir yama bulunamadı.";
                _btnApplyPatch.Enabled = false;
                _btnRollbackPatch.Enabled = false;
            }

            // Audit Kayıtlarını Listele
            _patchAuditListView.Items.Clear();
            var auditLogs = patchEngine.GetPatchAudit();
            foreach (var log in auditLogs)
            {
                var item = new ListViewItem(log.Timestamp.ToString("HH:mm:ss"));
                item.SubItems.Add(log.PatchId);
                item.SubItems.Add(log.Result);
                _patchAuditListView.Items.Add(item);
            }
        }

        private void OnApplyPatchClick(object sender, EventArgs e)
        {
            var patchEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Patch.IPatchEngine>();
            var profile = _parser?.Profile;
            if (profile == null)
            {
                MessageBox.Show("Yama uygulamak için aktif bir ROM yüklü olmalıdır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var patches = patchEngine.GetAvailablePatches(profile);
            if (_patchList.SelectedIndex < 0 || _patchList.SelectedIndex >= patches.Count) return;

            var patch = patches[_patchList.SelectedIndex];
            var buffer = _parser.GetRomBuffer();

            var result = patchEngine.ApplyPatch(buffer, patch.PatchId, profile, "TunerUser");
            if (result.IsSuccess)
            {
                _parser.SetRomBuffer(buffer);
                MarkDirty();
                RefreshPatchUi();
                MessageBox.Show($"Yama '{patch.Name}' başarıyla uygulandı.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Yama uygulaması doğrulanamadı:\n{result.ErrorMessage}", "Doğrulama Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshPatchUi();
            }
        }

        private void OnRollbackPatchClick(object sender, EventArgs e)
        {
            var patchEngine = Core.Container.ServiceContainer.Resolve<Core.Rom.Patch.IPatchEngine>();
            var profile = _parser?.Profile;
            if (profile == null) return;

            var patches = patchEngine.GetAvailablePatches(profile);
            if (_patchList.SelectedIndex < 0 || _patchList.SelectedIndex >= patches.Count) return;

            var patch = patches[_patchList.SelectedIndex];
            var buffer = _parser.GetRomBuffer();

            var result = patchEngine.RollbackPatch(buffer, patch.PatchId, profile, "TunerUser");
            if (result.IsSuccess)
            {
                _parser.SetRomBuffer(buffer);
                MarkDirty();
                RefreshPatchUi();
                MessageBox.Show($"Yama '{patch.Name}' geri alındı (Rollback).", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Yama geri alma hatası:\n{result.ErrorMessage}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshPatchUi();
            }
        }

        private void BuildAdvancedTuningControls(Panel tab)
        {
            _chkLaunchControlActive = new CheckBox
            {
                Text = "Launch Control (2-Step) Aktif",
                ForeColor = AccentRed,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(16, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _chkLaunchControlActive.CheckedChanged += (s, e) => MarkDirty();

            var lblLC = new Label { Text = "Sınır Devri:", ForeColor = TextPrimary, Location = new Point(16, 52), Size = new Size(100, 20), BackColor = Color.Transparent };
            _numLaunchControlRpm = new NumericUpDown { Minimum = 2000, Maximum = 7000, Increment = 100, Value = 3500, Location = new Point(120, 50), Width = 90, BackColor = BgCard, ForeColor = TextPrimary };
            _numLaunchControlRpm.ValueChanged += (s, e) => MarkDirty();
            var lblLCRpm = new Label { Text = "RPM", ForeColor = TextMuted, Location = new Point(220, 52), Size = new Size(40, 20), BackColor = Color.Transparent };

            var lblLCS = new Label { Text = "Hız Eşiği:", ForeColor = TextPrimary, Location = new Point(16, 82), Size = new Size(100, 20), BackColor = Color.Transparent };
            _numLaunchControlSpeed = new NumericUpDown { Minimum = 0, Maximum = 30, Increment = 1, Value = 8, Location = new Point(120, 80), Width = 90, BackColor = BgCard, ForeColor = TextPrimary };
            _numLaunchControlSpeed.ValueChanged += (s, e) => MarkDirty();
            var lblLCSpeed = new Label { Text = "km/h", ForeColor = TextMuted, Location = new Point(220, 82), Size = new Size(40, 20), BackColor = Color.Transparent };

            var lblDTCHeader = new Label { Text = "🔧 DTC (Arıza Işığı) Devre Dışı Bırakma", ForeColor = AccentBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(16, 130), Size = new Size(350, 22), BackColor = Color.Transparent };

            _chkDtcKnock = new CheckBox { Text = "Vuruntu Sensörünü Bypass Et (Knock Sensor CEL 23)", Location = new Point(16, 160), Width = 400, ForeColor = TextPrimary, AutoSize = true, BackColor = Color.Transparent };
            _chkDtcVtec = new CheckBox { Text = "VTEC Yağ Basınç Müşürünü Bypass Et (VTEC Switch CEL 22)", Location = new Point(16, 185), Width = 400, ForeColor = TextPrimary, AutoSize = true, BackColor = Color.Transparent };
            _chkDtcO2 = new CheckBox { Text = "Oksijen Sensörü Isıtıcısını Bypass Et (O2 Heater CEL 41)", Location = new Point(16, 210), Width = 400, ForeColor = TextPrimary, AutoSize = true, BackColor = Color.Transparent };
            _chkDtcEld = new CheckBox { Text = "ELD - Elektriksel Yük Dedektörünü Bypass Et (ELD CEL 20)", Location = new Point(16, 235), Width = 400, ForeColor = TextPrimary, AutoSize = true, BackColor = Color.Transparent };

            _chkDtcKnock.CheckedChanged += (s, e) => MarkDirty();
            _chkDtcVtec.CheckedChanged += (s, e) => MarkDirty();
            _chkDtcO2.CheckedChanged += (s, e) => MarkDirty();
            _chkDtcEld.CheckedChanged += (s, e) => MarkDirty();

            tab.Controls.Add(_chkLaunchControlActive);
            tab.Controls.Add(lblLC);
            tab.Controls.Add(_numLaunchControlRpm);
            tab.Controls.Add(lblLCRpm);
            tab.Controls.Add(lblLCS);
            tab.Controls.Add(_numLaunchControlSpeed);
            tab.Controls.Add(lblLCSpeed);
            tab.Controls.Add(lblDTCHeader);
            tab.Controls.Add(_chkDtcKnock);
            tab.Controls.Add(_chkDtcVtec);
            tab.Controls.Add(_chkDtcO2);
            tab.Controls.Add(_chkDtcEld);
        }

        private void BuildCalibrationWizards(Panel tab)
        {
            var lblInjHeader = new Label { Text = "🧪 Enjektör Ölçekleme Sihirbazı", ForeColor = AccentBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(16, 20), Size = new Size(350, 22), BackColor = Color.Transparent };

            var lblOldInj = new Label { Text = "Eski Enjektör Boyutu:", ForeColor = TextPrimary, Location = new Point(16, 52), Size = new Size(130, 20), BackColor = Color.Transparent };
            _numOldInjector = new NumericUpDown { Minimum = 180, Maximum = 2000, Increment = 10, Value = 240, Location = new Point(150, 50), Width = 90, BackColor = BgCard, ForeColor = TextPrimary };
            var lblOldInjCc = new Label { Text = "cc", ForeColor = TextMuted, Location = new Point(250, 52), AutoSize = true, BackColor = Color.Transparent };

            var lblNewInj = new Label { Text = "Yeni Enjektör Boyutu:", ForeColor = TextPrimary, Location = new Point(16, 82), Size = new Size(130, 20), BackColor = Color.Transparent };
            _numNewInjector = new NumericUpDown { Minimum = 180, Maximum = 2000, Increment = 10, Value = 440, Location = new Point(150, 80), Width = 90, BackColor = BgCard, ForeColor = TextPrimary };
            var lblNewInjCc = new Label { Text = "cc", ForeColor = TextMuted, Location = new Point(250, 82), AutoSize = true, BackColor = Color.Transparent };

            var btnScaleInj = MakeButton("Enjektörleri Ölçekle", new Point(16, 115), 180, AccentRed);
            btnScaleInj.Click += OnScaleInjectorsClick;

            var lblMapHeader = new Label { Text = "🔌 MAP Sensörü Kalibrasyon Sihirbazı", ForeColor = AccentBlue, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(16, 170), Size = new Size(350, 22), BackColor = Color.Transparent };

            var lblNewMap = new Label { Text = "Yeni MAP Sensörü Seçin:", ForeColor = TextPrimary, Location = new Point(16, 202), Size = new Size(140, 20), BackColor = Color.Transparent };
            _comboNewMapSensor = new ComboBox { Location = new Point(160, 200), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = BgCard, ForeColor = TextPrimary };
            _comboNewMapSensor.Items.AddRange(new object[] { "Stok 1-Bar (20 - 105 kPa)", "Motorola 2.5-Bar (20 - 250 kPa)", "Omnipower 3-Bar (20 - 300 kPa)", "Omnipower 4-Bar (20 - 400 kPa)" });
            _comboNewMapSensor.SelectedIndex = 0;

            var btnScaleMap = MakeButton("Yük Eksenini Kalibre Et", new Point(16, 235), 180, AccentBlue);
            btnScaleMap.Click += OnScaleMapSensorClick;

            tab.Controls.Add(lblInjHeader);
            tab.Controls.Add(lblOldInj);
            tab.Controls.Add(_numOldInjector);
            tab.Controls.Add(lblOldInjCc);
            tab.Controls.Add(lblNewInj);
            tab.Controls.Add(_numNewInjector);
            tab.Controls.Add(lblNewInjCc);
            tab.Controls.Add(btnScaleInj);

            tab.Controls.Add(lblMapHeader);
            tab.Controls.Add(lblNewMap);
            tab.Controls.Add(_comboNewMapSensor);
            tab.Controls.Add(btnScaleMap);
        }

        private void OnScaleInjectorsClick(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded)
            {
                NoRomWarning();
                return;
            }

            double oldCc = (double)_numOldInjector.Value;
            double newCc = (double)_numNewInjector.Value;
            if (oldCc <= 0 || newCc <= 0) return;

            double ratio = oldCc / newCc;

            // Fuel map
            byte[,] fuelMap = _parser.ReadFuelMap();
            int rows = fuelMap.GetLength(0);
            int cols = fuelMap.GetLength(1);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int scaled = (int)Math.Round(fuelMap[r, c] * ratio);
                    fuelMap[r, c] = (byte)Math.Max(0, Math.Min(255, scaled));
                }
            }

            _parser.WriteFuelMap(fuelMap);
            _fuelGrid.SetData(fuelMap);

            MarkDirty();
            MessageBox.Show($"Enjektör ölçekleme başarıyla uygulandı!\nÖlçek oranı: {ratio:F3}\nYakıt haritası güncellendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnScaleMapSensorClick(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded)
            {
                NoRomWarning();
                return;
            }

            int index = _comboNewMapSensor.SelectedIndex;
            int maxKpa = 105;
            if (index == 1) maxKpa = 250;
            else if (index == 2) maxKpa = 300;
            else if (index == 3) maxKpa = 400;

            // Rescale LoadAxis
            int cols = _activeProfile.FuelMapCols;
            int minKpa = 20;

            for (int i = 0; i < cols; i++)
            {
                double pct = (double)i / (cols - 1);
                _activeProfile.LoadAxis[i] = (int)Math.Round(minKpa + pct * (maxKpa - minKpa));
            }

            // Rebuild column headers for grids
            _fuelGrid.LoadMap(_parser.ReadFuelMap(), "Yakıt", _activeProfile);
            _ignGrid.LoadMap(_parser.ReadIgnitionMap(), "Ateşleme", _activeProfile);
            _fuelGrid.RebuildGrid();
            _ignGrid.RebuildGrid();

            MarkDirty();
            MessageBox.Show($"MAP sensörü başarıyla kalibre edildi!\nYeni Yük ekseni (kPa) 20 - {maxKpa} aralığına ölçeklendi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddAssistantLabel(Control parent, string text, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(14, y),
                Size = new Size(310, 22),
                ForeColor = AccentBlue,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            });
        }

        private NumericUpDown AddAssistantSpinner(Control parent, string label, decimal min, decimal max, decimal inc, decimal value, ref int y, int decimals = 0)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(14, y + 4),
                Size = new Size(145, 22),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9f),
            });

            var spinner = new NumericUpDown
            {
                Location = new Point(170, y),
                Width = 90,
                Minimum = min,
                Maximum = max,
                Increment = inc,
                DecimalPlaces = decimals,
                Value = Clamp(value, min, max),
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
            };

            parent.Controls.Add(spinner);
            y += 30;
            return spinner;
        }


        // ── VTEC / Rev Limit / Limitörler Paneli ─────────────────

        private void BuildVtecPanel()
        {
            _vtecPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = BgPanel,
            };
            _vtecPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, _vtecPanel.Width, 0);
            };

            int x = 12;

            // VTEC RPM
            _vtecRpmLabel = MakeLabel("⚡ VTEC RPM:", new Font("Segoe UI", 9f, FontStyle.Bold), VtecGreen, new Point(x, 18));
            x += 85;

            _vtecRpmSpinner = MakeSpinner(new Point(x, 15), 65, 1000, 8000, 100, 4800);
            _vtecRpmSpinner.ValueChanged += (s, e) => MarkDirty();
            x += 70;

            MakeLabel("rpm", new Font("Segoe UI", 8f), TextMuted, new Point(x, 20)).Parent = _vtecPanel;
            x += 32;

            // VTEC Yük Eşiği (M2)
            var vtecLoadLabel = MakeLabel("Yük Eşiği:", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(x, 18));
            x += 65;

            _vtecLoadSpinner = MakeSpinner(new Point(x, 15), 55, 10, 150, 5, 60);
            _vtecLoadSpinner.ValueChanged += (s, e) => MarkDirty();
            x += 60;

            MakeLabel("kPa", new Font("Segoe UI", 8f), TextMuted, new Point(x, 20)).Parent = _vtecPanel;
            x += 35;

            // Rev Limit
            var revLabel = MakeLabel("Rev Limit:", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(x, 18));
            x += 65;

            _revLimitSpinner = MakeSpinner(new Point(x, 15), 65, 4000, 9500, 100, 7200);
            _revLimitSpinner.ValueChanged += (s, e) => MarkDirty();
            x += 70;

            MakeLabel("rpm", new Font("Segoe UI", 8f), TextMuted, new Point(x, 20)).Parent = _vtecPanel;
            x += 35;

            // Hız Sınırı (M2)
            var speedLabel = MakeLabel("Hız Sınırı:", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(x, 18));
            x += 65;

            _speedLimitSpinner = MakeSpinner(new Point(x, 15), 55, 50, 300, 5, 180);
            _speedLimitSpinner.ValueChanged += (s, e) => MarkDirty();
            x += 60;

            MakeLabel("km/h", new Font("Segoe UI", 8f), TextMuted, new Point(x, 20)).Parent = _vtecPanel;
            x += 44;

            // Enjektör Ölü Süresi (M2)
            var injLabel = MakeLabel("Inj. Dead:", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(x, 18));
            x += 65;

            _injDeadTimeSpinner = new NumericUpDown
            {
                Location = new Point(x, 15),
                Width = 55,
                Minimum = 0,
                Maximum = (decimal)12.75,
                Increment = (decimal)0.05,
                DecimalPlaces = 2,
                Value = (decimal)0.80,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Enabled = false,
            };
            _injDeadTimeSpinner.ValueChanged += (s, e) => MarkDirty();
            x += 60;

            MakeLabel("ms", new Font("Segoe UI", 8f), TextMuted, new Point(x, 20)).Parent = _vtecPanel;

            // Tüm spinner'ları panele ekle
            _vtecPanel.Controls.Add(_vtecRpmLabel);
            _vtecPanel.Controls.Add(_vtecRpmSpinner);
            _vtecPanel.Controls.Add(vtecLoadLabel);
            _vtecPanel.Controls.Add(_vtecLoadSpinner);
            _vtecPanel.Controls.Add(revLabel);
            _vtecPanel.Controls.Add(_revLimitSpinner);
            _vtecPanel.Controls.Add(speedLabel);
            _vtecPanel.Controls.Add(_speedLimitSpinner);
            _vtecPanel.Controls.Add(injLabel);
            _vtecPanel.Controls.Add(_injDeadTimeSpinner);

            Controls.Add(_vtecPanel);
        }

        private NumericUpDown MakeSpinner(Point loc, int w, decimal min, decimal max, decimal inc, decimal val)
        {
            var s = new NumericUpDown
            {
                Location = loc,
                Width = w,
                Minimum = min,
                Maximum = max,
                Increment = inc,
                Value = Clamp(val, min, max),
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Enabled = false,
            };
            return s;
        }

        private void UpdateVtecPanel()
        {
            bool vtec = _activeProfile.HasVtec;
            _vtecRpmLabel.Visible = vtec;
            _vtecRpmSpinner.Visible = vtec;
            _vtecLoadSpinner.Visible = vtec;

            if (vtec)
            {
                _vtecRpmSpinner.Minimum = _activeProfile.VtecRpmMin;
                _vtecRpmSpinner.Maximum = _activeProfile.VtecRpmMax;
                _vtecRpmSpinner.Value = Clamp((int)_vtecRpmSpinner.Value,
                    _activeProfile.VtecRpmMin, _activeProfile.VtecRpmMax);
            }
            _revLimitSpinner.Minimum = _activeProfile.RevLimitMin;
            _revLimitSpinner.Maximum = _activeProfile.RevLimitMax;
        }

        // ── Status Bar ───────────────────────────────────────────

        private void BuildStatusBar()
        {
            _status = new StatusStrip
            {
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = TextPrimary,
                SizingGrip = false,
            };
            _status.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, _status.Width, 0);
            };

            _statusLabel = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextPrimary,
            };
            _checksumLabel = new ToolStripStatusLabel
            {
                Text = "—",
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                ForeColor = TextMuted,
            };
            _profileLabel = new ToolStripStatusLabel
            {
                Text = "",
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                ForeColor = AccentBlue,
            };
            _datalogStatusLabel = new ToolStripStatusLabel
            {
                Text = "⬛ OFFLINE",
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                ForeColor = TextMuted,
            };
            _progStatusLabel = new ToolStripStatusLabel
            {
                Text = "🔌 PROG: OFFLINE",
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                ForeColor = TextMuted,
            };
            _emuStatusLabel = new ToolStripStatusLabel
            {
                Text = "🎮 EMU: OFFLINE",
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                ForeColor = TextMuted,
            };

            _status.Items.Add(_statusLabel);
            _status.Items.Add(_checksumLabel);
            _status.Items.Add(_profileLabel);
            _status.Items.Add(_datalogStatusLabel);
            _status.Items.Add(_progStatusLabel);
            _status.Items.Add(_emuStatusLabel);
            Controls.Add(_status);
        }

        // ── M1: Datalog Bağlantı Olayları ────────────────────────

        private void OnDatalogConnect(object sender, EventArgs e)
        {
            if (_comPortCombo.SelectedItem == null)
            { MessageBox.Show("Lütfen bir COM port seçin.", "Bağlantı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                _datalogMgr.Connect(_comPortCombo.SelectedItem.ToString());
                SetDatalogStatus(true, false);
                _fuelGrid.StartTracing();
                _ignGrid.StartTracing();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDatalogSimulate(object sender, EventArgs e)
        {
            _datalogMgr.StartSimulation();
            SetDatalogStatus(true, true);
            _fuelGrid.StartTracing();
            _ignGrid.StartTracing();
        }

        private void OnDatalogDisconnect(object sender, EventArgs e)
        {
            _datalogMgr.Disconnect();
            SetDatalogStatus(false, false);
            _fuelGrid.StopTracing();
            _ignGrid.StopTracing();
        }

        private void SetDatalogStatus(bool running, bool sim)
        {
            _btnConnect.Enabled = !running;
            _btnSimulate.Enabled = !running;
            _btnDisconnect.Enabled = running;
            _comPortCombo.Enabled = !running;

            if (running)
                _datalogStatusLabel.Text = sim ? "🟢 SİMÜLASYON" : "🔴 CANLI";
            else
                _datalogStatusLabel.Text = "⬛ OFFLINE";

            _datalogStatusLabel.ForeColor = running ? VtecGreen : TextMuted;
        }

        private void OnTelemetryDataReceived(TelemetryFrame frame)
        {
            // Cross-thread güvenli UI güncellemesi
            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => OnTelemetryDataReceived(frame)));
                return;
            }

            _telemetryDash?.UpdateValues(
                frame.Rpm, frame.Map, frame.Speed,
                frame.Afr, frame.Ect, frame.Iat);

            // Cell Tracing — iki grid üzerinde de vurgula
            _fuelGrid.SetTraceCell(frame.Rpm, frame.Map);
            _ignGrid.SetTraceCell(frame.Rpm, frame.Map);

            // Feed telemetry dynamically to AutoTune Closed-Loop engine
            if (_autoTuneEngine != null && _autoTuneEngine.IsRunning)
            {
                var snap = new HondaTuner.Core.Telemetry.TelemetrySnapshot(
                    version: "2.0",
                    timestamp: DateTime.UtcNow,
                    sequence: _telemetrySequence++,
                    rpm: frame.Rpm,
                    tps: frame.Tps,
                    map: frame.Map,
                    ect: frame.Ect,
                    iat: frame.Iat,
                    battery: frame.BatteryVolts,
                    speed: frame.Speed,
                    injectorDuty: frame.InjDuty,
                    ignitionAdvance: frame.IgnAdvance,
                    afr: frame.Afr,
                    lambda: frame.Afr / 14.7,
                    knockCount: 0,
                    fuelTrimStft: 0,
                    fuelTrimLtft: 0,
                    closedLoop: true,
                    openLoop: false,
                    engineLoad: frame.Map
                );
                _autoTuneEngine.ProcessTelemetry(snap);

                // Update ECT and Knock Indicators live on UI
                if (_lblAtEct != null)
                {
                    _lblAtEct.Text = $"ECT Sıcaklığı: {frame.Ect:0} °C";
                    _lblAtEct.ForeColor = frame.Ect >= 100.0 ? AccentRed : TextPrimary;
                }
            }
        }

        // ── Araç / Profil Seçimi ─────────────────────────────────

        private void OnSelectVehicle(object sender, EventArgs e)
        {
            if (_isDirty && !ConfirmDiscard()) return;
            using var dlg = new VehicleSelectDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            _activeVehicle = dlg.SelectedVehicle;
            ApplyProfile(dlg.SelectedProfile, null);
        }

        private void ApplyProfile(EcuProfile profile, ToolStripMenuItem clickedItem)
        {
            _activeProfile = profile;
            if (clickedItem != null && clickedItem.OwnerItem is ToolStripMenuItem parent)
                foreach (ToolStripItem si in parent.DropDownItems)
                    if (si is ToolStripMenuItem mi) mi.Checked = mi == clickedItem;
            UpdateProfileUI();
            UpdateVtecPanel();
            UpdateAssistantDefaults();
            SetStatus($"Profil: {profile.Name}" +
                (_activeVehicle != null ? $"  |  {_activeVehicle}" : ""));
        }

        private void UpdateProfileUI()
        {
            string vtecTag = _activeProfile.HasVtec ? "VTEC" : "Non-VTEC";
            string iabTag = _activeProfile.HasIab ? " + IAB" : "";
            string vehicle = _activeVehicle != null
                ? $"{_activeVehicle.Make} {_activeVehicle.Model} {_activeVehicle.Trim} ({_activeVehicle.YearRange})"
                : _activeProfile.CasaTag;

            Text = $"HondaTuner — {_activeProfile.EcuCode} / {_activeProfile.EngineCode}" +
                   (_isDirty ? "  ●" : "");
            if (_headerVehicleLabel != null)
                _headerVehicleLabel.Text = $"📌  {vehicle}  |  {vtecTag}{iabTag}";
            if (_profileLabel != null)
                _profileLabel.Text = $"[{_activeProfile.EcuCode}] {_activeProfile.EngineCode}  {vtecTag}{iabTag}";
        }

        // ── Dosya İşlemleri ──────────────────────────────────────

        private void OnOpen(object sender, EventArgs e)
        {
            if (_isDirty && !ConfirmDiscard()) return;

            using var dlg = new OpenFileDialog
            {
                Title = "ROM Seç",
                Filter = "ROM Binary|*.bin|Tüm Dosyalar|*.*",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                byte[] romBytes = File.ReadAllBytes(dlg.FileName);
                var identifier = Core.Container.ServiceContainer.Resolve<Core.Interfaces.IRomIdentifier>();
                var identificationResult = identifier.IdentifyRom(romBytes, _loadedProfiles);

                if (identificationResult.IsMismatch)
                {
                    var msg = $"ROM otomatik olarak tanımlanamadı (Güven: %{identificationResult.Confidence:F0}).\nManuel olarak bir ECU / Araç profili seçmek ister misiniz?";
                    var r = MessageBox.Show(msg, "Tanımlama Başarısız", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r != DialogResult.Yes) return;

                    using var vehicleDlg = new VehicleSelectDialog();
                    if (vehicleDlg.ShowDialog(this) != DialogResult.OK) return;
                    _activeVehicle = vehicleDlg.SelectedVehicle;
                    _activeProfile = vehicleDlg.SelectedProfile;
                }
                else
                {
                    _activeProfile = identificationResult.MatchedProfile;
                    // EcuDatabase üzerinden eşleşen araç varsa seç
                    var record = EcuDatabase.GetByCode(_activeProfile.EcuCode);
                    if (record != null && record.Vehicles != null && record.Vehicles.Length > 0)
                    {
                        _activeVehicle = record.Vehicles[0];
                    }
                    else
                    {
                        _activeVehicle = null;
                    }

                    string matchDetails = $"Tanımlanan ECU: {_activeProfile.EcuCode}\nMotor: {_activeProfile.EngineCode}\nGüven: %{identificationResult.Confidence:F0}\n\nEşleşen Kurallar:\n"
                        + string.Join("\n", identificationResult.MatchedRules);
                    MessageBox.Show(matchDetails, "ROM Başarıyla Tanımlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                _parser.Load(dlg.FileName, _activeProfile);

                _backupMgr.InitBackup(_parser.GetRomBuffer());

                _stockFuelMap = _parser.ReadFuelMap();
                _stockIgnMap = _parser.ReadIgnitionMap();

                _fuelGrid.LoadMap(_stockFuelMap, "Fuel Map", _activeProfile);
                _ignGrid.LoadMap(_stockIgnMap, "Ignition Map", _activeProfile);

                // M2 — ROM parametrelerini yükle
                if (_activeProfile.HasVtec)
                {
                    _vtecRpmSpinner.Minimum = _activeProfile.VtecRpmMin;
                    _vtecRpmSpinner.Maximum = _activeProfile.VtecRpmMax;
                    _vtecRpmSpinner.Value = _parser.ReadVtecRpm();
                    _vtecRpmSpinner.Enabled = true;

                    _vtecLoadSpinner.Enabled = true;
                    _vtecLoadSpinner.Value = Math.Max(10,
                        Math.Min(150, _parser.ReadVtecLoadThreshold()));
                }
                else
                {
                    _vtecRpmSpinner.Enabled = false;
                    _vtecLoadSpinner.Enabled = false;
                }

                _revLimitSpinner.Minimum = _activeProfile.RevLimitMin;
                _revLimitSpinner.Maximum = _activeProfile.RevLimitMax;
                _revLimitSpinner.Value = _parser.ReadRevLimit();
                _revLimitSpinner.Enabled = true;

                _speedLimitSpinner.Value = Math.Max(50, Math.Min(300, _parser.ReadSpeedLimiter()));
                _speedLimitSpinner.Enabled = true;

                _injDeadTimeSpinner.Value = (decimal)_parser.ReadInjectorDeadTime();
                _injDeadTimeSpinner.Enabled = true;

                _chkLaunchControlActive.Checked = _parser.ReadLaunchControlActive();
                _numLaunchControlRpm.Value = Math.Max(2000, Math.Min(7000, _parser.ReadLaunchControlRpm()));
                _numLaunchControlSpeed.Value = Math.Max(0, Math.Min(30, _parser.ReadLaunchControlSpeed()));

                _chkDtcKnock.Checked = _parser.ReadDtcBypass(0x1FB6);
                _chkDtcVtec.Checked = _parser.ReadDtcBypass(0x1FB7);
                _chkDtcO2.Checked = _parser.ReadDtcBypass(0x1FB8);
                _chkDtcEld.Checked = _parser.ReadDtcBypass(0x1FB9);

                _diffView.Compare(_stockFuelMap, _stockFuelMap, "Fuel Map");

                _isDirty = false;
                SetStatus($"✅  Yüklendi: {System.IO.Path.GetFileName(dlg.FileName)}  [{_activeProfile.EcuCode}]");
                SetChecksum(true);
                UpdateProfileUI();

                var romService = Core.Container.ServiceContainer.Resolve<Core.Interfaces.IRomService>();
                if (_metadataControl != null)
                {
                    _metadataControl.BindRomService(romService);
                }
                if (_reverseControl != null)
                {
                    _reverseControl.BindRomService(romService);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Yükleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateUIFromRom()
        {
            if (!_parser.IsLoaded) return;

            if (_activeProfile.HasVtec)
            {
                _vtecRpmSpinner.Minimum = _activeProfile.VtecRpmMin;
                _vtecRpmSpinner.Maximum = _activeProfile.VtecRpmMax;
                _vtecRpmSpinner.Value = _parser.ReadVtecRpm();
                _vtecRpmSpinner.Enabled = true;

                _vtecLoadSpinner.Enabled = true;
                _vtecLoadSpinner.Value = Math.Max(10,
                    Math.Min(150, _parser.ReadVtecLoadThreshold()));
            }
            else
            {
                _vtecRpmSpinner.Enabled = false;
                _vtecLoadSpinner.Enabled = false;
            }

            _revLimitSpinner.Minimum = _activeProfile.RevLimitMin;
            _revLimitSpinner.Maximum = _activeProfile.RevLimitMax;
            _revLimitSpinner.Value = _parser.ReadRevLimit();
            _revLimitSpinner.Enabled = true;

            _speedLimitSpinner.Value = Math.Max(50, Math.Min(300, _parser.ReadSpeedLimiter()));
            _speedLimitSpinner.Enabled = true;

            _injDeadTimeSpinner.Value = (decimal)_parser.ReadInjectorDeadTime();
            _injDeadTimeSpinner.Enabled = true;

            _chkLaunchControlActive.Checked = _parser.ReadLaunchControlActive();
            _numLaunchControlRpm.Value = Math.Max(2000, Math.Min(7000, _parser.ReadLaunchControlRpm()));
            _numLaunchControlSpeed.Value = Math.Max(0, Math.Min(30, _parser.ReadLaunchControlSpeed()));

            _chkDtcKnock.Checked = _parser.ReadDtcBypass(0x1FB6);
            _chkDtcVtec.Checked = _parser.ReadDtcBypass(0x1FB7);
            _chkDtcO2.Checked = _parser.ReadDtcBypass(0x1FB8);
            _chkDtcEld.Checked = _parser.ReadDtcBypass(0x1FB9);
        }

        private void OnUndoEdit(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }
            byte[] undoData = _backupMgr.Undo();
            if (undoData != null)
            {
                _parser.SetRomBuffer(undoData);
                _fuelGrid.LoadMap(_parser.ReadFuelMap(), "Fuel Map", _activeProfile);
                _ignGrid.LoadMap(_parser.ReadIgnitionMap(), "Ignition Map", _activeProfile);
                UpdateUIFromRom();
                MarkDirty();
                SetStatus("Geri Alma (Undo) işlemi uygulandı.");
            }
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) return;
            CommitAndSave(_parser.FilePath);
        }

        private void OnSaveAs(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) return;
            using var dlg = new SaveFileDialog
            {
                Title = "ROM'u Kaydet",
                Filter = "ROM Binary|*.bin",
                FileName = $"{_activeProfile.EcuCode.ToLower()}_modified.bin",
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                CommitAndSave(dlg.FileName);
        }

        private void CommitAndSave(string path)
        {
            try
            {
                _parser.WriteFuelMap(_fuelGrid.GetData());
                _parser.WriteIgnitionMap(_ignGrid.GetData());

                if (_activeProfile.HasVtec)
                {
                    _parser.WriteVtecRpm((int)_vtecRpmSpinner.Value);
                    _parser.WriteVtecLoadThreshold((int)_vtecLoadSpinner.Value);
                }

                _parser.WriteRevLimit((int)_revLimitSpinner.Value);
                _parser.WriteSpeedLimiter((int)_speedLimitSpinner.Value);
                _parser.WriteInjectorDeadTime((double)_injDeadTimeSpinner.Value);

                if (_chkLaunchControlActive != null)
                {
                    _parser.WriteLaunchControlActive(_chkLaunchControlActive.Checked);
                    _parser.WriteLaunchControlRpm((int)_numLaunchControlRpm.Value);
                    _parser.WriteLaunchControlSpeed((int)_numLaunchControlSpeed.Value);

                    _parser.WriteDtcBypass(0x1FB6, _chkDtcKnock.Checked);
                    _parser.WriteDtcBypass(0x1FB7, _chkDtcVtec.Checked);
                    _parser.WriteDtcBypass(0x1FB8, _chkDtcO2.Checked);
                    _parser.WriteDtcBypass(0x1FB9, _chkDtcEld.Checked);
                }

                _parser.Save(path);

                var romService = Core.Container.ServiceContainer.Resolve<Core.Interfaces.IRomService>();
                if (romService != null)
                {
                    romService.SaveMetadata(path);
                }

                _backupMgr.SaveVersion(_parser.GetRomBuffer(), "ROM Degisiklikleri Kaydedildi");

                _isDirty = false;
                SetStatus($"💾  Kaydedildi: {System.IO.Path.GetFileName(path)}");
                SetChecksum(true);
                UpdateProfileUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Kayıt Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Araçlar ──────────────────────────────────────────────

        private void OnApplyAssistantBasemap(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }

            var setup = BuildAssistantSetup();
            var result = TuningAssistant.CreateBaseMap(
                _activeProfile,
                _fuelGrid.GetData(),
                _ignGrid.GetData(),
                setup);

            _fuelGrid.SetData(result.FuelMap);
            _ignGrid.SetData(result.IgnitionMap);

            if (_activeProfile.HasVtec)
                _vtecRpmSpinner.Value = Clamp(setup.VtecRpm, _vtecRpmSpinner.Minimum, _vtecRpmSpinner.Maximum);
            _revLimitSpinner.Value = Clamp(setup.RevLimitRpm, _revLimitSpinner.Minimum, _revLimitSpinner.Maximum);
            _speedLimitSpinner.Value = Clamp(setup.SpeedLimitKmh, _speedLimitSpinner.Minimum, _speedLimitSpinner.Maximum);
            _injDeadTimeSpinner.Value = Clamp((decimal)setup.InjectorDeadTimeMs, _injDeadTimeSpinner.Minimum, _injDeadTimeSpinner.Maximum);

            _assistantNotes.Text = result.Summary;
            _assistantNotes.SelectionStart = 0;
            _assistantNotes.SelectionLength = 0;
            _assistantNotes.ScrollToCaret();

            MarkDirty();
            SetStatus("Tuning asistanı basemap'i haritalara uyguladı.");
        }

        private void OnApplyStage1Basemap(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }

            var confirm = MessageBox.Show(
                "Stage 1 Basemap;\n" +
                "  • VTEC RPM, Rev Limit ve Hız Sınırı'nı ROM'a yazacak\n" +
                "  • Yakıt ve Ateşleme haritalarını enjektör + hedef AFR'ye göre güncelleyecek\n\n" +
                "Devam etmek istiyor musunuz?",
                "⚡ Stage 1 Basemap Onayla",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                var setup = BuildAssistantSetup();

                var result = TuningAssistant.CreateStage1Map(
                    _activeProfile,
                    _fuelGrid.GetData(),
                    _ignGrid.GetData(),
                    setup,
                    _parser);   // parser writes limits directly to ROM

                // Sync UI spinners with the values that were written
                if (_activeProfile.HasVtec)
                    _vtecRpmSpinner.Value = Clamp(setup.VtecRpm, _vtecRpmSpinner.Minimum, _vtecRpmSpinner.Maximum);
                _revLimitSpinner.Value = Clamp(setup.RevLimitRpm, _revLimitSpinner.Minimum, _revLimitSpinner.Maximum);
                _speedLimitSpinner.Value = Clamp(setup.SpeedLimitKmh, _speedLimitSpinner.Minimum, _speedLimitSpinner.Maximum);

                // Sync fuel/ignition grids with the written maps
                _fuelGrid.SetData(result.FuelMap);
                _ignGrid.SetData(result.IgnitionMap);

                // Show the full report in the assistant notes panel
                _assistantNotes.Text = result.Summary;
                _assistantNotes.SelectionStart = 0;
                _assistantNotes.SelectionLength = 0;
                _assistantNotes.ScrollToCaret();

                MarkDirty();
                SetStatus("⚡ Stage 1 Basemap ROM'a uygulandı — kaydetmeyi unutmayın.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Stage 1 uygulanamadı:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnApplyWidebandCorrection(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }

            var corrected = TuningAssistant.ApplyWidebandCorrection(
                _fuelGrid.GetData(),
                _activeProfile,
                (double)_measuredAfrSpinner.Value,
                (double)_targetAfrSpinner.Value,
                (int)_widebandRpmSpinner.Value,
                (int)_widebandLoadSpinner.Value,
                (int)_widebandRadiusSpinner.Value);

            _fuelGrid.SetData(corrected);
            _assistantNotes.Text =
                $"Wideband duzeltme uygulandi.\r\n" +
                $"Olculen AFR: {_measuredAfrSpinner.Value:0.0}\r\n" +
                $"Hedef AFR: {_targetAfrSpinner.Value:0.0}\r\n" +
                $"Hücre: {_widebandRpmSpinner.Value:0} rpm / {_widebandLoadSpinner.Value:0} kPa\r\n" +
                $"Etki alani: {_widebandRadiusSpinner.Value:0}\r\n\r\n" +
                "Olculen AFR hedefin ustundeyse yakit eklenir; altindaysa yakit azaltilir.";

            _assistantNotes.SelectionStart = 0;
            _assistantNotes.SelectionLength = 0;
            _assistantNotes.ScrollToCaret();

            MarkDirty();
            SetStatus("Wideband AFR ölçümüne göre yakıt haritası düzeltildi.");
        }

        private TuningSetup BuildAssistantSetup()
        {
            var defaults = TuningAssistant.DefaultsFor(_activeProfile, _activeVehicle);
            defaults.Goal = SelectedGoal();
            defaults.InjectorCc = (int)_injectorCcSpinner.Value;
            defaults.MapSensorBar = (int)_mapSensorSpinner.Value;
            defaults.TargetAfrPower = (double)_targetAfrSpinner.Value;
            defaults.VtecRpm = _activeProfile.HasVtec
                ? (int)Clamp(_activeProfile.VtecRpmDefault, _vtecRpmSpinner.Minimum, _vtecRpmSpinner.Maximum)
                : 0;
            defaults.RevLimitRpm = (int)Clamp(_activeProfile.RevLimitDefault, _revLimitSpinner.Minimum, _revLimitSpinner.Maximum);
            defaults.SpeedLimitKmh = 220;
            defaults.InjectorDeadTimeMs = EstimateInjectorDeadTime(defaults.InjectorCc);
            return defaults;
        }

        private void UpdateAssistantDefaults()
        {
            if (_goalCombo == null) return;
            var setup = TuningAssistant.DefaultsFor(_activeProfile, _activeVehicle);
            _targetAfrSpinner.Value = (decimal)setup.TargetAfrPower;
            _widebandRpmSpinner.Value = Clamp(_activeProfile.HasVtec ? _activeProfile.VtecRpmDefault : 4500,
                _widebandRpmSpinner.Minimum, _widebandRpmSpinner.Maximum);

            string dbg = "";
            if (_panelNotes != null)
            {
                dbg = $"[DBG] Notes: Loc={_panelNotes.Location}, Sz={_panelNotes.Size}, Dock={_panelNotes.Dock}, Vis={_panelNotes.Visible}, Parent={_panelNotes.Parent?.GetType().Name}\r\n" +
                      $"[DBG] Text: Loc={_assistantNotes.Location}, Sz={_assistantNotes.Size}, Dock={_assistantNotes.Dock}, Parent={_assistantNotes.Parent?.GetType().Name}\r\n" +
                      $"[DBG] Right: Loc={_rightPanel.Location}, Sz={_rightPanel.Size}, Dock={_rightPanel.Dock}\r\n\r\n";
            }

            _assistantNotes.Text = dbg +
                $"Aktif profil: {_activeProfile.Name}\r\n" +
                $"Araç: {(_activeVehicle != null ? _activeVehicle.ToString() : _activeProfile.CasaTag)}\r\n\r\n" +
                "ROM dosyası kullanıcıdan alınır. Uygulama telifli stock ROM indirmez veya dağıtmaz; test/basemap üretir ve kendi okuduğun ROM üzerinde çalışır.";

            _assistantNotes.SelectionStart = 0;
            _assistantNotes.SelectionLength = 0;
            _assistantNotes.ScrollToCaret();
        }

        private TuningGoal SelectedGoal()
        {
            switch (_goalCombo?.SelectedIndex ?? 0)
            {
                case 1: return TuningGoal.NaturallyAspirated;
                case 2: return TuningGoal.TurboSafeBase;
                case 3: return TuningGoal.Economy;
                case 4: return TuningGoal.StockStreet;
                default: return TuningGoal.IesVtecStreet;
            }
        }

        private static double EstimateInjectorDeadTime(int injectorCc)
        {
            if (injectorCc >= 750) return 1.05;
            if (injectorCc >= 550) return 0.95;
            if (injectorCc >= 370) return 0.85;
            return 0.80;
        }

        private void OnVerifyChecksum(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }
            bool ok = _parser.VerifyChecksum();
            SetChecksum(ok);
            MessageBox.Show(
                ok ? "✅ Checksum geçerli." : "❌ Checksum hatalı!",
                "Checksum", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void OnResetToStock(object sender, EventArgs e)
        {
            if (!_parser.IsLoaded) { NoRomWarning(); return; }
            if (MessageBox.Show("Tüm değişiklikler silinecek. Emin misin?",
                "Stock'a Döndür", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _parser.Load(_parser.FilePath, _activeProfile);
            _fuelGrid.LoadMap(_parser.ReadFuelMap(), "Fuel Map", _activeProfile);
            _ignGrid.LoadMap(_parser.ReadIgnitionMap(), "Ignition Map", _activeProfile);

            if (_activeProfile.HasVtec)
                _vtecRpmSpinner.Value = _parser.ReadVtecRpm();
            _revLimitSpinner.Value = _parser.ReadRevLimit();

            _isDirty = false;
            SetStatus("↩  Stock ROM'a döndürüldü.");
        }

        // ── Diff ─────────────────────────────────────────────────

        private void RefreshDiff()
        {
            if (_stockFuelMap == null) return;
            string map = _activeTabIndex == 0 ? "Fuel Map" : "Ignition Map";
            var stock = map == "Fuel Map" ? _stockFuelMap : _stockIgnMap;
            var modified = map == "Fuel Map" ? _fuelGrid.GetData() : _ignGrid.GetData();
            _diffView.Compare(stock, modified, map);
        }

        // ── Yardımcılar ──────────────────────────────────────────

        private void MarkDirty()
        {
            _isDirty = true;
            UpdateProfileUI();
        }

        private void SetStatus(string msg) => _statusLabel.Text = msg;
        private void SetChecksum(bool ok) => _checksumLabel.Text =
            ok ? "✅ Checksum OK" : "❌ Checksum HATA";

        private void NoRomWarning() =>
            MessageBox.Show("Önce bir ROM dosyası yükleyin.",
                "ROM Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private bool ConfirmDiscard() =>
            MessageBox.Show("Kaydedilmemiş değişiklikler var. Devam et?",
                "Uyarı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

        private static Label MakeLabel(string text, Font font, Color fore, Point loc) =>
            new Label
            {
                Text = text,
                Font = font,
                ForeColor = fore,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = loc,
            };

        private Button MakeButton(string text, Point loc, int w, Color accent)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(w, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, accent),
                ForeColor = accent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderColor = accent;
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        /// <summary>Donanım paneli için beyaz yazılı, düz renkli buton.</summary>
        private Button MakeHwButton(string text, int w, Color bg)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(w, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            b.FlatAppearance.BorderColor = ControlPaint.Light(bg, 0.4f);
            b.FlatAppearance.BorderSize = 1;
            // Disabled durumda da beyaz metin korunuyor
            b.Paint += (s, e) =>
            {
                if (!b.Enabled)
                {
                    e.Graphics.Clear(Color.FromArgb(60, bg.R, bg.G, bg.B));
                    TextRenderer.DrawText(e.Graphics, b.Text, b.Font,
                        b.ClientRectangle, Color.FromArgb(160, 255, 255, 255),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
            return b;
        }

        // ── AutoTune Closed Loop (Phase 8) ──────────────────────────

        private void BuildAutoTunePage(Panel tab)
        {
            tab.Padding = new Padding(12);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = BgDark
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            var leftPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = BgPanel,
                Padding = new Padding(16)
            };

            var title = new Label
            {
                Text = "Closed Loop AutoTune Kontrol Paneli",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = AccentRed,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };
            leftPanel.Controls.Add(title);

            var gbConfig = new GroupBox
            {
                Text = "Oturum Ayarları",
                ForeColor = TextPrimary,
                Size = new Size(350, 180),
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(12)
            };

            var lblMode = MakeLabel("Çalışma Modu:", 12, 20);
            _atModeCombo = new ComboBox
            {
                Location = new Point(130, 18),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            _atModeCombo.Items.AddRange(new object[] { "DryRun", "Normal", "Simulation", "SafeMode" });
            _atModeCombo.SelectedIndex = 0;

            var lblProfile = MakeLabel("Ayar Profili:", 12, 55);
            _atProfileCombo = new ComboBox
            {
                Location = new Point(130, 53),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            _atProfileCombo.Items.AddRange(new object[] { "Default", "Street", "Dyno" });
            _atProfileCombo.SelectedIndex = 0;

            var lblUser = MakeLabel("Kullanıcı Rolü:", 12, 90);
            var comboUser = new ComboBox
            {
                Location = new Point(130, 88),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            comboUser.Items.AddRange(new object[] { "Professional", "Advanced", "Beginner" });
            comboUser.SelectedIndex = 0;

            gbConfig.Controls.Add(lblMode);
            gbConfig.Controls.Add(_atModeCombo);
            gbConfig.Controls.Add(lblProfile);
            gbConfig.Controls.Add(_atProfileCombo);
            gbConfig.Controls.Add(lblUser);
            gbConfig.Controls.Add(comboUser);
            leftPanel.Controls.Add(gbConfig);

            var actionPanel = new Panel { Size = new Size(350, 48), Margin = new Padding(0, 0, 0, 16) };
            _btnAtStart = MakeButton("▶ Başlat", 0, 0, 100, 36, (s, e) =>
            {
                string userId = comboUser.SelectedItem.ToString() == "Advanced" ? "AdvancedUser" :
                                (comboUser.SelectedItem.ToString() == "Beginner" ? "BeginnerUser" : "TunerUser");
                var mode = (HondaTuner.Core.AutoTune.AutoTuneOperatingMode)Enum.Parse(typeof(HondaTuner.Core.AutoTune.AutoTuneOperatingMode), _atModeCombo.SelectedItem.ToString());
                string profile = _atProfileCombo.SelectedItem.ToString();

                string ecuId = _activeActiveProfileEcuId();
                if (_autoTuneEngine.StartSession(ecuId, userId, mode, profile))
                {
                    _btnAtStart.Enabled = false;
                    _btnAtPause.Enabled = true;
                    _btnAtStop.Enabled = true;
                    _atModeCombo.Enabled = false;
                    _atProfileCombo.Enabled = false;
                    comboUser.Enabled = false;
                    SetStatus($"AutoTune Oturumu Başlatıldı ({mode})");
                }
                else
                {
                    MessageBox.Show("AutoTune oturumu başlatılamadı. Kilit veya yetki yetersizliği olabilir.", "AutoTune", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            });
            _btnAtStart.BackColor = VtecGreen;
            _btnAtStart.ForeColor = Color.White;

            _btnAtPause = MakeButton("⏸ Duraklat", 110, 0, 100, 36, (s, e) =>
            {
                if (_autoTuneEngine.IsRunning)
                {
                    _autoTuneEngine.PauseSession();
                    _btnAtPause.Text = "▶ Devam Et";
                    SetStatus("AutoTune Oturumu Duraklatıldı");
                }
                else
                {
                    _autoTuneEngine.ResumeSession();
                    _btnAtPause.Text = "⏸ Duraklat";
                    SetStatus("AutoTune Oturumu Devam Ettiriliyor");
                }
            });
            _btnAtPause.Enabled = false;
            _btnAtPause.BackColor = AccentBlue;
            _btnAtPause.ForeColor = Color.White;

            _btnAtStop = MakeButton("⏹ Durdur", 220, 0, 100, 36, (s, e) =>
            {
                _autoTuneEngine.StopSession();
                _btnAtStart.Enabled = true;
                _btnAtPause.Enabled = false;
                _btnAtPause.Text = "⏸ Duraklat";
                _btnAtStop.Enabled = false;
                _atModeCombo.Enabled = true;
                _atProfileCombo.Enabled = true;
                comboUser.Enabled = true;
                SetStatus("AutoTune Oturumu Durduruldu");
            });
            _btnAtStop.Enabled = false;
            _btnAtStop.BackColor = AccentRed;
            _btnAtStop.ForeColor = Color.White;

            actionPanel.Controls.Add(_btnAtStart);
            actionPanel.Controls.Add(_btnAtPause);
            actionPanel.Controls.Add(_btnAtStop);
            leftPanel.Controls.Add(actionPanel);

            var gbStatus = new GroupBox
            {
                Text = "Canlı Durum ve Güvenlik Limitleri",
                ForeColor = TextPrimary,
                Size = new Size(350, 240),
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(12)
            };

            _lblAtStatus = MakeLabel("Durum: OFF", 12, 22);
            _lblAtUser = MakeLabel("Kullanıcı Rolü: Professional", 12, 52);
            _lblAtEcu = MakeLabel("ECU Bağlantısı: Yok", 12, 82);
            _lblAtSafety = MakeLabel("Güvenlik Durumu: SAFE", 12, 112);
            _lblAtSafety.ForeColor = VtecGreen;
            _lblAtSafety.Font = new Font(_lblAtSafety.Font, FontStyle.Bold);

            _lblAtKnock = MakeLabel("Knock Sayacı: 0", 12, 142);
            _lblAtEct = MakeLabel("ECT Sıcaklığı: 0 °C", 12, 172);
            _lblAtQuality = MakeLabel("Tuning Kalite Skoru: --", 12, 202);

            gbStatus.Controls.Add(_lblAtStatus);
            gbStatus.Controls.Add(_lblAtUser);
            gbStatus.Controls.Add(_lblAtEcu);
            gbStatus.Controls.Add(_lblAtSafety);
            gbStatus.Controls.Add(_lblAtKnock);
            gbStatus.Controls.Add(_lblAtEct);
            gbStatus.Controls.Add(_lblAtQuality);
            leftPanel.Controls.Add(gbStatus);

            var gbRtp = new GroupBox
            {
                Text = "RTP Real-Time Calibration & Emulator",
                ForeColor = TextPrimary,
                Size = new Size(350, 240),
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(12)
            };

            _btnRtpConnect = MakeButton("▶ Emulator Bağlan", 12, 20, 150, 28, (s, e) =>
            {
                try
                {
                    _rtpEngine.ConnectEmulator();
                    UpdateRtpStatusUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Emulator bağlantısı başarısız oldu: {ex.Message}", "RTP Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            _btnRtpConnect.BackColor = VtecGreen;
            _btnRtpConnect.ForeColor = Color.White;

            _btnRtpDisconnect = MakeButton("⏹ Bağlantıyı Kes", 172, 20, 150, 28, (s, e) =>
            {
                _rtpEngine.DisconnectEmulator();
                UpdateRtpStatusUI();
            });
            _btnRtpDisconnect.BackColor = AccentRed;
            _btnRtpDisconnect.ForeColor = Color.White;

            _chkRtpSyncEnabled = new CheckBox
            {
                Text = "Real-Time Calibration Sync Etkin",
                Location = new Point(12, 55),
                Size = new Size(300, 22),
                ForeColor = TextPrimary
            };
            _chkRtpSyncEnabled.CheckedChanged += (s, e) =>
            {
                if (_rtpEngine == null) return;
                bool act = _chkRtpSyncEnabled.Checked;
                if (act && !_rtpEngine.IsSyncActive)
                {
                    try
                    {
                        _rtpEngine.EnableSync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Senkronizasyon etkinleştirilemedi: {ex.Message}", "RTP Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        _chkRtpSyncEnabled.Checked = false;
                    }
                }
                else if (!act && _rtpEngine.IsSyncActive)
                {
                    _rtpEngine.DisableSync();
                }
                UpdateRtpStatusUI();
            };

            _btnRtpFullSync = MakeButton("🔄 Tüm ROM'u Senkronize Et (Upload)", 12, 85, 310, 30, (s, e) =>
            {
                try
                {
                    _rtpEngine.SyncFullCalibration();
                    UpdateRtpStatusUI();
                    MessageBox.Show("Tam kalibrasyon emulatöre başarıyla yüklendi ve doğrulandı.", "RTP Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tam kalibrasyon senkronizasyonu başarısız: {ex.Message}", "RTP Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
            _btnRtpFullSync.BackColor = AccentBlue;
            _btnRtpFullSync.ForeColor = Color.White;

            _lblRtpState = MakeLabel("Bağlantı Durumu: Disconnected", 12, 125);
            _lblRtpQueueDepth = MakeLabel("Kuyruk Derinliği: 0 eleman", 12, 145);
            _lblRtpAvgLatency = MakeLabel("Ortalama Gecikme: 0.0 ms", 12, 165);
            _lblRtpFailureCount = MakeLabel("Hata / Yeniden Deneme: 0 / 0", 12, 185);
            _lblRtpDroppedWrites = MakeLabel("Düşen Yazmalar: 0", 12, 205);

            gbRtp.Controls.Add(_btnRtpConnect);
            gbRtp.Controls.Add(_btnRtpDisconnect);
            gbRtp.Controls.Add(_chkRtpSyncEnabled);
            gbRtp.Controls.Add(_btnRtpFullSync);
            gbRtp.Controls.Add(_lblRtpState);
            gbRtp.Controls.Add(_lblRtpQueueDepth);
            gbRtp.Controls.Add(_lblRtpAvgLatency);
            gbRtp.Controls.Add(_lblRtpFailureCount);
            gbRtp.Controls.Add(_lblRtpDroppedWrites);

            leftPanel.Controls.Add(gbRtp);

            // Fetch initial state
            UpdateRtpStatusUI();

            mainLayout.Controls.Add(leftPanel, 0, 0);

            var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            var lblDecisions = new Label
            {
                Text = "Önerilen ve Uygulanan Kararlar",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextPrimary,
                Dock = DockStyle.Top,
                Height = 24
            };
            rightPanel.Controls.Add(lblDecisions);

            _atDecisionsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };
            _atDecisionsListView.Columns.Add("Zaman", 80);
            _atDecisionsListView.Columns.Add("Tip", 60);
            _atDecisionsListView.Columns.Add("Harita", 80);
            _atDecisionsListView.Columns.Add("Hücre [R, C]", 80);
            _atDecisionsListView.Columns.Add("Sapma", 80);
            _atDecisionsListView.Columns.Add("Düzeltme", 80);
            _atDecisionsListView.Columns.Add("Güven Skoru", 90);
            _atDecisionsListView.Columns.Add("Durum", 100);

            rightPanel.Controls.Add(_atDecisionsListView);
            mainLayout.Controls.Add(rightPanel, 1, 0);

            tab.Controls.Add(mainLayout);
        }

        private string _activeActiveProfileEcuId()
        {
            return _activeProfile?.EcuCode ?? "P28";
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return MakeLabel(text, new Font("Segoe UI", 9f), TextPrimary, new Point(x, y));
        }

        private Button MakeButton(string text, int x, int y, int w, int h, EventHandler handler)
        {
            var b = MakeButton(text, new Point(x, y), w, AccentBlue);
            b.Height = h;
            b.Click += handler;
            return b;
        }

        private void OnAutoTuneDomainEvent(HondaTuner.Core.AutoTune.IAutoTuneDomainEvent ev)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() => OnAutoTuneDomainEvent(ev)));
                    return;
                }

                if (ev.EventType == "SessionCreated")
                {
                    _lblAtStatus.Text = "Durum: Sürüyor";
                    _lblAtUser.Text = $"Kullanıcı Rolü: {ev.User}";
                    _lblAtEcu.Text = $"ECU Bağlantısı: {ev.EcuIdentifier}";
                    _lblAtSafety.Text = "Güvenlik Durumu: SAFE";
                    _lblAtSafety.ForeColor = VtecGreen;
                }
                else if (ev.EventType == "SessionStopped")
                {
                    _lblAtStatus.Text = "Durum: Durduruldu";
                    _lblAtSafety.Text = "Güvenlik Durumu: --";
                    _lblAtSafety.ForeColor = TextMuted;
                }
                else if (ev.EventType == "SessionPaused")
                {
                    _lblAtStatus.Text = "Durum: Askıda";
                }
                else if (ev.EventType == "SessionResumed")
                {
                    _lblAtStatus.Text = "Durum: Sürüyor";
                }
                else if (ev.EventType == "SafetyViolation")
                {
                    _lblAtSafety.Text = "Güvenlik Durumu: VIOLATION";
                    _lblAtSafety.ForeColor = AccentRed;
                    SetStatus($"[WARN] Güvenlik İhlali: {ev.Payload}");
                }
                else if (ev.EventType == "DecisionCreated" || ev.EventType == "SafetyValidated" || ev.EventType == "MapChangeApplied" || ev.EventType == "Committed")
                {
                    string timeStr = DateTime.Now.ToString("HH:mm:ss");
                    var item = new ListViewItem(timeStr);
                    item.SubItems.Add(ev.EventType == "Committed" ? "Commit" : "Tune");
                    item.SubItems.Add("Map");
                    item.SubItems.Add("[*,*]");
                    item.SubItems.Add("--");
                    item.SubItems.Add("--");
                    item.SubItems.Add("100%");
                    item.SubItems.Add(ev.Payload);

                    if (_atDecisionsListView != null)
                    {
                        _atDecisionsListView.Items.Insert(0, item);
                        if (_atDecisionsListView.Items.Count > 100)
                        {
                            _atDecisionsListView.Items.RemoveAt(100);
                        }
                    }

                    if (_autoTuneEngine != null && _autoTuneEngine.ActiveSession != null)
                    {
                        var qualityAnalyzer = new HondaTuner.Core.AutoTune.CalibrationQualityAnalyzer();
                        double qualScore = qualityAnalyzer.CalculateQualityScore(_autoTuneEngine.Memory);
                        if (_lblAtQuality != null)
                            _lblAtQuality.Text = $"Tuning Kalite Skoru: {qualScore:0.0}%";
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] OnAutoTuneDomainEvent UI güncelleme hatası: {ex.Message}"); }
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        private static decimal Clamp(decimal v, decimal min, decimal max) =>
            v < min ? min : v > max ? max : v;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _datalogMgr?.Disconnect();
            _programmer?.Disconnect();
            _ostrich?.Disconnect();
            _obdConn?.Disconnect();
            if (_rtpEngine != null)
            {
                _rtpEngine.OnRtpDomainEvent -= OnRtpDomainEvent;
            }
            if (_isDirty && !ConfirmDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        private void OnRtpDomainEvent(HondaTuner.Core.Rtp.IRtpDomainEvent ev)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke((Action)(() => OnRtpDomainEvent(ev)));
                    return;
                }

                string timeStr = DateTime.Now.ToString("HH:mm:ss");
                var item = new ListViewItem(timeStr);
                item.SubItems.Add("RTP");
                item.SubItems.Add("Sync");
                item.SubItems.Add("--");
                item.SubItems.Add("--");
                item.SubItems.Add("--");
                item.SubItems.Add("--");
                item.SubItems.Add(ev.Message);

                if (_atDecisionsListView != null)
                {
                    _atDecisionsListView.Items.Insert(0, item);
                    if (_atDecisionsListView.Items.Count > 100)
                    {
                        _atDecisionsListView.Items.RemoveAt(100);
                    }
                }

                UpdateRtpStatusUI();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainForm] OnRtpDomainEvent UI güncelleme hatası: {ex.Message}"); }
        }

        private void UpdateRtpStatusUI()
        {
            if (_rtpEngine == null) return;

            _lblRtpState.Text = $"Bağlantı Durumu: {_rtpEngine.ConnectionState}";
            switch (_rtpEngine.ConnectionState)
            {
                case Core.Rtp.RtpConnectionState.Disconnected:
                    _lblRtpState.ForeColor = TextMuted;
                    _btnRtpConnect.Enabled = true;
                    _btnRtpDisconnect.Enabled = false;
                    _chkRtpSyncEnabled.Enabled = false;
                    _chkRtpSyncEnabled.Checked = false;
                    _btnRtpFullSync.Enabled = false;
                    break;
                case Core.Rtp.RtpConnectionState.Connecting:
                    _lblRtpState.ForeColor = AccentBlue;
                    _btnRtpConnect.Enabled = false;
                    _btnRtpDisconnect.Enabled = true;
                    _chkRtpSyncEnabled.Enabled = false;
                    _btnRtpFullSync.Enabled = false;
                    break;
                case Core.Rtp.RtpConnectionState.Connected:
                    _lblRtpState.ForeColor = VtecGreen;
                    _btnRtpConnect.Enabled = false;
                    _btnRtpDisconnect.Enabled = true;
                    _chkRtpSyncEnabled.Enabled = true;
                    _chkRtpSyncEnabled.Checked = _rtpEngine.IsSyncActive;
                    _btnRtpFullSync.Enabled = _parser.IsLoaded;
                    break;
                case Core.Rtp.RtpConnectionState.Synchronizing:
                    _lblRtpState.ForeColor = VtecGreen;
                    _btnRtpConnect.Enabled = false;
                    _btnRtpDisconnect.Enabled = true;
                    _chkRtpSyncEnabled.Enabled = true;
                    _chkRtpSyncEnabled.Checked = true;
                    _btnRtpFullSync.Enabled = _parser.IsLoaded;
                    break;
                case Core.Rtp.RtpConnectionState.Paused:
                    _lblRtpState.ForeColor = AccentBlue;
                    _btnRtpConnect.Enabled = false;
                    _btnRtpDisconnect.Enabled = true;
                    _chkRtpSyncEnabled.Enabled = true;
                    _chkRtpSyncEnabled.Checked = false;
                    _btnRtpFullSync.Enabled = false;
                    break;
                case Core.Rtp.RtpConnectionState.Faulted:
                    _lblRtpState.ForeColor = AccentRed;
                    _btnRtpConnect.Enabled = true;
                    _btnRtpDisconnect.Enabled = false;
                    _chkRtpSyncEnabled.Enabled = false;
                    _chkRtpSyncEnabled.Checked = false;
                    _btnRtpFullSync.Enabled = false;
                    break;
            }

            _lblRtpQueueDepth.Text = $"Kuyruk Derinliği: {_rtpEngine.QueueDepth} eleman";
            _lblRtpAvgLatency.Text = $"Ortalama Gecikme: {_rtpEngine.AvgSyncLatencyMs:0.0} ms";
            _lblRtpFailureCount.Text = $"Hata / Yeniden Deneme: {_rtpEngine.FailureCount} / {_rtpEngine.RetryCount}";
            _lblRtpDroppedWrites.Text = $"Düşen Yazmalar: {_rtpEngine.DroppedWritesCount}";
        }
    }

    // ── Koyu Menü Renderer ───────────────────────────────────────
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color BgMenu = Color.FromArgb(22, 27, 34);
        private static readonly Color BgHover = Color.FromArgb(48, 54, 61);
        private static readonly Color AccentR = Color.FromArgb(233, 69, 96);

        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var rect = new Rectangle(Point.Empty, e.Item.Size);
            Color bg = e.Item.Selected ? BgHover : BgMenu;
            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, rect);
            if (e.Item.Selected)
            {
                using var pen = new Pen(AccentR, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
        }
    }

    internal class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color BgMenu = Color.FromArgb(22, 27, 34);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);

        public override Color MenuItemSelected => Color.FromArgb(48, 54, 61);
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
        public override Color ToolStripDropDownBackground => BgMenu;
        public override Color ImageMarginGradientBegin => BgMenu;
        public override Color ImageMarginGradientMiddle => BgMenu;
        public override Color ImageMarginGradientEnd => BgMenu;
        public override Color MenuStripGradientBegin => BgMenu;
        public override Color MenuStripGradientEnd => BgMenu;
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(48, 54, 61);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(48, 54, 61);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(33, 38, 45);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(33, 38, 45);
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
