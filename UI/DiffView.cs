using System;
using System.Drawing;
using System.Windows.Forms;
using HondaTuner.Core;

namespace HondaTuner.UI
{
    /// <summary>
    /// Stock ROM vs Modified ROM fark görünümü.
    /// Profil bağımlı eksen değerleri kullanır.
    /// Sol = Stock | Orta = Delta | Sağ = Modified
    /// </summary>
    public class DiffView : UserControl
    {
        private byte[,] _stock;
        private byte[,] _modified;
        private string _mapName = "Harita";
        private int[] _rpmAxis;
        private int[] _loadAxis;
        private int _rows = 16, _cols = 16;

        private TableLayoutPanel _layout;
        private DataGridView _gridStock;
        private DataGridView _gridDiff;
        private DataGridView _gridMod;
        private Panel _summaryPanel;
        private Label _lblChanged, _lblAvg, _lblMax, _lblTitle;

        private static readonly Color C_Bg = Color.FromArgb(22, 27, 34);
        private static readonly Color C_Header = Color.FromArgb(33, 38, 45);
        private static readonly Color C_Inc = Color.FromArgb(180, 40, 40);
        private static readonly Color C_Dec = Color.FromArgb(30, 100, 180);
        private static readonly Color C_Neutral = Color.FromArgb(38, 38, 50);

        public DiffView()
        {
            BackColor = C_Bg;
            Dock = DockStyle.Fill;
            BuildLayout();
        }

        // ── Veri ────────────────────────────────────────────────────

        public void Compare(byte[,] stock, byte[,] modified,
                            string mapName = "Harita",
                            int[] rpmAxis = null, int[] loadAxis = null)
        {
            _stock = (byte[,])stock.Clone();
            _modified = (byte[,])modified.Clone();
            _mapName = mapName;
            _rows = stock.GetLength(0);
            _cols = stock.GetLength(1);
            _rpmAxis = rpmAxis;
            _loadAxis = loadAxis;

            RebuildGridStructure();
            PopulateAll();
            UpdateSummary();
            _lblTitle.Text = $"📊 Diff: {mapName}  —  Stock vs Modified";
        }

        // ── Layout ──────────────────────────────────────────────────

