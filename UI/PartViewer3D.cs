using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using HondaTuner.Core;

namespace HondaTuner.UI
{
    // ══════════════════════════════════════════════════════════════════
    //  3D Parça Görüntüleyici — Phong Shading + Painter's Algorithm
    //  Tüm çizim GDI+ ile, dış bağımlılık yok.
    // ══════════════════════════════════════════════════════════════════
    public class PartViewer3D : UserControl
    {
        // ── Renkler ─────────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(10, 13, 18);
        private static readonly Color BgCard = Color.FromArgb(20, 24, 32);
        private static readonly Color AccentBlue = Color.FromArgb(88, 166, 255);
        private static readonly Color AccentRed = Color.FromArgb(233, 69, 96);
        private static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(100, 110, 125);

        // ── Parça tanımları ──────────────────────────────────────────
        public enum PartType { EcuBoard, EepromChip, Obd1Connector, MapSensor, Injector, Distributor, B16Engine }
        public static readonly string[] PartNames =
        {
            "ECU Ana Kartı (Honda P28)", "EEPROM Çip (28C256)", "OBD1 Konnektör (3-Plug)",
            "MAP Sensörü (1 bar)", "Enjektör (240cc EV1)", "Distribütör (TDC Sensörlü)", "Motor (B16 FWD)"
        };
        public static readonly string[] PartNotes =
        {
            "16 MHz NEC V25 · 32KB · OBD1",
            "DIP-28 · 32KB · 5V · In-circuit",
            "3-plug A/B/C · Jumper hafıza",
            "0–200 kPa · 5V · Barometrik",
            "EV1 · 12Ω · 240cc · 4/6 adet",
            "TDC · CYP/CKP dahili · Bobin",
            "1.6L DOHC VTEC · B16A · FWD"
        };

        // ── Işık ─────────────────────────────────────────────────────
        private static readonly Vec3 LightDir =
            Vec3.Normalize(new Vec3(0.6f, -0.9f, 0.5f));

        // ── Kamera ───────────────────────────────────────────────────
        private float _rotX = 22f, _rotY = -38f;
        private float _zoom = 1.0f;
        private float _panX = 0f, _panY = 0f;

        private Point _lastMouse;
        private bool _rotating, _panning;

        // ── UI ───────────────────────────────────────────────────────
        private PartType _currentPart = PartType.EcuBoard;

        // ── Ctor ─────────────────────────────────────────────────────
        public PartViewer3D()
        {
            DoubleBuffered = true;
            BackColor = BgDark;
            MinimumSize = new Size(240, 200);
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += (s, e) => { _rotating = _panning = false; };
            MouseWheel += (s, e) =>
            {
                _zoom = Math.Max(0.2f, Math.Min(6f, _zoom * (e.Delta > 0 ? 1.12f : 0.89f)));
                Invalidate();
            };
        }

        // ── Profil senkronizasyonu ────────────────────────────────────
        public void SyncWithProfile(EcuProfile p, VehicleEntry v) { }

        public void SetPart(PartType part)
        {
            _currentPart = part;
            ResetCamera();
            Invalidate();
        }

        public void ResetCamera()
        { _rotX = 22f; _rotY = -38f; _zoom = 1f; _panX = 0; _panY = 0; }

        // ── Mouse ────────────────────────────────────────────────────
        private void OnMouseDown(object s, MouseEventArgs e)
        {
            _lastMouse = e.Location;
            _rotating = e.Button == MouseButtons.Left;
            _panning = e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle;
        }
        private void OnMouseMove(object s, MouseEventArgs e)
        {
            if (!_rotating && !_panning) return;
            float dx = e.X - _lastMouse.X, dy = e.Y - _lastMouse.Y;
            if (_rotating) { _rotY += dx * 0.45f; _rotX += dy * 0.45f; _rotX = Clamp(_rotX, -89, 89); }
            if (_panning) { _panX += dx * 0.7f; _panY += dy * 0.7f; }
            _lastMouse = e.Location;
            Invalidate();
        }

