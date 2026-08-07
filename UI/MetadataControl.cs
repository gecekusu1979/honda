using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HondaTuner.Core;
using HondaTuner.Core.Interfaces;
using HondaTuner.Core.Metadata;
using HondaTuner.Core.Rom;

namespace HondaTuner.UI
{
    public class MetadataControl : UserControl
    {
        private IRomService _romService;
        private EcuMetadata _metadata;

        // UI elementleri - Sol Panel (Meta Bilgiler)
        private TextBox _txtSerial;
        private TextBox _txtHardware;
        private TextBox _txtVin;
        private TextBox _txtChassis;
        private NumericUpDown _numCR;
        private ComboBox _cmbCams;
        private ComboBox _cmbGearbox;
        private ComboBox _cmbInduction;

        // UI elementleri - Sağ Panel (Doğrulama ve Pinout)
        private TabControl _tabRight;
        private TabPage _tabValidation;
        private TabPage _tabPinout;
        private ListBox _lstValidation;
        private DataGridView _gridPinout;
        private ComboBox _cmbPinConnectorFilter;
        private TextBox _txtPinSearch;

        // Tasarım renkleri (MainForm ile uyumlu)
        private static readonly Color BgPanel = Color.FromArgb(20, 24, 30);
        private static readonly Color BgCard = Color.FromArgb(28, 33, 41);
        private static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);
        private static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
        private static readonly Color AccentGreen = Color.FromArgb(57, 219, 109);
        private static readonly Color AccentRed = Color.FromArgb(255, 123, 114);

        public event EventHandler MetadataChanged;

        public MetadataControl()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        public void BindRomService(IRomService romService)
        {
            _romService = romService;
            if (_romService != null)
            {
                _metadata = _romService.Metadata ?? new EcuMetadata();
                UpdateUiFromMetadata();
                RunValidation();
            }
        }

        private void InitializeComponent()
        {
            BackColor = BgPanel;
            ForeColor = TextPrimary;
            Dock = DockStyle.Fill;
            Font = new Font("Segoe UI", 9f);

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 380,
                BorderStyle = BorderStyle.None
            };
            Controls.Add(splitContainer);

