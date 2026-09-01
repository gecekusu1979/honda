using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HondaTuner.UI
{
    /// <summary>
    /// Canlı telemetri gösterge paneli: RPM, MAP, Speed, AFR, ECT/IAT.
    /// GDI+ destekli koyu tema kadranlar.
    /// </summary>
    public class TelemetryDashboard : UserControl, ILocalizable
    {
        public void ApplyLocalization()
        {
            MainForm.ApplyRecursiveLocalization(this);
        }
        // ── Renk Paleti ──────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(13, 17, 23);
        private static readonly Color BgPanel = Color.FromArgb(22, 27, 34);
        private static readonly Color BgCard = Color.FromArgb(33, 38, 45);
        private static readonly Color AccentRed = Color.FromArgb(233, 69, 96);
        private static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
        private static readonly Color AccentGreen = Color.FromArgb(63, 185, 80);
        private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
        private static readonly Color VtecGreen = Color.FromArgb(63, 185, 80);
        private static readonly Color NeonBlue = Color.FromArgb(0, 212, 255);
        private static readonly Color WarnOrange = Color.FromArgb(255, 149, 0);
        private static readonly Color WarnBlue = Color.FromArgb(30, 144, 255);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);

        // ── Canlı Değerler ────────────────────────────────────────
        private double _rpm = 0;
        private double _map = 100;   // kPa
        private double _speed = 0;     // km/h
        private double _afr = 14.7;  // stoich
        private double _ect = 85;    // °C
        private double _iat = 25;    // °C

        // Γösterge panelleri
        private Panel _rpmGauge;
        private Panel _mapGauge;
        private Panel _speedGauge;
        private Panel _afrGauge;
        private Panel _ectBar;
        private Panel _iatBar;

        // Değer etiketleri
        private Label _ectVal;
        private Label _iatVal;

        public TelemetryDashboard()
        {
            BackColor = BgDark;
            DoubleBuffered = true;
            BuildUI();
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = BgDark,
                Padding = new Padding(8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            // Üst satır — 4 kadran
            _rpmGauge = MakeGaugePanel("RPM");
            _mapGauge = MakeGaugePanel("MAP");
            _speedGauge = MakeGaugePanel("SPEED");
            _afrGauge = MakeGaugePanel("AFR");

            _rpmGauge.Paint += (s, e) => DrawRpmGauge(e.Graphics, _rpmGauge);
            _mapGauge.Paint += (s, e) => DrawMapGauge(e.Graphics, _mapGauge);
            _speedGauge.Paint += (s, e) => DrawSpeedGauge(e.Graphics, _speedGauge);
            _afrGauge.Paint += (s, e) => DrawAfrGauge(e.Graphics, _afrGauge);

            mainLayout.Controls.Add(_rpmGauge, 0, 0);
            mainLayout.Controls.Add(_mapGauge, 1, 0);
            mainLayout.Controls.Add(_speedGauge, 2, 0);
            mainLayout.Controls.Add(_afrGauge, 3, 0);

            // Alt satır — ECT + IAT progress barları
            var barPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Margin = new Padding(4) };
            BuildEctIatBars(barPanel);
            mainLayout.Controls.Add(barPanel, 0, 1);
            mainLayout.SetColumnSpan(barPanel, 4);

            Controls.Add(mainLayout);
        }

        private Panel MakeGaugePanel(string name)
        {
            var p = new Panel
            {
                Margin = new Padding(6),
                Dock = DockStyle.Fill,
                BackColor = BgCard,
            };
            p.GetType().GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(p, true);
            return p;
        }

        private void BuildEctIatBars(Panel parent)
        {
            int y = 10;

            // ECT
            var ectLabel = MkLabel("🌡 ECT (Motor Sıcaklığı):", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(12, y));
            _ectVal = MkLabel("85 °C", new Font("Segoe UI", 10f, FontStyle.Bold), AccentBlue, new Point(230, y - 1));
            _ectBar = new Panel
            {
                Location = new Point(12, y + 22),
                Height = 14,
                Width = parent.Width - 40,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = BgPanel,
            };
            _ectBar.Paint += (s, e) => DrawProgressBar(e.Graphics, _ectBar, _ect, 0, 130,
                Color.FromArgb(30, 120, 255), Color.FromArgb(255, 80, 0), "°C");

            y += 52;

            // IAT
            var iatLabel = MkLabel("💨 IAT (Emme Havası Sıcaklığı):", new Font("Segoe UI", 9f, FontStyle.Bold), TextMuted, new Point(12, y));
            _iatVal = MkLabel("25 °C", new Font("Segoe UI", 10f, FontStyle.Bold), AccentBlue, new Point(268, y - 1));
            _iatBar = new Panel
            {
                Location = new Point(12, y + 22),
                Height = 14,
                Width = parent.Width - 40,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                BackColor = BgPanel,
            };
            _iatBar.Paint += (s, e) => DrawProgressBar(e.Graphics, _iatBar, _iat, -10, 80,
                Color.FromArgb(0, 200, 100), Color.FromArgb(255, 100, 0), "°C");

            parent.Controls.AddRange(new Control[] { ectLabel, _ectVal, _ectBar, iatLabel, _iatVal, _iatBar });
        }

        // ── Kadran Çiziciler ─────────────────────────────────────

        private void DrawRpmGauge(Graphics g, Panel p)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BgCard);

            var cx = p.Width / 2;
            var cy = (int)(p.Height * 0.58);
            var r = (int)(Math.Min(p.Width, p.Height) * 0.42);

            // Arka halka
            DrawArcGauge(g, cx, cy, r, 0, 10000, _rpm,
                startAngle: 150, sweepAngle: 240,
                bgColor: Color.FromArgb(40, 50, 65),
                fgColor: _rpm > 7000 ? AccentRed : (_rpm > 5000 ? VtecGreen : AccentBlue),
                label: "RPM", unit: "×1000",
                displayVal: $"{_rpm / 1000.0:F1}",
                glowColor: _rpm > 7000 ? AccentRed : (_rpm > 5000 ? VtecGreen : AccentBlue));

            // VTEC etiketi
            if (_rpm >= 5000)
            {
                using var vtecFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                using var vtecBrush = new SolidBrush(VtecGreen);
                g.DrawString("VTEC ON!", vtecFont, vtecBrush,
                    new RectangleF(cx - 30, cy + r - 12, 60, 14),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            DrawGaugeLabel(g, p, "RPM", "×1000");
        }

        private void DrawMapGauge(Graphics g, Panel p)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BgCard);

            var cx = p.Width / 2;
            var cy = (int)(p.Height * 0.58);
            var r = (int)(Math.Min(p.Width, p.Height) * 0.42);

            DrawArcGauge(g, cx, cy, r, 0, 200, _map,
                startAngle: 150, sweepAngle: 240,
                bgColor: Color.FromArgb(40, 50, 65),
                fgColor: Color.FromArgb(0, 180, 255),
                label: "MAP", unit: "kPa",
                displayVal: $"{_map:F0}",
                glowColor: Color.FromArgb(0, 180, 255));

            DrawGaugeLabel(g, p, "MAP", "kPa");
        }

        private void DrawSpeedGauge(Graphics g, Panel p)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BgCard);

            var cx = p.Width / 2;
            var cy = (int)(p.Height * 0.58);
            var r = (int)(Math.Min(p.Width, p.Height) * 0.42);

            DrawArcGauge(g, cx, cy, r, 0, 260, _speed,
                startAngle: 150, sweepAngle: 240,
                bgColor: Color.FromArgb(40, 50, 65),
                fgColor: _speed > 200 ? WarnOrange : AccentGreen,
                label: "SPEED", unit: "km/h",
                displayVal: $"{_speed:F0}",
                glowColor: _speed > 200 ? WarnOrange : AccentGreen);

            DrawGaugeLabel(g, p, "SPEED", "km/h");
        }

        private void DrawAfrGauge(Graphics g, Panel p)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BgCard);

            var cx = p.Width / 2;
            var cy = (int)(p.Height * 0.58);
            var r = (int)(Math.Min(p.Width, p.Height) * 0.42);

            // AFR 10..20 skala; stoich = 14.7
            Color fgColor;
            if (_afr < 13.0) fgColor = WarnBlue;   // zengin — mavi
            else if (_afr > 16.0) fgColor = WarnOrange;  // fakir — turuncu
            else fgColor = AccentGreen; // stoich/ideal

            DrawArcGauge(g, cx, cy, r, 10.0, 20.0, _afr,
                startAngle: 150, sweepAngle: 240,
                bgColor: Color.FromArgb(40, 50, 65),
                fgColor: fgColor,
                label: "AFR", unit: "",
                displayVal: $"{_afr:F2}",
                glowColor: fgColor);

            // Stoich referans çizgisi
            double stoichPct = (14.7 - 10.0) / (20.0 - 10.0);
            float stoichAngle = 150f + (float)(stoichPct * 240.0);
            float stoichRad = (float)(stoichAngle * Math.PI / 180.0);
            float ix = cx + (r - 4) * (float)Math.Cos(stoichRad);
            float iy = cy + (r - 4) * (float)Math.Sin(stoichRad);
            float ox = cx + (r + 6) * (float)Math.Cos(stoichRad);
            float oy = cy + (r + 6) * (float)Math.Sin(stoichRad);
            using var stoichPen = new Pen(Color.FromArgb(200, 255, 255, 100), 2);
            g.DrawLine(stoichPen, ix, iy, ox, oy);

            DrawGaugeLabel(g, p, "AFR", "λ=1 → 14.7");
        }

        private void DrawArcGauge(Graphics g, int cx, int cy, int r,
            double minVal, double maxVal, double curVal,
            float startAngle, float sweepAngle,
            Color bgColor, Color fgColor, string label, string unit,
            string displayVal, Color glowColor)
        {
            float pct = (float)Math.Max(0, Math.Min(1, (curVal - minVal) / (maxVal - minVal)));
            float sweep = pct * sweepAngle;

            var rect = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            int thick = Math.Max(8, r / 5);

            // Arka arc
            using (var bgPen = new Pen(bgColor, thick))
            {
                bgPen.StartCap = bgPen.EndCap = LineCap.Round;
                g.DrawArc(bgPen, rect, startAngle, sweepAngle);
            }

            if (sweep > 0.5f)
            {
                // Glow efekti
                using (var glowPen = new Pen(Color.FromArgb(40, glowColor), thick + 10))
                    g.DrawArc(glowPen, rect, startAngle, sweep);

                // Ana renkli arc
                using (var fgPen = new Pen(fgColor, thick - 2))
                {
                    fgPen.StartCap = fgPen.EndCap = LineCap.Round;
                    g.DrawArc(fgPen, rect, startAngle, sweep);
                }
            }

            // Orta sayısal değer
            using var valFont = new Font("Segoe UI", r * 0.28f, FontStyle.Bold);
            using var valBrush = new SolidBrush(fgColor);
            var valRect = new RectangleF(cx - r, cy - r * 0.4f, r * 2, r * 0.7f);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(displayVal, valFont, valBrush, valRect, sf);
        }

        private void DrawGaugeLabel(Graphics g, Panel p, string label, string unit)
        {
            using var lbFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var unFont = new Font("Segoe UI", 7f);
            using var lbBrush = new SolidBrush(TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center };

            g.DrawString(label, lbFont, lbBrush,
                new RectangleF(0, p.Height - 40, p.Width, 16), sf);
            g.DrawString(unit, unFont, lbBrush,
                new RectangleF(0, p.Height - 25, p.Width, 14), sf);
        }

        private void DrawProgressBar(Graphics g, Panel p,
            double value, double min, double max,
            Color lowColor, Color highColor, string unit)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = p.ClientRectangle;

            double pct = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            int fillW = (int)(rect.Width * pct);

            // Arka plan
            using var bgBrush = new SolidBrush(BgPanel);
            g.FillRectangle(bgBrush, rect);

            if (fillW > 0)
            {
                // Gradyan dolgu
                using var grad = new LinearGradientBrush(
                    new Rectangle(rect.X, rect.Y, Math.Max(1, fillW), rect.Height),
                    lowColor, highColor, LinearGradientMode.Horizontal);
                g.FillRectangle(grad, rect.X, rect.Y, fillW, rect.Height);
            }

            // Kenarlık
            using var borderPen = new Pen(Border, 1);
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
        }

        // ── Güncelleme API ───────────────────────────────────────

        /// <summary>Telemetri değerlerini güncelle ve yeniden çiz.</summary>
        public void UpdateValues(double rpm, double map, double speed,
                                 double afr, double ect, double iat)
        {
            _rpm = rpm;
            _map = map;
            _speed = speed;
            _afr = afr;
            _ect = ect;
            _iat = iat;

            _rpmGauge.Invalidate();
            _mapGauge.Invalidate();
            _speedGauge.Invalidate();
            _afrGauge.Invalidate();
            _ectBar.Invalidate();
            _iatBar.Invalidate();

            if (_ectVal != null) _ectVal.Text = $"{_ect:F0} °C";
            if (_iatVal != null) _iatVal.Text = $"{_iat:F0} °C";
        }

        // ── Yardımcı ─────────────────────────────────────────────

        private static Label MkLabel(string text, Font font, Color fore, Point loc) =>
            new Label
            {
                Text = text,
                Font = font,
                ForeColor = fore,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = loc,
            };
    }
}