        // ── Tam Ekran ────────────────────────────────────────────────
        public void ShowFullscreen()
        {
            var form = new Form
            {
                Text = $"3D — {PartNames[(int)_currentPart]}",
                Size = new Size(960, 720),
                BackColor = BgDark,
                StartPosition = FormStartPosition.CenterScreen
            };
            var v = new PartViewer3D { Dock = DockStyle.Fill };
            v.SetPart(_currentPart);
            v._rotX = _rotX; v._rotY = _rotY; v._zoom = _zoom;
            v._panX = _panX; v._panY = _panY;
            form.Controls.Add(v);
            form.Show();
        }

        // ════════════════════════════════════════════════════════════
        //  RENDER
        // ════════════════════════════════════════════════════════════
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var area = ClientRectangle;
            g.SetClip(area);

            // Arka plan gradyanı
            using (var bg = new LinearGradientBrush(area,
                Color.FromArgb(14, 18, 26), Color.FromArgb(8, 11, 17), 90f))
                g.FillRectangle(bg, area);

            // Izgara zeminini çiz
            DrawFloorGrid(g, area);

            // Parça geometrisini oluştur ve render et
            var mesh = BuildMesh(_currentPart);
            RenderMesh(g, mesh, area);

            // Parça adı etiketi
            using var font = new Font("Segoe UI Semibold", 9f);
            using var br = new SolidBrush(Color.FromArgb(90, AccentBlue));
            g.DrawString(PartNames[(int)_currentPart], font, br,
                new PointF(area.X + 8, area.Y + 6));


            g.ResetClip();
        }

        // ── Zemin Izgarası ────────────────────────────────────────────
        private void DrawFloorGrid(Graphics g, Rectangle area)
        {
            int cx = area.X + area.Width / 2 + (int)_panX;
            int cy = area.Y + area.Height / 2 + (int)_panY + (int)(80 * _zoom);
            float s = _zoom;
            int gSize = (int)(36 * s), n = 7;
            using var gPen = new Pen(Color.FromArgb(18, AccentBlue), 1);
            for (int i = -n; i <= n; i++)
            {
                g.DrawLine(gPen, cx - n * gSize, cy + i * gSize / 3,
                                 cx + n * gSize, cy + i * gSize / 3);
                g.DrawLine(gPen, cx + i * gSize, cy - n * gSize / 3,
                                 cx + i * gSize, cy + n * gSize / 3);
            }
        }

        // ── Mesh Oluştur ─────────────────────────────────────────────
        private Mesh BuildMesh(PartType part)
        {
            switch (part)
            {
                case PartType.EcuBoard: return BuildEcuBoard();
                case PartType.EepromChip: return BuildEeprom();
                case PartType.Obd1Connector: return BuildConnector();
                case PartType.MapSensor: return BuildMapSensor();
                case PartType.Injector: return BuildInjector();
                case PartType.Distributor: return BuildDistributor();
                case PartType.B16Engine: return BuildB16Engine();
                default: return new Mesh();
            }
        }

