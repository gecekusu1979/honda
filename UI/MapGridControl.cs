using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HondaTuner.Core;

namespace HondaTuner.UI
{
    /// <summary>
    /// Gelişmiş harita editörü:
    /// - 5 renk heatmap
    /// - Hover tooltip
    /// - Çoklu hücre seçimi + ContextMenu (Ekle/Çıkar, Yüzde, Enterpolasyon)
    /// - Klavye kısayolları (+/-)
    /// - Cell Tracing (canlı vurgulama)
    /// - 3D yüzey grafiği ile senkronizasyon
    /// </summary>
    public class MapGridControl : UserControl
    {
        private DataGridView _grid;
        private byte[,] _data;
        private string _mapName;
        private EcuProfile _profile;
        private ToolTip _tooltip;
        private int _lastTipRow = -1;
        private int _lastTipCol = -1;

        // Cell Tracing
        private int _traceRow = -1;
        private int _traceCol = -1;
        private System.Collections.Generic.List<Tuple<int, int>> _traceNeighbors = new System.Collections.Generic.List<Tuple<int, int>>();
        private System.Windows.Forms.Timer _traceBlinkTimer;
        private bool _traceBlink = false;

        // 3D grafik referansı (senkronizasyon için)
        public SurfaceChart3D LinkedChart3D { get; set; }

        public event EventHandler DataChanged;

        // ── Renk Paleti ─────────────────────────────────────────
        private static readonly Color BgGrid = Color.FromArgb(22, 27, 34);
        private static readonly Color BgHeader = Color.FromArgb(33, 38, 45);
        private static readonly Color TextLight = Color.FromArgb(230, 237, 243);
        private static readonly Color TextDark = Color.FromArgb(13, 17, 23);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);
        private static readonly Color TraceColor = Color.FromArgb(180, 0, 212, 255);

        public MapGridControl()
        {
            _tooltip = new ToolTip { ShowAlways = true, AutoPopDelay = 4000, InitialDelay = 400 };

            // Blink timer (cell tracing animasyonu)
            _traceBlinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _traceBlinkTimer.Tick += (s, e) =>
            {
                _traceBlink = !_traceBlink;
                if (_traceRow >= 0 && _traceCol >= 0)
                {
                    _grid.InvalidateCell(_traceCol, _traceRow);
                    foreach (var n in _traceNeighbors)
                    {
                        _grid.InvalidateCell(n.Item2, n.Item1);
                    }
                }
            };

            InitializeGrid(EcuProfiles.P28);
            BuildContextMenu();
        }

        // ── Veri Yükle ───────────────────────────────────────────

        public void LoadMap(byte[,] data, string mapName, EcuProfile profile = null)
        {
            _data = (byte[,])data.Clone();
            _mapName = mapName;

            bool profileChanged = profile != null && profile != _profile;
            if (profileChanged)
            {
                _profile = profile;
                RebuildGrid();
            }
            RefreshGrid();
            LinkedChart3D?.SetData(_data, _mapName);
        }

        public byte[,] GetData() => (byte[,])_data.Clone();

        public void SetData(byte[,] data)
        {
            if (data == null) return;
            _data = (byte[,])data.Clone();
            RefreshGrid();
            DataChanged?.Invoke(this, EventArgs.Empty);
            LinkedChart3D?.SetData(_data, _mapName);
        }

        // ── Cell Tracing API ─────────────────────────────────────

        /// <summary>
        /// Verilen RPM ve yük değeri için en yakın hücreyi vurgula.
        /// </summary>
        public void SetTraceCell(double rpm, double load)
        {
            if (_profile == null || _data == null) return;

            var result = HondaTuner.Core.Algorithms.VisualTraceEngine.TrackCell(rpm, load, _profile.RpmAxis, _profile.LoadAxis);

            bool changed = result.ActiveRow != _traceRow || result.ActiveCol != _traceCol;
            _traceRow = result.ActiveRow;
            _traceCol = result.ActiveCol;
            _traceNeighbors = result.Neighbors;

            if (changed)
            {
                _grid.ClearSelection();
                if (_traceRow >= 0 && _traceRow < _grid.RowCount &&
                    _traceCol >= 0 && _traceCol < _grid.ColumnCount)
                {
                    _grid.CurrentCell = _grid.Rows[_traceRow].Cells[_traceCol];
                }
                LinkedChart3D?.SetActiveCell(_traceRow, _traceCol);
            }
        }

