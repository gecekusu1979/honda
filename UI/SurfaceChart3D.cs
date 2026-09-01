using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HondaTuner.UI
{
    /// <summary>
    /// Gerçek zamanlı 3D yüzey haritası (GDI+ tel kafes / Mesh).
    /// X = MAP (Load), Y = RPM, Z = Hücre değeri (0-255).
    /// Mouse sürükleme ile kamera döndürme desteği.
    /// </summary>
    public class SurfaceChart3D : Control
    {
        // ── Renk Paleti ──────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(13, 17, 23);
        private static readonly Color TextMuted = Color.FromArgb(139, 148, 158);
        private static readonly Color Border = Color.FromArgb(48, 54, 61);

        // ── Veri ─────────────────────────────────────────────────
        private byte[,] _data;
        private int _rows;
        private int _cols;
        private string _title = HondaTuner.Core.Localization.L.Get("chart_3d_title");

        // ── Kamera Parametreleri ──────────────────────────────────
        private float _rotX = 35f;   // dikey tilt (derece)
        private float _rotZ = -35f;  // yatay dönüş (derece)

        private Point _lastMouse;
        private bool _dragging;

        // ── Aktif Hücre (Cell Tracing) ───────────────────────────
        private int _activeRow = -1;
        private int _activeCol = -1;

        public SurfaceChart3D()
        {
            DoubleBuffered = true;
            BackColor = BgDark;
            MinimumSize = new Size(160, 120);

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            Cursor = Cursors.SizeAll;
        }

        // ── API ───────────────────────────────────────────────────

        public void SetData(byte[,] data, string title = null)
        {
            _data = data;
            _rows = data?.GetLength(0) ?? 0;
            _cols = data?.GetLength(1) ?? 0;
            if (title != null) _title = title;
            Invalidate();
        }

        public void SetActiveCell(int row, int col)
        {
            _activeRow = row;
            _activeCol = col;
            Invalidate();
        }

        // ── Mouse Etkileşimi ─────────────────────────────────────

        private void OnMouseDown(object s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            { _lastMouse = e.Location; _dragging = true; }
        }

        private void OnMouseMove(object s, MouseEventArgs e)
        {
            if (!_dragging) return;
            float dx = e.X - _lastMouse.X;
            float dy = e.Y - _lastMouse.Y;
            _rotZ += dx * 0.5f;
            _rotX += dy * 0.5f;
            _rotX = Math.Max(10f, Math.Min(80f, _rotX));
            _lastMouse = e.Location;
            Invalidate();
        }

        private void OnMouseUp(object s, MouseEventArgs e) => _dragging = false;

        // ── Çizim ────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BgDark);

            if (_data == null || _rows == 0 || _cols == 0)
            {
                using var mFont = new Font("Segoe UI", 9f);
                using var mBrush = new SolidBrush(TextMuted);
                g.DrawString(HondaTuner.Core.Localization.L.Get("chart_waiting_data"),
                    mFont, mBrush,
                    new RectangleF(0, 0, Width, Height),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                return;
            }

            // Başlık
            using (var tFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var tBrush = new SolidBrush(TextMuted))
            {
                g.DrawString(_title, tFont, tBrush, new PointF(8, 4));
            }

            // Döndürme ipucu
            using (var hFont = new Font("Segoe UI", 7f))
            using (var hBrush = new SolidBrush(Color.FromArgb(80, 139, 148, 158)))
            {
                g.DrawString(HondaTuner.Core.Localization.L.Get("chart_drag_rotate"), hFont, hBrush,
                    new PointF(Width - 85, 4));
            }

            Project3D(g);
        }

        private void Project3D(Graphics g)
        {
            // Çizim alanı (başlık boşluğu bırak)
            float cxF = Width * 0.5f;
            float cyF = Height * 0.52f;
            float scaleXY = Math.Min(Width, Height) * 0.32f;
            float scaleZ = Height * 0.30f;

            double rx = _rotX * Math.PI / 180.0;
            double rz = _rotZ * Math.PI / 180.0;

            // Normalize koordinatlar: 0..1
            int maxVal = 255;

            PointF[,] pts = new PointF[_rows, _cols];
            float[,] zVals = new float[_rows, _cols];

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    // 3D koordinatlar (-1..1 aralığında)
                    double wx = (double)c / (_cols - 1) * 2 - 1;
                    double wy = (double)r / (_rows - 1) * 2 - 1;
                    double wz = (double)_data[r, c] / maxVal;  // 0..1

                    // Rz dönüşü (yatay)
                    double rx2 = wx * Math.Cos(rz) - wy * Math.Sin(rz);
                    double ry2 = wx * Math.Sin(rz) + wy * Math.Cos(rz);

                    // Rx dönüşü (dikey tilt)
                    double ry3 = ry2 * Math.Cos(rx) - wz * Math.Sin(rx);
                    double rz3 = ry2 * Math.Sin(rx) + wz * Math.Cos(rx);

                    pts[r, c] = new PointF(
                        cxF + (float)(rx2 * scaleXY),
                        cyF - (float)(ry3 * scaleXY * 0.6f) - (float)(rz3 * scaleZ));
                    zVals[r, c] = (float)wz;
                }
            }

            // Quads = arkadan öne sıra çizimi (Painter's algo — yaklaşık)
            bool xForward = Math.Cos(rz) < 0;
            bool yForward = Math.Sin(rx) > 0;

            int r0 = yForward ? 0 : _rows - 2;
            int r1 = yForward ? _rows - 1 : -1;
            int rD = yForward ? 1 : -1;

            int c0 = xForward ? 0 : _cols - 2;
            int c1 = xForward ? _cols - 1 : -1;
            int cD = xForward ? 1 : -1;

            for (int r = r0; r != r1; r += rD)
            {
                for (int c = c0; c != c1; c += cD)
                {
                    // 4 köşe noktası
                    var p00 = pts[r, c];
                    var p10 = pts[r + 1, c];
                    var p11 = pts[r + 1, c + 1];
                    var p01 = pts[r, c + 1];

                    float avgZ = (zVals[r, c] + zVals[r + 1, c] +
                                  zVals[r + 1, c + 1] + zVals[r, c + 1]) * 0.25f;

                    Color faceColor = GetZColor(avgZ);

                    var poly = new[] { p00, p10, p11, p01 };

                    // Yüz dolgusu
                    using (var fill = new SolidBrush(Color.FromArgb(180, faceColor)))
                        g.FillPolygon(fill, poly);

                    // Tel kafes çizgisi
                    bool isActive = (r == _activeRow || r + 1 == _activeRow) &&
                                    (c == _activeCol || c + 1 == _activeCol);
                    Color lineColor = isActive
                        ? Color.FromArgb(220, 0, 212, 255)
                        : Color.FromArgb(80, Border);
                    using (var pen = new Pen(lineColor, isActive ? 1.5f : 0.6f))
                    {
                        g.DrawPolygon(pen, poly);
                    }
                }
            }

            // Aktif nokta işaretçisi
            if (_activeRow >= 0 && _activeCol >= 0 &&
                _activeRow < _rows && _activeCol < _cols)
            {
                var ap = pts[_activeRow, _activeCol];
                using var apBrush = new SolidBrush(Color.FromArgb(200, 0, 212, 255));
                g.FillEllipse(apBrush, ap.X - 4, ap.Y - 4, 8, 8);
                using var apPen = new Pen(Color.White, 1.5f);
                g.DrawEllipse(apPen, ap.X - 4, ap.Y - 4, 8, 8);
            }

            // Renk skalası (sağ kenar)
            DrawColorLegend(g);
        }

        private void DrawColorLegend(Graphics g)
        {
            int lh = Height - 40;
            int lx = Width - 18;
            int ly = 24;

            for (int i = 0; i < lh; i++)
            {
                float t = 1f - (float)i / lh;
                using var pen = new Pen(GetZColor(t), 2);
                g.DrawLine(pen, lx, ly + i, lx + 10, ly + i);
            }

            using var f = new Font("Segoe UI", 7f);
            using var br = new SolidBrush(TextMuted);
            g.DrawString("255", f, br, lx - 12, ly - 2);
            g.DrawString("0", f, br, lx - 6, ly + lh - 8);
        }

        private static Color GetZColor(float t)
        {
            // t: 0 (düşük) → mavi; 1 (yüksek) → kırmızı
            Color[] stops =
            {
                Color.FromArgb(30,  60, 200),  // 0.0 — koyu mavi
                Color.FromArgb(0,  180, 255),  // 0.25 — cyan
                Color.FromArgb(0,  200,  80),  // 0.5  — yeşil
                Color.FromArgb(255, 160,  0),  // 0.75 — turuncu
                Color.FromArgb(220,  30,  30), // 1.0  — kırmızı
            };
            float s = t * (stops.Length - 1);
            int idx = Math.Min((int)s, stops.Length - 2);
            float u = s - idx;
            Color a = stops[idx], b = stops[idx + 1];
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * u),
                (int)(a.G + (b.G - a.G) * u),
                (int)(a.B + (b.B - a.B) * u));
        }
    }
}