        // ── Render Pipeline ──────────────────────────────────────────
        private void RenderMesh(Graphics g, Mesh mesh, Rectangle area)
        {
            int cx = area.X + area.Width / 2 + (int)_panX;
            int cy = area.Y + area.Height / 2 + (int)_panY;

            // 1. Tüm vertexleri dönüştür (dünya → kamera)
            var xformed = mesh.Vertices.Select(v =>
            {
                // Y ekseni döndürme
                double ry = _rotY * Math.PI / 180.0;
                double rx = _rotX * Math.PI / 180.0;
                float x1 = v.X * (float)Math.Cos(ry) + v.Z * (float)Math.Sin(ry);
                float z1 = -v.X * (float)Math.Sin(ry) + v.Z * (float)Math.Cos(ry);
                float y1 = v.Y * (float)Math.Cos(rx) - z1 * (float)Math.Sin(rx);
                float z2 = v.Y * (float)Math.Sin(rx) + z1 * (float)Math.Cos(rx);
                return new Vec3(x1, y1, z2);
            }).ToArray();

            // 2. Yüzleri kamera-Z'sine göre sırala (Painter's algorithm: uzaktan yakına)
            var sorted = mesh.Faces
                .Where(f => f.Indices.Length >= 3)
                .Select(f =>
                {
                    float avgZ = f.Indices.Average(i => xformed[i].Z);
                    return (face: f, avgZ);
                })
                .OrderByDescending(t => t.avgZ)
                .ToList();

            // 3. Her yüzü render et
            foreach (var (face, _) in sorted)
            {
                int[] idx = face.Indices;
                // Yüz normali hesapla (kamera uzayında)
                Vec3 v0 = xformed[idx[0]];
                Vec3 v1 = xformed[idx[1]];
                Vec3 v2 = xformed[idx[2]];
                Vec3 edge0 = Vec3.Sub(v1, v0);
                Vec3 edge1 = Vec3.Sub(v2, v0);
                Vec3 normal = Vec3.Normalize(Vec3.Cross(edge0, edge1));

                // Back-face culling: Perspektife duyarlı bakış yönü vektörü ile hassas eleme
                Vec3 viewDir = Vec3.Normalize(new Vec3(v0.X, v0.Y, v0.Z + 420f)); // fov = 420
                if (Vec3.Dot(normal, viewDir) > 0.05f) continue;

                // Diffuse + ambient aydınlatma
                // Işık yönünü kameraya dönüştür
                Vec3 ld = TransformLightDir();
                float diff = Math.Max(0f, Vec3.Dot(normal, Vec3.Negate(ld)));
                float ambnt = face.Material.Ambient;
                float lit = ambnt + (1f - ambnt) * diff;

                // Specular highlight
                Vec3 specViewDir = Vec3.Normalize(new Vec3(0, 0, -1));
                Vec3 reflect = Vec3.Reflect(ld, normal);
                float spec = (float)Math.Pow(Math.Max(0f, Vec3.Dot(reflect, Vec3.Negate(specViewDir))),
                                    face.Material.Shininess);

                Color baseCol = face.Material.Color;
                Color shade = LightColor(baseCol, lit, spec * face.Material.Specular);

                // Perspektif projeksiyon
                float fov = 420f;
                var pts2d = idx.Select(i =>
                {
                    Vec3 cv = xformed[i];
                    float pz = fov + cv.Z * _zoom;
                    if (pz < 1f) pz = 1f;
                    float px = cx + cv.X * _zoom * fov / pz;
                    float py = cy - cv.Y * _zoom * fov / pz;
                    return new PointF(px, py);
                }).ToArray();

                if (pts2d.Length < 3) continue;

                using var fill = new SolidBrush(shade);
                g.FillPolygon(fill, pts2d);

                // Kenar çizgisi
                if (face.DrawEdge)
                {
                    using var ePen = new Pen(Color.FromArgb(60, Color.White), 0.6f);
                    g.DrawPolygon(ePen, pts2d);
                }

                // Face üstü etiket varsa çiz
                if (face.Label != null)
                {
                    var center = new PointF(pts2d.Average(p => p.X), pts2d.Average(p => p.Y));
                    using var lf = new Font("Segoe UI", 5.5f);
                    using var lb = new SolidBrush(Color.FromArgb(160, TextPrimary));
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(face.Label, lf, lb, center, sf);
                }
            }
        }

        private Vec3 TransformLightDir()
        {
            // Işığı kamera uzayına dönüştür
            double ry = _rotY * Math.PI / 180.0;
            double rx = _rotX * Math.PI / 180.0;
            Vec3 ld = LightDir;
            float x1 = ld.X * (float)Math.Cos(ry) + ld.Z * (float)Math.Sin(ry);
            float z1 = -ld.X * (float)Math.Sin(ry) + ld.Z * (float)Math.Cos(ry);
            float y1 = ld.Y * (float)Math.Cos(rx) - z1 * (float)Math.Sin(rx);
            float z2 = ld.Y * (float)Math.Sin(rx) + z1 * (float)Math.Cos(rx);
            return Vec3.Normalize(new Vec3(x1, y1, z2));
        }