        public void StartTracing() => _traceBlinkTimer.Start();
        public void StopTracing()
        {
            _traceBlinkTimer.Stop();
            _traceRow = -1;
            _traceCol = -1;
            _traceNeighbors.Clear();
            _traceBlink = false;
            _grid.Invalidate();
            LinkedChart3D?.SetActiveCell(-1, -1);
        }

        private static int FindNearestIndex(int[] axis, int target)
        {
            int best = 0;
            int bestDist = int.MaxValue;
            for (int i = 0; i < axis.Length; i++)
            {
                int d = Math.Abs(axis[i] - target);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        // ── Context Menu (M3) ────────────────────────────────────

        private void BuildContextMenu()
        {
            var ctx = new ContextMenuStrip();

            var addItem = new ToolStripMenuItem("➕  Değer Ekle/Çıkar…");
            var pctItem = new ToolStripMenuItem("📊  Yüzdesel Çarpan Uygula…");
            var interpItem = new ToolStripMenuItem("🔗  Hücreleri Enterpole Et");
            var sep1 = new ToolStripSeparator();
            var plus1Item = new ToolStripMenuItem("+1  Seçili hücrelere +1 ekle");
            var min1Item = new ToolStripMenuItem("−1  Seçili hücrelere −1 çıkar");

            addItem.Click += OnBulkAddSubtract;
            pctItem.Click += OnPercentScale;
            interpItem.Click += OnInterpolate;
            plus1Item.Click += (s, e) => ApplyDeltaToSelection(1);
            min1Item.Click += (s, e) => ApplyDeltaToSelection(-1);

            ctx.Items.AddRange(new ToolStripItem[]
            { addItem, pctItem, sep1, interpItem, sep1, plus1Item, min1Item });

            _grid.ContextMenuStrip = ctx;
        }

        private void OnBulkAddSubtract(object sender, EventArgs e)
        {
            using var dlg = new BulkEditDialog("Toplu Değer Ekle/Çıkar",
                "Eklenecek/çıkarılacak değeri girin (örn: +10 veya -5):", "0");
            if (dlg.ShowDialog() != DialogResult.OK) return;
            if (!int.TryParse(dlg.Result, out int delta)) return;
            ApplyDeltaToSelection(delta);
        }

        private void OnPercentScale(object sender, EventArgs e)
        {
            using var dlg = new BulkEditDialog("Yüzdesel Çarpan",
                "Yüzde değeri girin (örn: 5 = %5 artır, -3 = %3 azalt):", "0");
            if (dlg.ShowDialog() != DialogResult.OK) return;
            if (!double.TryParse(dlg.Result, out double pct)) return;
            ApplyPercentToSelection(pct / 100.0);
        }

        private void OnInterpolate(object sender, EventArgs e)
        {
            var cells = _grid.SelectedCells;
            if (cells.Count < 4)
            {
                MessageBox.Show("En az 4 hücre seçin (köşe noktaları için).",
                    "Enterpolasyon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Seçili hücrelerin sınırlarını bul
            int rMin = int.MaxValue, rMax = int.MinValue;
            int cMin = int.MaxValue, cMax = int.MinValue;
            foreach (DataGridViewCell cell in cells)
            {
                if (cell.RowIndex < rMin) rMin = cell.RowIndex;
                if (cell.RowIndex > rMax) rMax = cell.RowIndex;
                if (cell.ColumnIndex < cMin) cMin = cell.ColumnIndex;
                if (cell.ColumnIndex > cMax) cMax = cell.ColumnIndex;
            }

            if (rMax == rMin || cMax == cMin)
            {
                MessageBox.Show("Enterpolasyon için en az 2 satır ve 2 sütun seçin.",
                    "Enterpolasyon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 4 köşe değerlerini al
            double v00 = _data[rMin, cMin];
            double v01 = _data[rMin, cMax];
            double v10 = _data[rMax, cMin];
            double v11 = _data[rMax, cMax];

            // Bilinear interpolasyon
            for (int r = rMin; r <= rMax; r++)
            {
                double ty = (rMax == rMin) ? 0 : (double)(r - rMin) / (rMax - rMin);
                for (int c = cMin; c <= cMax; c++)
                {
                    // Köşeler korunur
                    if ((r == rMin || r == rMax) && (c == cMin || c == cMax)) continue;
                    double tx = (cMax == cMin) ? 0 : (double)(c - cMin) / (cMax - cMin);
                    double val = v00 * (1 - tx) * (1 - ty)
                               + v01 * tx * (1 - ty)
                               + v10 * (1 - tx) * ty
                               + v11 * tx * ty;
                    _data[r, c] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(val)));
                }
            }

            RefreshGrid();
            DataChanged?.Invoke(this, EventArgs.Empty);
            LinkedChart3D?.SetData(_data, _mapName);
        }

        // ── Toplu Düzenlemeler ───────────────────────────────────

        private void ApplyDeltaToSelection(int delta)
        {
            var cells = _grid.SelectedCells;
            if (cells.Count == 0) return;
            foreach (DataGridViewCell cell in cells)
            {
                int r = cell.RowIndex, c = cell.ColumnIndex;
                int newVal = Math.Max(0, Math.Min(255, _data[r, c] + delta));
                _data[r, c] = (byte)newVal;
                cell.Value = _data[r, c];
            }
            DataChanged?.Invoke(this, EventArgs.Empty);
            LinkedChart3D?.SetData(_data, _mapName);
        }

        private void ApplyPercentToSelection(double pct)
        {
            var cells = _grid.SelectedCells;
            if (cells.Count == 0) return;
            foreach (DataGridViewCell cell in cells)
            {
                int r = cell.RowIndex, c = cell.ColumnIndex;
                int newVal = (int)Math.Round(_data[r, c] * (1.0 + pct));
                newVal = Math.Max(0, Math.Min(255, newVal));
                _data[r, c] = (byte)newVal;
                cell.Value = _data[r, c];
            }
            DataChanged?.Invoke(this, EventArgs.Empty);
            LinkedChart3D?.SetData(_data, _mapName);
        }

        // ── Klavye Kısayolları ───────────────────────────────────

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_data == null) return base.ProcessCmdKey(ref msg, keyData);

            if (keyData == Keys.Oemplus || keyData == (Keys.Shift | Keys.Oemplus))
            { ApplyDeltaToSelection(1); return true; }
            if (keyData == Keys.OemMinus)
            { ApplyDeltaToSelection(-1); return true; }
            if (keyData == (Keys.Shift | Keys.OemMinus))
            { ApplyDeltaToSelection(-1); return true; }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Grid Kurulum ─────────────────────────────────────────

        private void InitializeGrid(EcuProfile profile)
        {
            _profile = profile;

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                RowHeadersWidth = 70,
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                ColumnHeadersHeight = 28,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9.5f),
                BackgroundColor = BgGrid,
                GridColor = Border,
                BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EditMode = DataGridViewEditMode.EditOnKeystroke,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = BgGrid,
                    ForeColor = TextLight,
                    SelectionBackColor = Color.FromArgb(88, 166, 255),
                    SelectionForeColor = TextDark,
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = BgHeader,
                    ForeColor = Color.FromArgb(139, 148, 158),
                    Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    SelectionBackColor = BgHeader,
                    SelectionForeColor = TextLight,
                },
                RowHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = BgHeader,
                    ForeColor = Color.FromArgb(139, 148, 158),
                    Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    SelectionBackColor = BgHeader,
                    SelectionForeColor = TextLight,
                },
            };