        private void BuildLayout()
        {
            _lblTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(28, 35, 50),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "Diff görünümü — önce bir ROM yükleyin",
            };

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = C_Bg,
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));

            _gridStock = BuildGrid(Color.FromArgb(35, 38, 50));
            _gridDiff = BuildGrid(Color.FromArgb(25, 25, 35));
            _gridMod = BuildGrid(Color.FromArgb(28, 42, 65));

            _layout.Controls.Add(Wrap(_gridStock, "📋 STOCK", Color.FromArgb(55, 60, 80)), 0, 0);
            _layout.Controls.Add(Wrap(_gridDiff, "⚡ DELTA (Δ)", Color.FromArgb(60, 40, 40)), 1, 0);
            _layout.Controls.Add(Wrap(_gridMod, "✏️ MODIFIED", Color.FromArgb(28, 52, 90)), 2, 0);

            _summaryPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(18, 22, 30),
            };
            _lblChanged = SumLabel("Değişen: —", 10);
            _lblAvg = SumLabel("Ort. Δ: —", 200);
            _lblMax = SumLabel("Maks. Δ: —", 360);
            _summaryPanel.Controls.AddRange(new Control[] { _lblChanged, _lblAvg, _lblMax });

            Controls.Add(_layout);
            Controls.Add(_summaryPanel);
            Controls.Add(_lblTitle);
        }

        private Panel Wrap(Control inner, string title, Color hdrColor)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = C_Bg, Padding = new Padding(1) };
            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = title,
                ForeColor = Color.White,
                BackColor = hdrColor,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            inner.Dock = DockStyle.Fill;
            panel.Controls.Add(inner);
            panel.Controls.Add(header);
            return panel;
        }

        private DataGridView BuildGrid(Color bg)
        {
            var g = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ScrollBars = ScrollBars.Both,
                BackgroundColor = bg,
                GridColor = Color.FromArgb(48, 54, 61),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.5f),
                RowHeadersWidth = 70,
                RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing,
                ColumnHeadersHeight = 22,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                EnableHeadersVisualStyles = false,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = bg,
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(70, 100, 150),
                    SelectionForeColor = Color.White,
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C_Header,
                    ForeColor = Color.FromArgb(139, 148, 158),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Consolas", 8f),
                },
                RowHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = C_Header,
                    ForeColor = Color.FromArgb(139, 148, 158),
                    Font = new Font("Consolas", 8f),
                },
            };
            return g;
        }

        // ── Grid Yapılandırma ────────────────────────────────────────

        private void RebuildGridStructure()
        {
            foreach (var grid in new[] { _gridStock, _gridDiff, _gridMod })
            {
                grid.Columns.Clear();
                grid.Rows.Clear();

                for (int c = 0; c < _cols; c++)
                {
                    string hdr = (_loadAxis != null && c < _loadAxis.Length)
                        ? $"{_loadAxis[c]}" : $"{c}";
                    grid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        HeaderText = hdr,
                        Width = 38,
                        SortMode = DataGridViewColumnSortMode.NotSortable,
                    });
                }
                for (int r = 0; r < _rows; r++)
                    grid.Rows.Add();
                for (int r = 0; r < _rows; r++)
                {
                    string rHdr = (_rpmAxis != null && r < _rpmAxis.Length)
                        ? $"{_rpmAxis[r]}" : $"{r}";
                    grid.Rows[r].HeaderCell.Value = rHdr;
                    grid.Rows[r].Height = 24;
                }
            }
        }

        // ── Doldur ──────────────────────────────────────────────────

        private void PopulateAll()
        {
            if (_stock == null || _modified == null) return;

            _gridStock.SuspendLayout();
            _gridDiff.SuspendLayout();
            _gridMod.SuspendLayout();

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    byte sv = _stock[r, c];
                    byte mv = _modified[r, c];
                    int dt = mv - sv;

                    // Stock
                    _gridStock.Rows[r].Cells[c].Value = sv;
                    _gridStock.Rows[r].Cells[c].Style.BackColor = HeatColor(sv, false);
                    _gridStock.Rows[r].Cells[c].Style.ForeColor = sv > 160 ? Color.White : Color.LightGray;
                    if (sv != mv)
                        _gridStock.Rows[r].Cells[c].Style.Font = new Font("Consolas", 7.5f, FontStyle.Bold);

                    // Modified
                    _gridMod.Rows[r].Cells[c].Value = mv;
                    _gridMod.Rows[r].Cells[c].Style.BackColor = HeatColor(mv, true);
                    _gridMod.Rows[r].Cells[c].Style.ForeColor = mv > 160 ? Color.White : Color.LightGray;
                    if (sv != mv)
                        _gridMod.Rows[r].Cells[c].Style.Font = new Font("Consolas", 7.5f, FontStyle.Bold);

                    // Delta
                    var dc = _gridDiff.Rows[r].Cells[c];
                    if (dt == 0)
                    {
                        dc.Value = "·";
                        dc.Style.BackColor = C_Neutral;
                        dc.Style.ForeColor = Color.FromArgb(70, 75, 90);
                    }
                    else
                    {
                        dc.Value = dt > 0 ? $"+{dt}" : $"{dt}";
                        float intensity = Math.Min(Math.Abs(dt) / 50f, 1f);
                        dc.Style.BackColor = Lerp(C_Neutral, dt > 0 ? C_Inc : C_Dec, intensity);
                        dc.Style.ForeColor = Color.White;
                        dc.Style.Font = new Font("Consolas", 7.5f, FontStyle.Bold);
                    }
                }

            _gridStock.ResumeLayout();
            _gridDiff.ResumeLayout();
            _gridMod.ResumeLayout();
        }

        private void UpdateSummary()
        {
            if (_stock == null) return;
            int n = 0, total = 0, max = 0;
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    int d = Math.Abs(_modified[r, c] - _stock[r, c]);
                    if (d > 0) { n++; total += d; if (d > max) max = d; }
                }
            int cells = _rows * _cols;
            double avg = n > 0 ? (double)total / n : 0;
            _lblChanged.Text = $"Değişen: {n} / {cells}";
            _lblAvg.Text = $"Ort. Δ: {avg:F1}";
            _lblMax.Text = $"Maks. Δ: {max}";
            _lblChanged.ForeColor = n > 80 ? Color.OrangeRed : n > 25 ? Color.Yellow : Color.LightGreen;
        }

        private static Color HeatColor(byte v, bool isMod)
        {
            float t = v / 255f;
            if (!isMod)
                return t < 0.5f
                    ? Lerp(Color.FromArgb(20, 30, 70), Color.FromArgb(30, 80, 60), t * 2f)
                    : Lerp(Color.FromArgb(30, 80, 60), Color.FromArgb(80, 40, 40), (t - 0.5f) * 2f);
            return t < 0.5f
                ? Lerp(Color.FromArgb(10, 25, 80), Color.FromArgb(20, 70, 100), t * 2f)
                : Lerp(Color.FromArgb(20, 70, 100), Color.FromArgb(60, 40, 120), (t - 0.5f) * 2f);
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static Label SumLabel(string text, int x) => new Label
        {
            Text = text,
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(x, 10),
            Font = new Font("Segoe UI", 8.5f),
        };
    }
}
