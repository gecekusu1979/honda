using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using HondaTuner.Core;
using HondaTuner.Core.Localization;

namespace HondaTuner.UI
{
    /// <summary>
    /// Profesyonel araç / ECU seçim dialogu.
    /// Sol: custom-draw kategori paneli (VS Code sidebar benzeri)
    /// Sağ üst: scrollable araç listesi | Sağ alt: detay kartı
    /// </summary>
    public class VehicleSelectDialog : Form
    {
        // ── Renkler ──────────────────────────────────────────────────
        private static readonly Color C_BgDark = Color.FromArgb(13, 17, 23);
        private static readonly Color C_BgPanel = Color.FromArgb(22, 27, 34);
        private static readonly Color C_BgCard = Color.FromArgb(30, 36, 44);
        private static readonly Color C_BgHover = Color.FromArgb(38, 45, 55);
        private static readonly Color C_BgSel = Color.FromArgb(31, 55, 88);
        private static readonly Color C_AccentRed = Color.FromArgb(233, 69, 96);
        private static readonly Color C_AccentBlue = Color.FromArgb(88, 166, 255);
        private static readonly Color C_Green = Color.FromArgb(63, 185, 80);
        private static readonly Color C_Orange = Color.FromArgb(255, 166, 0);
        private static readonly Color C_TextPrim = Color.FromArgb(220, 228, 236);
        private static readonly Color C_TextMuted = Color.FromArgb(120, 130, 142);
        private static readonly Color C_Border = Color.FromArgb(48, 54, 61);
        private static readonly Color C_SelBar = Color.FromArgb(233, 69, 96);

        // ── UI ───────────────────────────────────────────────────────
        private EcuSidebarPanel _sidebar;
        private DataGridView _vehicleGrid;
        private Panel _detailCard;
        private Label _lblEcuName, _lblBadges, _lblDesc, _lblVehicle;
        private Button _btnOk, _btnCancel;
        private Label _countLabel;

        public EcuProfile SelectedProfile { get; private set; }
        public VehicleEntry SelectedVehicle { get; private set; }

        public VehicleSelectDialog()
        {
            Text = L.Get("veh_dialog_title");
            Size = new Size(1020, 640);
            MinimumSize = new Size(860, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = C_BgDark;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;

            BuildHeader();
            BuildBottomBar();   // Bottom — önce
            BuildBody();        // Fill — sonra
        }

        // ── Header ───────────────────────────────────────────────────
        private void BuildHeader()
        {
            var hdr = new Panel { Dock = DockStyle.Top, Height = 76 };
            hdr.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var b = new LinearGradientBrush(hdr.ClientRectangle,
                    Color.FromArgb(22, 28, 42), Color.FromArgb(14, 48, 90),
                    LinearGradientMode.Horizontal);
                g.FillRectangle(b, hdr.ClientRectangle);
                using var p = new Pen(C_AccentRed, 2);
                g.DrawLine(p, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            };

            var logo = new Label
            {
                Text = "H",
                Font = new Font("Arial Black", 30f, FontStyle.Bold),
                ForeColor = C_AccentRed,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 16)
            };
            var title = new Label
            {
                Text = L.Get("veh_dialog_label_title"),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = C_TextPrim,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(76, 18)
            };
            var sub = new Label
            {
                Text = L.Get("veh_dialog_label_sub"),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = C_TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(78, 50)
            };

            hdr.Controls.AddRange(new Control[] { logo, title, sub });
            Controls.Add(hdr);
        }