            BuildColumns();
            BuildRows();

            _grid.CellEndEdit += OnCellEdit;
            _grid.CellFormatting += OnCellFormatting;
            _grid.CellMouseMove += OnCellMouseMove;

            Controls.Add(_grid);
        }

        private void BuildColumns()
        {
            _grid.Columns.Clear();
            for (int c = 0; c < _profile.FuelMapCols; c++)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = $"{_profile.LoadAxis[c]}",
                    Width = 46,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                });
            }
        }

        private void BuildRows()
        {
            _grid.Rows.Clear();
            for (int r = 0; r < _profile.FuelMapRows; r++)
                _grid.Rows.Add();

            for (int r = 0; r < _grid.Rows.Count; r++)
            {
                _grid.Rows[r].HeaderCell.Value = $"{_profile.RpmAxis[r]}";
                _grid.Rows[r].Height = 25;
            }
        }

        public void RebuildGrid()
        {
            _grid.SuspendLayout();
            BuildColumns();
            BuildRows();
            _grid.ResumeLayout();
        }

        private void RefreshGrid()
        {
            if (_data == null) return;
            int rows = _data.GetLength(0);
            int cols = _data.GetLength(1);
            _grid.SuspendLayout();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    _grid.Rows[r].Cells[c].Value = _data[r, c];
            _grid.ResumeLayout();
        }

        // ── Hücre Düzenleme ──────────────────────────────────────

        private void OnCellEdit(object sender, DataGridViewCellEventArgs e)
        {
            var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (byte.TryParse(cell.Value?.ToString(), out byte val))
            {
                _data[e.RowIndex, e.ColumnIndex] = val;
                DataChanged?.Invoke(this, EventArgs.Empty);
                LinkedChart3D?.SetData(_data, _mapName);
            }
            else
            {
                cell.Value = _data[e.RowIndex, e.ColumnIndex];
            }
        }

        // ── Hover Tooltip ────────────────────────────────────────

        private void OnCellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.RowIndex == _lastTipRow && e.ColumnIndex == _lastTipCol) return;
            _lastTipRow = e.RowIndex;
            _lastTipCol = e.ColumnIndex;

            if (_data == null) return;
            byte val = _data[e.RowIndex, e.ColumnIndex];
            int rpm = (e.RowIndex < _profile.RpmAxis.Length) ? _profile.RpmAxis[e.RowIndex] : 0;
            int load = (e.ColumnIndex < _profile.LoadAxis.Length) ? _profile.LoadAxis[e.ColumnIndex] : 0;
            string tip = $"{rpm} RPM @ {load} kPa  →  {val}  (0x{val:X2})";
            _tooltip.SetToolTip(_grid, tip);
        }

        // ── Renklendirme (Cell Tracing + Heatmap) ───────────────

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;
            if (!byte.TryParse(e.Value.ToString(), out byte val)) return;

            // Cell Tracing vurgusu: Aktif Hücre (Blinking Green)
            if (e.RowIndex == _traceRow && e.ColumnIndex == _traceCol)
            {
                e.CellStyle.BackColor = _traceBlink
                    ? Color.FromArgb(63, 185, 80) // VtecGreen
                    : Color.FromArgb(45, 63, 185, 80);
                e.CellStyle.ForeColor = _traceBlink ? TextDark : TextLight;
                return;
            }

            // Yakın Hücreler (Gold / Sarı)
            bool isNeighbor = false;
            foreach (var n in _traceNeighbors)
            {
                if (n.Item1 == e.RowIndex && n.Item2 == e.ColumnIndex)
                {
                    isNeighbor = true;
                    break;
                }
            }
            if (isNeighbor)
            {
                e.CellStyle.BackColor = Color.FromArgb(241, 196, 15); // Gold / Yellow
                e.CellStyle.ForeColor = TextDark;
                return;
            }

            e.CellStyle.BackColor = GetHeatColor(val);
            float lum = (0.299f * e.CellStyle.BackColor.R +
                         0.587f * e.CellStyle.BackColor.G +
                         0.114f * e.CellStyle.BackColor.B) / 255f;
            e.CellStyle.ForeColor = lum > 0.45f ? TextDark : TextLight;
        }

        private static Color GetHeatColor(byte v)
        {
            Color[] stops =
            {
                Color.FromArgb(26,  35, 126),
                Color.FromArgb( 2, 136, 209),
                Color.FromArgb( 0, 200,  83),
                Color.FromArgb(255, 111,  0),
                Color.FromArgb(183,  28,  28),
            };
            float t = v / 255f * (stops.Length - 1);
            int i = Math.Min((int)t, stops.Length - 2);
            float u = t - i;
            Color a = stops[i], b = stops[i + 1];
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * u),
                (int)(a.G + (b.G - a.G) * u),
                (int)(a.B + (b.B - a.B) * u));
        }
    }

    // ── Toplu Düzenleme Giriş Penceresi ─────────────────────────

    internal class BulkEditDialog : Form
    {
        public string Result { get; private set; }

        private TextBox _input;

        public BulkEditDialog(string title, string prompt, string defaultVal)
        {
            Text = title;
            Size = new Size(340, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(33, 38, 45);

            var lbl = new Label
            {
                Text = prompt,
                ForeColor = Color.FromArgb(230, 237, 243),
                Location = new Point(12, 12),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 9f),
            };

            _input = new TextBox
            {
                Text = defaultVal,
                Location = new Point(12, 48),
                Width = 200,
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(22, 27, 34),
                ForeColor = Color.FromArgb(230, 237, 243),
            };
            _input.SelectAll();

            var ok = new Button
            {
                Text = "Uygula",
                DialogResult = DialogResult.OK,
                Location = new Point(12, 80),
                Width = 90,
                BackColor = Color.FromArgb(88, 166, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            ok.Click += (s, e) => Result = _input.Text.Trim();

            var cancel = new Button
            {
                Text = "İptal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(110, 80),
                Width = 70,
                BackColor = Color.FromArgb(48, 54, 61),
                ForeColor = Color.FromArgb(230, 237, 243),
                FlatStyle = FlatStyle.Flat,
            };

            AcceptButton = ok;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { lbl, _input, ok, cancel });
        }
    }
}