            // ── SOL PANEL (GİRİŞ ALANLARI) ──
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgCard,
                Padding = new Padding(15)
            };
            splitContainer.Panel1.Controls.Add(pnlLeft);

            var lblLeftHeader = new Label
            {
                Text = "📝 PROJE VE MOTOR META VERİLERİ",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(15, 15),
                Size = new Size(350, 25),
                ForeColor = AccentBlue
            };
            pnlLeft.Controls.Add(lblLeftHeader);

            int y = 55;

            // Seri No
            AddLabel(pnlLeft, "ECU Seri Numarası:", 15, y);
            _txtSerial = AddTextBox(pnlLeft, 185, y, 160);
            _txtSerial.TextChanged += OnFieldChanged;
            y += 35;

            // Donanım Rev
            AddLabel(pnlLeft, "Hardware Revizyonu:", 15, y);
            _txtHardware = AddTextBox(pnlLeft, 185, y, 160);
            _txtHardware.TextChanged += OnFieldChanged;
            y += 35;

            // VIN
            AddLabel(pnlLeft, "Şasi Numarası (VIN):", 15, y);
            _txtVin = AddTextBox(pnlLeft, 185, y, 160);
            _txtVin.TextChanged += OnFieldChanged;
            y += 35;

            // Kasa Kodu
            AddLabel(pnlLeft, "Kasa Kodu (Örn: EG6, EK4):", 15, y);
            _txtChassis = AddTextBox(pnlLeft, 185, y, 160);
            _txtChassis.TextChanged += OnFieldChanged;
            y += 35;

            // CR (Sıkıştırma Oranı)
            AddLabel(pnlLeft, "Sıkıştırma Oranı (:1):", 15, y);
            _numCR = new NumericUpDown
            {
                Location = new Point(185, y),
                Width = 160,
                Minimum = 7.0m,
                Maximum = 16.0m,
                DecimalPlaces = 1,
                Increment = 0.1m,
                Value = 9.2m,
                BackColor = BgPanel,
                ForeColor = TextPrimary
            };
            _numCR.ValueChanged += OnFieldChanged;
            pnlLeft.Controls.Add(_numCR);
            y += 35;

            // Eksantrik
            AddLabel(pnlLeft, "Eksantrik Profili:", 15, y);
            _cmbCams = AddComboBox(pnlLeft, 185, y, 160, new[] { "OEM", "Stage 1", "Stage 2", "Stage 3" });
            _cmbCams.SelectedIndexChanged += OnFieldChanged;
            y += 35;

            // Şanzıman
            AddLabel(pnlLeft, "Şanzıman Tipi:", 15, y);
            _cmbGearbox = AddComboBox(pnlLeft, 185, y, 160, new[] { "S40 (EG DX/LX)", "S80 (B18C)", "Y21 (B16A)", "Custom" });
            _cmbGearbox.SelectedIndexChanged += OnFieldChanged;
            y += 35;

            // Aşırı Besleme
            AddLabel(pnlLeft, "İndüksiyon Türü:", 15, y);
            _cmbInduction = AddComboBox(pnlLeft, 185, y, 160, new[] { "N/A", "Turbo", "Supercharger" });
            _cmbInduction.SelectedIndexChanged += OnFieldChanged;
            y += 45;

            // Kaydet Butonu
            var btnSaveMeta = new Button
            {
                Text = "💾 Değişiklikleri Kaydet",
                Location = new Point(185, y),
                Size = new Size(160, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 134, 54),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnSaveMeta.FlatAppearance.BorderSize = 0;
            btnSaveMeta.Click += (s, e) =>
            {
                SaveToMetadata();
                if (_romService != null)
                {
                    _romService.Metadata = _metadata;
                    _romService.SaveMetadata(_romService.FilePath);
                    MessageBox.Show("Proje meta verileri başarıyla kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            pnlLeft.Controls.Add(btnSaveMeta);

            // ── SAĞ PANEL (DOĞRULAMA VE PINOUT HARİTASI) ──
            _tabRight = new TabControl
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel
            };
            splitContainer.Panel2.Controls.Add(_tabRight);

            // Tab 1: Doğrulama Sonuçları
            _tabValidation = new TabPage
            {
                Text = "⚡ Canlı Analiz",
                BackColor = BgCard
            };
            _tabRight.TabPages.Add(_tabValidation);

            _lstValidation = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI Semibold", 9.5f),
                ItemHeight = 22,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _lstValidation.DrawItem += LstValidation_DrawItem;
            _tabValidation.Controls.Add(_lstValidation);

            // Tab 2: Pinout Tablosu
            _tabPinout = new TabPage
            {
                Text = "🔌 OBD1 ECU Pinout",
                BackColor = BgCard
            };
            _tabRight.TabPages.Add(_tabPinout);

            var pnlPinoutControls = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = BgCard,
                Padding = new Padding(5)
            };
            _tabPinout.Controls.Add(pnlPinoutControls);

            var lblSearch = new Label
            {
                Text = "Ara:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(10, 14),
                Size = new Size(35, 20),
                ForeColor = TextMuted
            };
            pnlPinoutControls.Controls.Add(lblSearch);

            _txtPinSearch = new TextBox
            {
                Location = new Point(45, 10),
                Width = 140,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtPinSearch.TextChanged += (s, e) => FilterPinoutGrid();
            pnlPinoutControls.Controls.Add(_txtPinSearch);

            var lblConnector = new Label
            {
                Text = "Soket:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(200, 14),
                Size = new Size(50, 20),
                ForeColor = TextMuted
            };
            pnlPinoutControls.Controls.Add(lblConnector);

            _cmbPinConnectorFilter = new ComboBox
            {
                Location = new Point(255, 10),
                Width = 100,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbPinConnectorFilter.Items.AddRange(new[] { "Hepsi", "Soket A", "Soket B", "Soket D" });
            _cmbPinConnectorFilter.SelectedIndex = 0;
            _cmbPinConnectorFilter.SelectedIndexChanged += (s, e) => FilterPinoutGrid();
            pnlPinoutControls.Controls.Add(_cmbPinConnectorFilter);

            _gridPinout = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = BgCard,
                ForeColor = TextPrimary,
                GridColor = Border,
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            };
            _gridPinout.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgPanel,
                ForeColor = AccentBlue,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _gridPinout.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                SelectionBackColor = Color.FromArgb(38, 48, 60),
                SelectionForeColor = TextPrimary
            };

            // Grid Kolonları
            _gridPinout.Columns.Add("PinNumber", "Pin");
            _gridPinout.Columns.Add("Symbol", "Sembol");
            _gridPinout.Columns.Add("SignalType", "Sinyal Türü");
            _gridPinout.Columns.Add("WiringColor", "Kablo Rengi");
            _gridPinout.Columns.Add("Description", "Açıklama");

            _gridPinout.Columns[0].Width = 45;
            _gridPinout.Columns[1].Width = 70;
            _gridPinout.Columns[2].Width = 85;
            _gridPinout.Columns[3].Width = 90;
            _gridPinout.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _gridPinout.Columns[4].MinimumWidth = 240;

            _tabPinout.Controls.Add(_gridPinout);

            // Pinout verisini yükle
            LoadPinoutGrid();
        }

        private void AddLabel(Panel parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y + 4),
                Size = new Size(165, 18),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
        }

        private TextBox AddTextBox(Panel parent, int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(txt);
            return txt;
        }

        private ComboBox AddComboBox(Panel parent, int x, int y, int width, string[] items)
        {
            var cmb = new ComboBox
            {
                Location = new Point(x, y),
                Width = width,
                BackColor = BgPanel,
                ForeColor = TextPrimary,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmb.Items.AddRange(items);
            cmb.SelectedIndex = 0;
            parent.Controls.Add(cmb);
            return cmb;
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            MetadataChanged?.Invoke(this, EventArgs.Empty);
            SaveToMetadata();
            RunValidation();
        }

        private void SaveToMetadata()
        {
            if (_metadata == null) _metadata = new EcuMetadata();

            _metadata.SerialNumber = _txtSerial.Text;
            _metadata.HardwareRevision = _txtHardware.Text;
            _metadata.Vin = _txtVin.Text;
            _metadata.Chassis = _txtChassis.Text;
            _metadata.CompressionRatio = (double)_numCR.Value;
            _metadata.CamshaftProfile = _cmbCams.SelectedItem?.ToString() ?? "OEM";
            _metadata.GearboxType = _cmbGearbox.SelectedItem?.ToString() ?? "S40 SOHC";
            _metadata.InductionType = _cmbInduction.SelectedItem?.ToString() ?? "N/A";
        }

        private void UpdateUiFromMetadata()
        {
            if (_metadata == null) return;

            _txtSerial.Text = _metadata.SerialNumber;
            _txtHardware.Text = _metadata.HardwareRevision;
            _txtVin.Text = _metadata.Vin;
            _txtChassis.Text = _metadata.Chassis;
            _numCR.Value = (decimal)_metadata.CompressionRatio;

            SelectComboItem(_cmbCams, _metadata.CamshaftProfile);
            SelectComboItem(_cmbGearbox, _metadata.GearboxType);
            SelectComboItem(_cmbInduction, _metadata.InductionType);
        }

        private void SelectComboItem(ComboBox cmb, string value)
        {
            if (value == null) return;
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i].ToString().StartsWith(value, StringComparison.OrdinalIgnoreCase))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }
        }

        public void RunValidation()
        {
            if (_metadata == null || _romService == null) return;

            _lstValidation.Items.Clear();

            int revLmtValue = 7200;
            int[] loadAx = null;
            try
            {
                var parser = (_romService as RomService)?.GetParser();
                if (parser != null && parser.IsLoaded)
                {
                    revLmtValue = parser.ReadRevLimit();
                    loadAx = parser.Profile?.LoadAxis;
                }
            }
            catch { }

            var validationResults = EcuMetadataValidator.Validate(_metadata, _romService.Profile, revLmtValue, loadAx);

            if (validationResults.Count == 0)
            {
                _lstValidation.Items.Add("✅ Analiz Sonucu: Herhangi bir çelişki veya uyarı bulunamadı. Kurulum kararlı.");
            }
            else
            {
                foreach (var r in validationResults)
                {
                    _lstValidation.Items.Add(r);
                }
            }
        }

        private void LstValidation_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            var item = _lstValidation.Items[e.Index];
            string text = item.ToString();
            Color foreColor = TextPrimary;

            if (item is MetadataValidationResult r)
            {
                text = $"• [{r.Level}] {r.Message}";
                if (r.Level == ValidationLevel.Error) foreColor = AccentRed;
                else if (r.Level == ValidationLevel.Warning) foreColor = Color.FromArgb(242, 203, 97);
                else foreColor = AccentBlue;
            }

            using (var brush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void LoadPinoutGrid()
        {
            FilterPinoutGrid();
        }

        private void FilterPinoutGrid()
        {
            _gridPinout.Rows.Clear();
            var manager = EcuPinoutManager.Instance;
            var pins = manager.GetAllPins();

            string search = _txtPinSearch.Text.Trim();
            string selectedConn = _cmbPinConnectorFilter.SelectedItem?.ToString() ?? "Hepsi";
            string connTarget = "";
            if (selectedConn.EndsWith("A")) connTarget = "A";
            else if (selectedConn.EndsWith("B")) connTarget = "B";
            else if (selectedConn.EndsWith("D")) connTarget = "D";

            foreach (var pin in pins)
            {
                if (!string.IsNullOrEmpty(connTarget) && !pin.Connector.Equals(connTarget, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(search))
                {
                    bool match = pin.PinNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 pin.Symbol.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 pin.Description.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 pin.WiringColor.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                _gridPinout.Rows.Add(pin.PinNumber, pin.Symbol, pin.SignalType, pin.WiringColor, pin.Description);
            }
        }
    }
}