        // ── Alt Buton Şeridi ─────────────────────────────────────────
        private void BuildBottomBar()
        {
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = C_BgPanel };
            bar.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1);
                e.Graphics.DrawLine(p, 0, 0, bar.Width, 0);
            };

            _countLabel = new Label
            {
                Text = L.Get("veh_dialog_count"),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = C_TextMuted,
                BackColor = Color.Transparent,
                AutoSize = true
            };
            _countLabel.Location = new Point(16, 20);

            _btnCancel = new Button
            {
                Text = L.Get("veh_dialog_cancel"),
                Size = new Size(90, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = C_BgCard,
                ForeColor = C_TextMuted,
                Font = new Font("Segoe UI", 9f)
            };
            _btnCancel.FlatAppearance.BorderColor = C_Border;
            _btnCancel.FlatAppearance.BorderSize = 1;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnOk = new Button
            {
                Text = L.Get("veh_dialog_ok"),
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = C_AccentRed,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Enabled = false
            };
            _btnOk.FlatAppearance.BorderSize = 0;
            _btnOk.Click += (s, e) => AcceptSelection();

            bar.Resize += (s, e) =>
            {
                _btnOk.Location = new Point(bar.Width - 216, 10);
                _btnCancel.Location = new Point(bar.Width - 314, 10);
            };

            bar.Controls.AddRange(new Control[] { _countLabel, _btnCancel, _btnOk });
            Controls.Add(bar);
        }

        // ── Gövde (Sidebar + Sağ Panel) ──────────────────────────────
        private void BuildBody()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 4,
                BackColor = C_Border,
                FixedPanel = FixedPanel.Panel1,
            };
            split.Panel1.BackColor = C_BgPanel;
            split.Panel2.BackColor = C_BgDark;

            // ── Sol: ECU Sidebar ──────────────────────────────────────
            _sidebar = new EcuSidebarPanel();
            _sidebar.Dock = DockStyle.Fill;
            _sidebar.EcuSelected += OnEcuSelected;
            split.Panel1.Controls.Add(_sidebar);

            // ── Sağ: üst grid + alt detay kartı ─────────────────────
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = C_BgDark,
                Padding = new Padding(0),
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 44));

            BuildVehicleGrid();
            BuildDetailCard();

            rightLayout.Controls.Add(_vehicleGrid, 0, 0);
            rightLayout.Controls.Add(_detailCard, 0, 1);
            split.Panel2.Controls.Add(rightLayout);

            Controls.Add(split);
            split.BringToFront(); // Fix docking overlap Z-order

            // Set size and properties after adding to controls to avoid design-time/init exceptions
            split.Width = ClientSize.Width;
            split.SplitterDistance = 240;
            split.Panel1MinSize = 180;
            split.Panel2MinSize = 400;
        }

        // ── Araç Grid (DataGridView) ──────────────────────────────────
        private void BuildVehicleGrid()
        {
            _vehicleGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = C_BgCard,
                GridColor = C_Border,
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                ScrollBars = ScrollBars.Vertical,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowTemplate = { Height = 34 },
                Margin = new Padding(0),
                Font = new Font("Segoe UI", 9f),
            };

            _vehicleGrid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = C_BgCard,
                ForeColor = C_TextPrim,
                SelectionBackColor = C_BgSel,
                SelectionForeColor = C_TextPrim,
                Padding = new Padding(6, 0, 0, 0),
            };
            _vehicleGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(27, 33, 41),
                ForeColor = C_TextPrim,
                SelectionBackColor = C_BgSel,
                SelectionForeColor = C_TextPrim,
                Padding = new Padding(6, 0, 0, 0),
            };
            _vehicleGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(28, 34, 44),
                ForeColor = C_TextMuted,
                SelectionBackColor = Color.FromArgb(28, 34, 44),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(6, 0, 0, 0),
            };
            _vehicleGrid.ColumnHeadersHeight = 30;
            _vehicleGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            AddCol(_vehicleGrid, "col_make", L.Get("veh_dialog_make"), 80, false);
            AddCol(_vehicleGrid, "col_model", L.Get("veh_dialog_model"), 80, false);
            AddCol(_vehicleGrid, "col_trim", L.Get("veh_dialog_trim"), 90, false);
            AddCol(_vehicleGrid, "col_engine", L.Get("veh_dialog_engine"), 75, false);
            AddCol(_vehicleGrid, "col_year", L.Get("veh_dialog_year"), 80, false);
            AddCol(_vehicleGrid, "col_hp", L.Get("veh_dialog_hp"), 50, false);
            AddCol(_vehicleGrid, "col_trans", L.Get("veh_dialog_trans"), 120, true);
            AddCol(_vehicleGrid, "col_region", L.Get("veh_dialog_region"), 60, false);

            _vehicleGrid.SelectionChanged += OnVehicleGridSelectionChanged;
            _vehicleGrid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AcceptSelection(); };

            // Sütun çizgi rengi
            _vehicleGrid.CellPainting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && _vehicleGrid.SelectedRows.Count > 0
                    && _vehicleGrid.SelectedRows[0].Index == e.RowIndex)
                {
                    e.PaintBackground(e.CellBounds, true);
                    e.PaintContent(e.CellBounds);
                    using var p = new Pen(C_SelBar, 3);
                    e.Graphics.DrawLine(p, e.CellBounds.Left, e.CellBounds.Top,
                                           e.CellBounds.Left, e.CellBounds.Bottom - 1);
                    e.Handled = true;
                }
            };
        }

        private static void AddCol(DataGridView g, string name, string hdr, int w, bool fill)
        {
            var c = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = hdr,
                Width = w,
                SortMode = DataGridViewColumnSortMode.Automatic,
                AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill
                                     : DataGridViewAutoSizeColumnMode.None,
                Resizable = DataGridViewTriState.True,
            };
            g.Columns.Add(c);
        }

        // ── Detay Kartı ───────────────────────────────────────────────
        private void BuildDetailCard()
        {
            _detailCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BgPanel,
                Padding = new Padding(20, 16, 20, 16),
                Margin = new Padding(0, 2, 0, 0),
            };
            _detailCard.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1);
                e.Graphics.DrawLine(p, 0, 0, _detailCard.Width, 0);
            };

            _lblEcuName = MkLabel("", new Font("Segoe UI", 13f, FontStyle.Bold), C_AccentRed, new Point(20, 18));
            _lblBadges = MkLabel("", new Font("Segoe UI", 8.5f, FontStyle.Bold), C_TextMuted, new Point(20, 48));
            _lblDesc = MkLabel("", new Font("Segoe UI", 9f), C_TextMuted, new Point(20, 70));
            _lblVehicle = MkLabel("", new Font("Segoe UI", 9.5f), C_TextPrim, new Point(20, 110));

            _lblDesc.MaximumSize = new Size(700, 0);
            _lblVehicle.MaximumSize = new Size(700, 0);

            _detailCard.Controls.AddRange(new Control[] { _lblEcuName, _lblBadges, _lblDesc, _lblVehicle });
        }

        // ── Olay: ECU Seçildi ─────────────────────────────────────────
        private void OnEcuSelected(EcuRecord rec)
        {
            SelectedProfile = rec.Profile;
            SelectedVehicle = null;
            _btnOk.Enabled = false;

            _vehicleGrid.Rows.Clear();
            foreach (var v in rec.Vehicles)
            {
                var row = _vehicleGrid.Rows.Add(
                    v.Make, v.Model, v.Trim,
                    v.EngineCode, v.YearRange,
                    $"{v.HorsePower} HP",
                    v.Transmission, v.Region);
                _vehicleGrid.Rows[row].Tag = v;
            }

            string iab = rec.Profile.HasIab ? L.Get("veh_dialog_iab") : "";
            string vtec = rec.Profile.HasVtec ? L.Get("veh_dialog_vtec") : L.Get("veh_dialog_nonvtec");
            _lblEcuName.Text = rec.Profile.Name;
            _lblBadges.Text = $"{vtec}{iab}   ·   {rec.Profile.EngineCode}   ·   {rec.VtecType}";
            _lblBadges.ForeColor = rec.Profile.HasVtec ? C_Green : C_TextMuted;
            _lblDesc.Text = rec.ShortDescription;
            _lblVehicle.Text = L.Get("veh_dialog_desc_select");
            _lblVehicle.ForeColor = C_TextMuted;

            _countLabel.Text = string.Format(L.Get("veh_dialog_ecu_count"), rec.Vehicles.Length, rec.Profile.EcuCode);
        }

        // ── Olay: Grid satır seçimi ───────────────────────────────────
        private void OnVehicleGridSelectionChanged(object sender, EventArgs e)
        {
            if (_vehicleGrid.SelectedRows.Count == 0) return;
            var v = _vehicleGrid.SelectedRows[0].Tag as VehicleEntry;
            if (v == null) return;

            SelectedVehicle = v;
            _lblVehicle.Text =
                $"🚗  {v.Make} {v.Model} {v.Trim}   |   {v.EngineCode}  {v.Displacement}L   |   " +
                $"{v.HorsePower} HP   |   {v.Transmission}   |   {v.YearRange}" +
                (string.IsNullOrEmpty(v.Notes) ? "" : $"\n📌  {v.Notes}");
            _lblVehicle.ForeColor = C_TextPrim;
            _btnOk.Enabled = true;
        }

        private void AcceptSelection()
        {
            if (SelectedProfile == null || SelectedVehicle == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label MkLabel(string text, Font f, Color fore, Point loc) =>
            new Label
            {
                Text = text,
                Font = f,
                ForeColor = fore,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = loc
            };

        // ──────────────────────────────────────────────────────────────
        // İç sınıf: VS Code benzeri ECU Sidebar paneli
        // ──────────────────────────────────────────────────────────────
        private class EcuSidebarPanel : UserControl
        {
            public event Action<EcuRecord> EcuSelected;

            private readonly List<SidebarItem> _items = new();
            private int _hovIdx = -1;
            private int _selIdx = -1;
            private VScrollBar _vbar;
            private int _scrollY = 0;
            private const int CAT_H = 32;
            private const int ECU_H = 30;
            private const int INDENT = 20;

            private static readonly Color C_CatBg = Color.FromArgb(22, 27, 34);
            private static readonly Color C_CatFg = Color.FromArgb(120, 130, 142);
            private static readonly Color C_EcuFg = Color.FromArgb(200, 210, 220);
            private static readonly Color C_HovBg = Color.FromArgb(38, 46, 58);
            private static readonly Color C_SelBg = Color.FromArgb(25, 52, 88);
            private static readonly Color C_SelBar = Color.FromArgb(233, 69, 96);
            private static readonly Color C_Vtec = Color.FromArgb(63, 185, 80);
            private static readonly Font F_Cat = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            private static readonly Font F_Ecu = new Font("Segoe UI", 9.5f);
            private static readonly Font F_EcuSel = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            private static readonly Font F_Badge = new Font("Segoe UI", 7f, FontStyle.Bold);

            private class SidebarItem
            {
                public bool IsCategory;
                public string CategoryName;
                public EcuRecord Record;
            }

            public EcuSidebarPanel()
            {
                DoubleBuffered = true;
                BackColor = C_CatBg;
                Cursor = Cursors.Hand;

                // Scroll bar
                _vbar = new VScrollBar
                {
                    Dock = DockStyle.Right,
                    Width = 14,
                    Minimum = 0,
                    Value = 0,
                    SmallChange = ECU_H,
                    LargeChange = ECU_H * 3,
                };
                _vbar.Scroll += (s, e) => { _scrollY = _vbar.Value; Invalidate(); };
                Controls.Add(_vbar);

                BuildItems();
            }

            protected override void OnLayout(LayoutEventArgs levent)
            {
                base.OnLayout(levent);
                UpdateScroll();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                Focus();
            }

            private void BuildItems()
            {
                string lastCat = null;
                foreach (var rec in EcuDatabase.Records)
                {
                    if (rec.Category != lastCat)
                    {
                        _items.Add(new SidebarItem { IsCategory = true, CategoryName = rec.Category });
                        lastCat = rec.Category;
                    }
                    _items.Add(new SidebarItem { IsCategory = false, Record = rec });
                }
            }

            private int TotalContentHeight()
            {
                int h = 0;
                foreach (var it in _items)
                    h += it.IsCategory ? CAT_H : ECU_H;
                return h;
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UpdateScroll();
                Invalidate();
            }

            private void UpdateScroll()
            {
                int content = TotalContentHeight();
                int view = ClientSize.Height;
                if (content > view)
                {
                    _vbar.Maximum = content - view + _vbar.LargeChange;
                    _vbar.Enabled = true;
                    _scrollY = Math.Min(_scrollY, _vbar.Maximum - _vbar.LargeChange);
                    _vbar.Value = Math.Max(0, _scrollY);
                }
                else
                {
                    _vbar.Value = 0; _scrollY = 0;
                    _vbar.Enabled = false;
                }
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                if (!_vbar.Enabled) return;
                _scrollY = Math.Max(0, Math.Min(_scrollY - e.Delta / 3,
                    _vbar.Maximum - _vbar.LargeChange));
                _vbar.Value = _scrollY;
                Invalidate();
            }

            // Tıklama konumuna göre item bul
            private (int idx, SidebarItem item) HitTest(int y)
            {
                int cy = -_scrollY;
                for (int i = 0; i < _items.Count; i++)
                {
                    int h = _items[i].IsCategory ? CAT_H : ECU_H;
                    if (y >= cy && y < cy + h) return (i, _items[i]);
                    cy += h;
                }
                return (-1, null);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                var (idx, it) = HitTest(e.Y);
                if (it != null && it.IsCategory) idx = -1;
                if (idx != _hovIdx) { _hovIdx = idx; Invalidate(); }
            }
            protected override void OnMouseLeave(EventArgs e) { _hovIdx = -1; Invalidate(); }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                var (idx, it) = HitTest(e.Y);
                if (it == null || it.IsCategory) return;
                _selIdx = idx;
                Invalidate();
                EcuSelected?.Invoke(it.Record);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int bw = _vbar.Visible ? _vbar.Width : 0;
                int w = ClientSize.Width - bw;
                int cy = -_scrollY;

                for (int i = 0; i < _items.Count; i++)
                {
                    var it = _items[i];
                    if (it.IsCategory)
                    {
                        // Kategori başlığı
                        var r = new Rectangle(0, cy, w, CAT_H);
                        using var bg = new SolidBrush(Color.FromArgb(18, 22, 28));
                        g.FillRectangle(bg, r);
                        // İnce üst çizgi (ilk kategori hariç)
                        if (i > 0)
                        {
                            using var sep = new Pen(Color.FromArgb(40, 50, 62), 1);
                            g.DrawLine(sep, 0, cy, w, cy);
                        }
                        // Kategori ikonu
                        string icon = it.CategoryName switch
                        {
                            "Civic" => "📁",
                            "Integra" => "📂",
                            "Prelude" => "📁",
                            _ => "📁"
                        };
                        using var catBrush = new SolidBrush(C_CatFg);
                        g.DrawString($"{icon}  {it.CategoryName.ToUpperInvariant()}",
                            F_Cat, catBrush,
                            new RectangleF(INDENT - 4, cy + (CAT_H - 14) / 2f, w - INDENT - 4, 14));
                    }
                    else
                    {
                        var rec = it.Record;
                        var r = new Rectangle(0, cy, w, ECU_H);
                        bool sel = i == _selIdx;
                        bool hov = i == _hovIdx;

                        // Arka plan
                        Color bgCol = sel ? C_SelBg : hov ? C_HovBg : C_CatBg;
                        using var bgBr = new SolidBrush(bgCol);
                        g.FillRectangle(bgBr, r);

                        // Sol seçim çubuğu
                        if (sel)
                        {
                            using var selBar = new SolidBrush(C_SelBar);
                            g.FillRectangle(selBar, 0, cy, 3, ECU_H);
                        }

                        // ECU kodu
                        var fnt = sel ? F_EcuSel : F_Ecu;
                        var fg = sel ? Color.White : C_EcuFg;
                        using var fgBr = new SolidBrush(fg);
                        g.DrawString($"{rec.Profile.EcuCode}", fnt, fgBr,
                            new PointF(INDENT + 4, cy + (ECU_H - 14) / 2f));

                        // Engine code sağda
                        string eng = rec.Profile.EngineCode;
                        var engSz = g.MeasureString(eng, F_Badge);
                        Color engColor = rec.Profile.HasVtec ? C_Vtec : C_CatFg;
                        using var engBr = new SolidBrush(engColor);
                        g.DrawString(eng, F_Badge, engBr,
                            new PointF(w - engSz.Width - 10, cy + (ECU_H - engSz.Height) / 2f + 1));

                        // VTEC nokta
                        if (rec.Profile.HasVtec)
                        {
                            using var dot = new SolidBrush(C_Vtec);
                            g.FillEllipse(dot, INDENT - 2, cy + (ECU_H - 6) / 2, 6, 6);
                        }
                        else
                        {
                            using var ring = new Pen(C_CatFg, 1.5f);
                            g.DrawEllipse(ring, INDENT - 2, cy + (ECU_H - 6) / 2, 6, 6);
                        }
                    }
                    cy += it.IsCategory ? CAT_H : ECU_H;
                }

                // Sağ kenar çizgisi
                using var border = new Pen(C_Border, 1);
                g.DrawLine(border, w - 1, 0, w - 1, ClientSize.Height);
            }
        }
    }
}