        private static Color LightColor(Color c, float lit, float spec)
        {
            float r = Math.Min(255f, c.R * lit + 255f * spec);
            float gr = Math.Min(255f, c.G * lit + 255f * spec);
            float b = Math.Min(255f, c.B * lit + 255f * spec);
            return Color.FromArgb(c.A,
                Math.Max(0, (int)r),
                Math.Max(0, (int)gr),
                Math.Max(0, (int)b));
        }

        // ════════════════════════════════════════════════════════════
        //  GEOMETRİ — Parça Mesh'leri
        // ════════════════════════════════════════════════════════════

        // ── Malzemeler ───────────────────────────────────────────────
        private static Material PCB => new Material(Color.FromArgb(20, 70, 35), 0.18f, 0.06f, 12f);
        private static Material PCBGold => new Material(Color.FromArgb(200, 165, 40), 0.20f, 0.45f, 40f);
        private static Material Chip => new Material(Color.FromArgb(18, 18, 22), 0.12f, 0.15f, 28f);
        private static Material ChipTop => new Material(Color.FromArgb(30, 30, 38), 0.15f, 0.10f, 20f);
        private static Material CapYlow => new Material(Color.FromArgb(190, 150, 0), 0.15f, 0.25f, 16f);
        private static Material CapBlue => new Material(Color.FromArgb(30, 80, 160), 0.15f, 0.20f, 16f);
        private static Material Metal => new Material(Color.FromArgb(140, 148, 158), 0.20f, 0.55f, 60f);
        private static Material DarkMet => new Material(Color.FromArgb(55, 58, 65), 0.15f, 0.35f, 32f);
        private static Material Plastic => new Material(Color.FromArgb(42, 45, 52), 0.15f, 0.08f, 8f);
        private static Material Brass => new Material(Color.FromArgb(180, 148, 60), 0.18f, 0.45f, 50f);
        private static Material Wire => new Material(Color.FromArgb(100, 80, 20), 0.12f, 0.08f, 6f);
        private static Material EProp => new Material(Color.FromArgb(160, 50, 30), 0.15f, 0.12f, 10f);
        private static Material ConA => new Material(Color.FromArgb(72, 28, 24), 0.15f, 0.06f, 8f);
        private static Material ConB => new Material(Color.FromArgb(24, 55, 80), 0.15f, 0.06f, 8f);
        private static Material ConC => new Material(Color.FromArgb(30, 65, 35), 0.15f, 0.06f, 8f);
        private static Material Iron => new Material(Color.FromArgb(120, 128, 135), 0.22f, 0.40f, 30f);

        // ── B16 Engine OBJ Loader ─────────────────────────────────────
        private Mesh BuildB16Engine()
        {
            var m = new Mesh();
            string[] paths = new[]
            {
                "honda_b16_engine.obj",
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "honda_b16_engine.obj"),
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "honda_b16_engine.obj"),
                @"c:\Users\ayhan\OneDrive\Desktop\Canlı OBD2 Araç Telemetri Ekranı\HondaTuner\honda_b16_engine.obj"
            };

            string foundPath = null;
            foreach (var p in paths)
            {
                if (System.IO.File.Exists(p))
                {
                    foundPath = p;
                    break;
                }
            }

            if (foundPath == null)
            {
                m.AddBox(-30, -30, -30, 60, 60, 60, Iron, "DOSYA BULUNAMADI");
                return m;
            }

            try
            {
                using (var reader = new System.IO.StreamReader(foundPath))
                {
                    string line;
                    var localVerts = new List<Vec3>();
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 4) continue;
                        if (parts[0] == "v")
                        {
                            float x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                            float z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                            localVerts.Add(new Vec3(x, y, z));
                        }
                        else if (parts[0] == "f")
                        {
                            int i0 = ParseObjIndex(parts[1]);
                            int i1 = ParseObjIndex(parts[2]);
                            int i2 = ParseObjIndex(parts[3]);
                            m.AddFaceDirect(new[] { i0 - 1, i1 - 1, i2 - 1 }, Iron);
                        }
                    }
                    m.Vertices.AddRange(localVerts);
                }
            }
            catch (Exception ex)
            {
                m.AddBox(-30, -30, -30, 60, 60, 60, Iron, "HATA: " + ex.Message);
            }
            return m;
        }

        private static int ParseObjIndex(string part)
        {
            int idx = part.IndexOf('/');
            if (idx >= 0) part = part.Substring(0, idx);
            return int.Parse(part);
        }


        // ── ECU Ana Kartı ────────────────────────────────────────────
        private Mesh BuildEcuBoard()
        {
            var m = new Mesh();
            // Ana PCB (yeşil) - Büyük yüzeyin Z-sıralama hatasını önlemek için 6x6 parçaya bölünerek ekleniyor
            float stepX = 210f / 6;
            float stepY = 130f / 6;
            for (int ix = 0; ix < 6; ix++)
            {
                for (int iy = 0; iy < 6; iy++)
                {
                    m.AddBox(-105 + ix * stepX, -65 + iy * stepY, -7, stepX, stepY, 7, PCB);
                }
            }

            // Devre izleri (üst yüzeyde altın çizgiler)
            m.AddBox(-100, -60, 0, 200, 2, 2, PCBGold);
            m.AddBox(-100, 10, 0, 200, 2, 2, PCBGold);
            m.AddBox(-100, 58, 0, 200, 2, 2, PCBGold);
            m.AddBox(-10, -60, 0, 2, 120, 2, PCBGold);
            m.AddBox(30, -60, 0, 2, 120, 2, PCBGold);
            m.AddBox(70, -60, 0, 2, 60, 2, PCBGold);

            // EEPROM çip (merkez)
            m.AddBox(-22, -16, 0, 44, 32, 10, Chip, "EEPROM");
            m.AddBox(-20, -14, 10, 40, 28, 2, ChipTop);

            // CPU çip
            m.AddBox(-60, 10, 0, 36, 36, 8, Chip, "CPU");
            m.AddBox(-58, 12, 8, 32, 32, 2, ChipTop);

            // Kondansatörler (silindir)
            foreach (var (px, py, col) in new[] {
                (-75f, -30f, CapYlow), (-55f, -30f, CapYlow),
                (-35f, -30f, CapBlue), (55f, -30f, CapYlow),
                (75f, -30f, CapYlow), (55f, 30f, CapBlue)})
            {
                m.AddCylinder(px, py, 0, 6, 22, 14, col);
                m.AddCylinder(px, py, 22, 6, 3, 14, Metal);
            }

            // Direnç sıraları  
            for (int i = 0; i < 5; i++)
                m.AddBox(-104 + i * 12, 30, 0, 8, 5, 4, EProp);
            for (int i = 0; i < 4; i++)
                m.AddBox(50 + i * 10, 30, 0, 6, 5, 4, EProp);

            // A Konnektör (sol alt)
            m.AddBox(-105, -65, -7, 32, 65, 22, Plastic, "A");
            // B Konnektör (sol üst)
            m.AddBox(-105, 5, -7, 32, 60, 22, Plastic, "B");
            // C Konnektör (sağ)
            m.AddBox(72, -65, -7, 33, 130, 22, Plastic, "C");

            // Pin'ler A
            for (int i = 0; i < 8; i++)
                m.AddBox(-120, -60 + i * 8, 0, 12, 4, 3, Metal);
            // Pin'ler B
            for (int i = 0; i < 7; i++)
                m.AddBox(-120, 10 + i * 8, 0, 12, 4, 3, Metal);
            // Pin'ler C
            for (int i = 0; i < 12; i++)
                m.AddBox(105, -60 + i * 10, 0, 15, 5, 3, Metal);

            // Montaj delikleri (köşe)
            foreach (var (px, py) in new[] { (-95f, -55f), (95f, -55f), (-95f, 55f), (95f, 55f) })
                m.AddCylinder(px, py, -7, 5, 14, 16, Metal);

            // Bağlantı izleri / vias
            for (int i = 0; i < 8; i++)
                m.AddCylinder(-30 + i * 10, -40, 0, 2, 4, 10, PCBGold);

            return m;
        }

        // ── EEPROM Çip ───────────────────────────────────────────────
        private Mesh BuildEeprom()
        {
            var m = new Mesh();
            // Gövde
            m.AddBox(-44, -18, 0, 88, 36, 6, Chip, "28C256");
            m.AddBox(-42, -16, 6, 84, 32, 2, ChipTop);

            // Çentik (pin 1 tarafı)
            m.AddBox(-44, -2, 4, 8, 4, 4, Chip);

            // İşaret noktası
            m.AddCylinder(-36, -10, 8, 3, 3, 12, PCBGold);

            // Pinler sol (14 pin)
            for (int i = 0; i < 14; i++)
            {
                m.AddBox(-45, -13 + i * 2, 0, 14, 1.2f, 2, Metal);  // platform
                m.AddBox(-56, -13 + i * 2, -4, 11, 1.2f, 4, Metal); // bacak
            }
            // Pinler sağ (14 pin)
            for (int i = 0; i < 14; i++)
            {
                m.AddBox(31, -13 + i * 2, 0, 14, 1.2f, 2, Metal);
                m.AddBox(45, -13 + i * 2, -4, 11, 1.2f, 4, Metal);
            }

            // Yazı benzeri çizgiler (chip marking)
            m.AddBox(-22, -8, 8, 44, 2, 1, ChipTop);
            m.AddBox(-22, -2, 8, 44, 2, 1, ChipTop);
            m.AddBox(-22, 4, 8, 44, 2, 1, ChipTop);

            return m;
        }

        // ── OBD1 Konnektör ───────────────────────────────────────────
        private Mesh BuildConnector()
        {
            var m = new Mesh();
            // 3 plug: A, B, C
            var plugs = new[] { (-80f, ConA, "A"), (0f, ConB, "B"), (80f, ConC, "C") };
            foreach (var (ox, mat, lbl) in plugs)
            {
                m.AddBox(ox - 28, -20, 0, 56, 40, 22, mat, lbl);
                // Plastik kafes içi (gri)
                m.AddBox(ox - 25, -17, 6, 50, 34, 10, Plastic);
                // Üst tab
                m.AddBox(ox - 15, -24, 0, 30, 4, 22, mat);
                // Kablo tarafı
                m.AddBox(ox - 22, -18, 22, 44, 36, 14, DarkMet);
                // Pinler (4 sıra × 4)
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++)
                        if (r * 4 + c < 14)
                            m.AddCylinder(ox - 18 + c * 12, -14 + r * 9, 0, 2, 6, 10, Metal);
            }
            // Kilitleme mandalı
            m.AddBox(-130, -5, 14, 260, 10, 8, DarkMet, "LOCK");
            return m;
        }

        // ── MAP Sensörü ──────────────────────────────────────────────
        private Mesh BuildMapSensor()
        {
            var m = new Mesh();
            // Ana gövde
            m.AddBox(-30, -30, 0, 60, 60, 50, Plastic, "MAP");
            m.AddBox(-28, -28, 50, 56, 56, 5, DarkMet);

            // Marka etiket alanı
            m.AddBox(-22, -20, 2, 44, 40, 4, Metal);

            // Vakum portu (üst)
            m.AddCylinder(0, -8, 55, 8, 28, 20, DarkMet, "VAC");
            m.AddCylinder(0, -8, 83, 5, 10, 16, Metal);

            // Konnektör (alt)
            m.AddBox(-20, -14, -22, 40, 28, 22, Plastic, "CONN");
            for (int i = 0; i < 3; i++)
                m.AddCylinder(-12 + i * 12, 0, -25, 3, 4, 12, Metal);

            // Köşe vidaları
            foreach (var (px, py) in new[] { (-24f, -24f), (24f, -24f), (-24f, 24f), (24f, 24f) })
                m.AddCylinder(px, py, 48, 4, 8, 10, Metal);

            return m;
        }

        // ── Enjektör ────────────────────────────────────────────────
        private Mesh BuildInjector()
        {
            var m = new Mesh();
            // Üst konnektör gövdesi
            m.AddBox(-18, -18, 50, 36, 36, 28, Plastic, "EV1");
            m.AddBox(-16, -16, 78, 32, 32, 8, DarkMet);
            // Pin'ler
            m.AddCylinder(-8, 0, 86, 4, 18, 12, Brass);
            m.AddCylinder(8, 0, 86, 4, 18, 12, Brass);
            // Filtre kapağı
            m.AddCylinder(0, 0, 78, 10, 12, 16, Metal);

            // Ana gövde silindir (uzun)
            m.AddCylinder(0, 0, -50, 14, 100, 24, Metal);

            // Orta şişkinlik (klips bölgesi)
            m.AddCylinder(0, 0, 30, 17, 20, 24, DarkMet);
            m.AddCylinder(0, 0, 20, 12, 10, 22, Metal);

            // Kauçuk o-ring bantları
            m.AddCylinder(0, 0, 40, 15, 5, 24, Wire);
            m.AddCylinder(0, 0, -5, 15, 5, 24, Wire);

            // Enjektör ucu
            m.AddCylinder(0, 0, -50, 6, 20, 16, Brass, "TIP");
            m.AddCylinder(0, 0, -70, 3, 8, 12, Metal);

            return m;
        }

        // ── Distribütör ─────────────────────────────────────────────
        private Mesh BuildDistributor()
        {
            var m = new Mesh();
            // Alt gövde (silindir)
            m.AddCylinder(0, 0, -30, 52, 30, 24, DarkMet);
            // O-ring
            m.AddCylinder(0, 0, -2, 54, 5, 24, Wire);
            // Cap (kap)
            m.AddCylinder(0, 0, 0, 50, 35, 24, Plastic, "CAP");

            // Kap üstü (düz kapak)
            m.AddCylinder(0, 0, 35, 48, 5, 24, DarkMet);

            // Rotor terminalleri (4 adet, 90° aralıklı)
            for (int i = 0; i < 4; i++)
            {
                float ang = i * 90f * (float)Math.PI / 180f;
                float tx = (float)Math.Cos(ang) * 40;
                float ty = (float)Math.Sin(ang) * 40;
                m.AddCylinder(tx, ty, 35, 7, 18, 14, Brass);
                m.AddCylinder(tx, ty, 53, 4, 6, 10, Metal);
            }

            // Merkez terminal
            m.AddCylinder(0, 0, 35, 9, 18, 14, Brass, "HT");

            // Alt flange (montaj)
            m.AddCylinder(0, 0, -30, 62, 10, 24, Metal);

            // Rotor kolu (ayarlanabilir)
            m.AddBox(-8, -4, -25, 16, 8, 20, Metal);

            // Mil
            m.AddCylinder(0, 0, -55, 9, 38, 18, Metal);
            m.AddBox(-6, -3, -60, 12, 6, 5, Brass);

            // Vakum avans hortumu bağlantısı
            m.AddCylinder(-50, 10, 5, 8, 22, 14, DarkMet, "VAC");
            m.AddCylinder(-50, 10, 27, 6, 8, 12, Metal);

            return m;
        }

        // ── Yardımcı ─────────────────────────────────────────────────
        private static float Clamp(float v, float min, float max)
            => v < min ? min : v > max ? max : v;
    }

    // ════════════════════════════════════════════════════════════════
    //  MESH — Geometri Yapıları
    // ════════════════════════════════════════════════════════════════
    internal struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static Vec3 Sub(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(
            a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Negate(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 Normalize(Vec3 v)
        {
            float len = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (len < 0.0001f) return new Vec3(0, 0, 1);
            return new Vec3(v.X / len, v.Y / len, v.Z / len);
        }
        public static Vec3 Reflect(Vec3 i, Vec3 n)
        {
            float d = 2f * Dot(i, n);
            return new Vec3(i.X - d * n.X, i.Y - d * n.Y, i.Z - d * n.Z);
        }
    }

    internal struct Material
    {
        public Color Color;
        public float Ambient, Specular, Shininess;
        public Material(Color c, float amb, float spec, float shin)
        { Color = c; Ambient = amb; Specular = spec; Shininess = shin; }
    }

    internal class Face
    {
        public int[] Indices;
        public Material Material;
        public bool DrawEdge = true;
        public string Label = null;
    }

    internal class Mesh
    {
        public List<Vec3> Vertices = new List<Vec3>();
        public List<Face> Faces = new List<Face>();

        private int AddVert(float x, float y, float z)
        { Vertices.Add(new Vec3(x, y, z)); return Vertices.Count - 1; }

        public void AddFaceDirect(int[] idx, Material mat, bool edge = false)
            => Faces.Add(new Face { Indices = idx, Material = mat, DrawEdge = edge, Label = null });


        // Kutu (8 köşe, 6 yüz)
        public void AddBox(float x, float y, float z, float w, float h, float d,
                           Material mat, string topLabel = null)
        {
            float x1 = x, x2 = x + w, y1 = y, y2 = y + h, z1 = z, z2 = z + d;
            int a = AddVert(x1, y1, z1), b = AddVert(x2, y1, z1),
                c = AddVert(x2, y2, z1), dd = AddVert(x1, y2, z1),
                e = AddVert(x1, y1, z2), f = AddVert(x2, y1, z2),
                g = AddVert(x2, y2, z2), h2 = AddVert(x1, y2, z2);

            // Correct CCW (dışa bakan) yüz tanımları
            AddFace(new[] { e, f, g, h2 }, mat, topLabel); // Front (z=z2, normal +Z)
            AddFace(new[] { b, a, dd, c }, mat);           // Back (z=z1, normal -Z)
            AddFace(new[] { dd, h2, g, c }, mat);          // Top (y=y2, normal +Y)
            AddFace(new[] { a, b, f, e }, mat);            // Bottom (y=y1, normal -Y)
            AddFace(new[] { a, e, h2, dd }, mat);          // Left (x=x1, normal -X)
            AddFace(new[] { f, b, c, g }, mat);            // Right (x=x2, normal +X)
        }

        // Silindir (üst/alt diskler + yan yüzler)
        public void AddCylinder(float cx, float cy, float z, float r, float height,
                                int segs, Material mat, string topLabel = null)
        {
            int botCenter = AddVert(cx, cy, z);
            int topCenter = AddVert(cx, cy, z + height);
            var bot = new int[segs]; var top = new int[segs];
            for (int i = 0; i < segs; i++)
            {
                float ang = i * 2f * (float)Math.PI / segs;
                float nx = cx + r * (float)Math.Cos(ang);
                float ny = cy + r * (float)Math.Sin(ang);
                bot[i] = AddVert(nx, ny, z);
                top[i] = AddVert(nx, ny, z + height);
            }
            // Yan yüzler
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                AddFace(new[] { bot[i], bot[j], top[j], top[i] }, mat);
            }
            // Alt kapak
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                AddFace(new[] { botCenter, bot[j], bot[i] }, mat, false);
            }
            // Üst kapak
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                AddFace(new[] { topCenter, top[i], top[j] }, mat, false,
                        i == 0 ? topLabel : null);
            }
        }

        private void AddFace(int[] idx, Material mat,
                              bool edge = true, string label = null)
            => Faces.Add(new Face
            {
                Indices = idx,
                Material = mat,
                DrawEdge = edge,
                Label = label
            });

        private void AddFace(int[] idx, Material mat, string label)
            => AddFace(idx, mat, true, label);
    }
}
