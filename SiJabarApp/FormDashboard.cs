using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MongoDB.Driver;
using SiJabarApp.model;

namespace SiJabarApp
{
    public class FormDashboard : Form
    {
        // --- DATABASE ---
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;

        // --- DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        // --- WARNA TEMA ---
        private readonly Color bgColor = Color.FromArgb(243, 244, 246);
        private readonly Color cardBg = Color.White;
        private readonly Color headerBg = Color.FromArgb(33, 37, 41);
        private readonly Color accentGreen = Color.FromArgb(46, 204, 113);
        private readonly Color accentBlue = Color.FromArgb(52, 152, 219);
        private readonly Color accentOrange = Color.FromArgb(243, 156, 18);
        private readonly Color accentRed = Color.FromArgb(231, 76, 60);
        private readonly Color accentPurple = Color.FromArgb(155, 89, 182);
        private readonly Color textDark = Color.FromArgb(44, 62, 80);
        private readonly Color textLight = Color.FromArgb(127, 140, 141);

        // Panel utama untuk scroll
        private Panel panelMain;

        public FormDashboard()
        {
            SetupForm();
            ConnectDB();
            BuildUI();
        }

        private void SetupForm()
        {
            this.Text = "Dashboard Ringkasan - SiJabar";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = bgColor;
            this.DoubleBuffered = true;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
        }

        private void ConnectDB()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var db = client.GetDatabase("SiJabarDB");
                collectionSampah = db.GetCollection<SampahModel>("Sampah");
                collectionMaster = db.GetCollection<MasterLokasiModel>("MasterLokasi");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error koneksi DB: " + ex.Message);
            }
        }

        private void BuildUI()
        {
            // ============================================================
            // 1. HEADER BAR
            // ============================================================
            Panel panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = headerBg,
                Padding = new Padding(20, 0, 10, 0)
            };
            panelHeader.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0x112, 0xf012, 0); };

            Label lblTitle = new Label
            {
                Text = "📊  Dashboard Ringkasan Data",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 14),
                BackColor = Color.Transparent
            };
            lblTitle.MouseDown += (s, e) => { ReleaseCapture(); SendMessage(Handle, 0x112, 0xf012, 0); };
            panelHeader.Controls.Add(lblTitle);

            // Window buttons
            Button btnClose = CreateWindowBtn("✕", 0, Color.Red);
            btnClose.Click += (s, e) => this.Close();
            panelHeader.Controls.Add(btnClose);

            Button btnMax = CreateWindowBtn("⬜", 45, Color.FromArgb(60, 60, 60));
            btnMax.Click += (s, e) => this.WindowState = (WindowState == FormWindowState.Normal) ? FormWindowState.Maximized : FormWindowState.Normal;
            panelHeader.Controls.Add(btnMax);

            Button btnMin = CreateWindowBtn("—", 90, Color.FromArgb(60, 60, 60));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
            panelHeader.Controls.Add(btnMin);

            this.Controls.Add(panelHeader);

            // ============================================================
            // 2. SCROLLABLE MAIN CONTENT
            // ============================================================
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = bgColor,
                Padding = new Padding(30, 20, 30, 20)
            };
            this.Controls.Add(panelMain);
            panelMain.BringToFront();

            // --- AMBIL DATA ---
            List<SampahModel> allData = new List<SampahModel>();
            List<MasterLokasiModel> allTPS = new List<MasterLokasiModel>();

            try
            {
                if (collectionSampah != null)
                    allData = collectionSampah.Find(_ => true).ToList();
                if (collectionMaster != null)
                    allTPS = collectionMaster.Find(_ => true).ToList();
            }
            catch { }

            // ============================================================
            // 3. SUMMARY CARDS (Baris Pertama)
            // ============================================================
            int totalLaporan = allData.Count;
            double totalBerat = allData.Sum(x => x.Berat);
            int totalTPS = allTPS.Count;
            int totalWilayah = allData.Select(x => x.Wilayah).Distinct().Count();

            int cardY = 10;
            int cardWidth = 230;
            int cardHeight = 120;
            int cardGap = 20;
            int startX = 10;

            var summaryCards = new[]
            {
                new { Title = "Total Laporan", Value = totalLaporan.ToString("N0"), Icon = "📋", Color = accentBlue },
                new { Title = "Total Berat", Value = $"{totalBerat:N1} Kg", Icon = "⚖️", Color = accentGreen },
                new { Title = "Lokasi TPS", Value = totalTPS.ToString(), Icon = "📍", Color = accentOrange },
                new { Title = "Wilayah Aktif", Value = totalWilayah.ToString(), Icon = "🗺️", Color = accentPurple },
            };

            for (int i = 0; i < summaryCards.Length; i++)
            {
                var card = summaryCards[i];
                Panel p = CreateSummaryCard(card.Title, card.Value, card.Icon, card.Color,
                    new Point(startX + i * (cardWidth + cardGap), cardY), new Size(cardWidth, cardHeight));
                panelMain.Controls.Add(p);
            }

            // ============================================================
            // 4. STATUS CARDS (Baris Kedua)
            // ============================================================
            int statusY = cardY + cardHeight + 25;

            var statusGroups = allData.GroupBy(x => x.Status ?? "Tidak Diketahui")
                .ToDictionary(g => g.Key, g => g.Count());

            var statusCards = new[]
            {
                new { Status = "Masuk", Color = accentRed, Icon = "🔴" },
                new { Status = "Dipilah", Color = accentOrange, Icon = "🟡" },
                new { Status = "Daur Ulang", Color = accentBlue, Icon = "🔵" },
                new { Status = "Selesai", Color = accentGreen, Icon = "🟢" },
            };

            for (int i = 0; i < statusCards.Length; i++)
            {
                var sc = statusCards[i];
                int count = statusGroups.ContainsKey(sc.Status) ? statusGroups[sc.Status] : 0;
                string persen = totalLaporan > 0 ? $"({(count * 100.0 / totalLaporan):F1}%)" : "(0%)";

                Panel p = CreateSummaryCard(sc.Status, $"{count} {persen}", sc.Icon, sc.Color,
                    new Point(startX + i * (cardWidth + cardGap), statusY), new Size(cardWidth, cardHeight));
                panelMain.Controls.Add(p);
            }

            // ============================================================
            // 5. JENIS SAMPAH CHART (Baris Ketiga - Kiri)
            // ============================================================
            int chartY = statusY + cardHeight + 25;
            int halfWidth = (cardWidth * 2 + cardGap);

            Panel panelJenis = CreateChartPanel("📊 Sampah per Jenis",
                new Point(startX, chartY), new Size(halfWidth, 260));

            var jenisGroups = allData.GroupBy(x => x.Jenis ?? "Lainnya")
                .Select(g => new { Jenis = g.Key, Count = g.Count(), Berat = g.Sum(x => x.Berat) })
                .OrderByDescending(x => x.Berat)
                .ToList();

            Color[] barColors = { accentGreen, accentBlue, accentOrange, accentRed, accentPurple };
            double maxBerat = jenisGroups.Any() ? jenisGroups.Max(x => x.Berat) : 1;

            int barStartY = 55;
            int barHeight = 35;
            int barGap = 10;

            for (int i = 0; i < jenisGroups.Count && i < 5; i++)
            {
                var item = jenisGroups[i];
                int barMaxWidth = halfWidth - 180;
                int barW = (int)(item.Berat / maxBerat * barMaxWidth);
                if (barW < 5) barW = 5;

                Color barColor = barColors[i % barColors.Length];
                int bY = barStartY + i * (barHeight + barGap);

                // Label Jenis
                Label lblJenis = new Label
                {
                    Text = item.Jenis,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = textDark,
                    Size = new Size(100, barHeight),
                    Location = new Point(15, bY),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                panelJenis.Controls.Add(lblJenis);

                // Bar
                Panel bar = new Panel
                {
                    Size = new Size(barW, barHeight - 6),
                    Location = new Point(120, bY + 3),
                    BackColor = barColor
                };
                // Rounded corners
                bar.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(((Panel)s).BackColor))
                    {
                        var rect = new Rectangle(0, 0, ((Panel)s).Width, ((Panel)s).Height);
                        var path = RoundRect(rect, 4);
                        e.Graphics.FillPath(brush, path);
                    }
                };
                panelJenis.Controls.Add(bar);

                // Label Nilai
                Label lblVal = new Label
                {
                    Text = $"{item.Berat:N1} Kg ({item.Count})",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = textLight,
                    AutoSize = true,
                    Location = new Point(125 + barW + 8, bY + 7)
                };
                panelJenis.Controls.Add(lblVal);
            }

            panelMain.Controls.Add(panelJenis);

            // ============================================================
            // 6. TOP WILAYAH (Baris Ketiga - Kanan)
            // ============================================================
            Panel panelWilayah = CreateChartPanel("🏆 Top 5 Wilayah",
                new Point(startX + halfWidth + cardGap, chartY), new Size(halfWidth, 260));

            var wilayahGroups = allData.GroupBy(x => x.Wilayah ?? "Tidak Diketahui")
                .Select(g => new { Wilayah = g.Key, Count = g.Count(), Berat = g.Sum(x => x.Berat) })
                .OrderByDescending(x => x.Berat)
                .Take(5)
                .ToList();

            // Table header
            int tblY = 50;
            AddTableRow(panelWilayah, "#", "Wilayah", "Laporan", "Berat (Kg)", tblY, true);
            tblY += 35;

            for (int i = 0; i < wilayahGroups.Count; i++)
            {
                var item = wilayahGroups[i];
                AddTableRow(panelWilayah, $"{i + 1}", item.Wilayah, item.Count.ToString(), $"{item.Berat:N1}", tblY, false);
                tblY += 32;
            }

            if (wilayahGroups.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "Belum ada data wilayah.",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = textLight,
                    Location = new Point(20, 80),
                    AutoSize = true
                };
                panelWilayah.Controls.Add(lblEmpty);
            }

            panelMain.Controls.Add(panelWilayah);

            // ============================================================
            // 7. LAPORAN TERBARU (Baris Keempat)
            // ============================================================
            int recentY = chartY + 260 + 25;
            int fullWidth = (cardWidth * 4 + cardGap * 3);

            Panel panelRecent = CreateChartPanel("🕐 5 Laporan Terbaru",
                new Point(startX, recentY), new Size(fullWidth, 240));

            var recentData = allData.OrderByDescending(x => x.Tanggal).Take(5).ToList();

            // Header
            int rY = 50;
            AddRecentRow(panelRecent, "Wilayah", "Jenis", "Berat", "Status", "Tanggal", rY, true, fullWidth);
            rY += 35;

            foreach (var item in recentData)
            {
                string tgl = item.Tanggal.ToString("dd MMM yyyy");
                AddRecentRow(panelRecent, item.Wilayah ?? "-", item.Jenis ?? "-",
                    $"{item.Berat:N1} Kg", item.Status ?? "-", tgl, rY, false, fullWidth);
                rY += 32;
            }

            if (recentData.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "Belum ada laporan.",
                    Font = new Font("Segoe UI", 11),
                    ForeColor = textLight,
                    Location = new Point(20, 80),
                    AutoSize = true
                };
                panelRecent.Controls.Add(lblEmpty);
            }

            panelMain.Controls.Add(panelRecent);

            // Footer spacing
            Panel spacer = new Panel
            {
                Size = new Size(10, 30),
                Location = new Point(0, recentY + 260),
                BackColor = Color.Transparent
            };
            panelMain.Controls.Add(spacer);
        }

        // ================================================================
        // HELPER METHODS
        // ================================================================

        private Button CreateWindowBtn(string text, int rightOffset, Color hoverBg)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI Symbol", 10),
                Size = new Size(40, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.LightGray,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Location = new Point(this.Width - 40 - rightOffset, 0);
            btn.MouseEnter += (s, e) => { btn.BackColor = hoverBg; btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.LightGray; };
            return btn;
        }

        private Panel CreateSummaryCard(string title, string value, string icon, Color accentColor, Point location, Size size)
        {
            Panel card = new Panel
            {
                Size = size,
                Location = location,
                BackColor = cardBg,
                Cursor = Cursors.Default
            };

            // Accent strip kiri
            Panel strip = new Panel
            {
                Size = new Size(5, size.Height),
                Location = new Point(0, 0),
                BackColor = accentColor
            };
            card.Controls.Add(strip);

            Label lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 24),
                Location = new Point(18, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblIcon);

            Label lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(70, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblValue);

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10),
                ForeColor = textLight,
                Location = new Point(70, 55),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            // Shadow effect via Paint
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid);
            };

            return card;
        }

        private Panel CreateChartPanel(string title, Point location, Size size)
        {
            Panel panel = new Panel
            {
                Size = size,
                Location = location,
                BackColor = cardBg
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(15, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            panel.Controls.Add(lblTitle);

            // Separator
            Panel sep = new Panel
            {
                Size = new Size(size.Width - 30, 1),
                Location = new Point(15, 42),
                BackColor = Color.FromArgb(230, 230, 230)
            };
            panel.Controls.Add(sep);

            panel.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid,
                    Color.FromArgb(230, 230, 230), 1, ButtonBorderStyle.Solid);
            };

            return panel;
        }

        private void AddTableRow(Panel parent, string col1, string col2, string col3, string col4, int y, bool isHeader)
        {
            Font f = isHeader ? new Font("Segoe UI", 10, FontStyle.Bold) : new Font("Segoe UI", 10);
            Color fg = isHeader ? textDark : Color.FromArgb(80, 80, 80);
            Color bg = isHeader ? Color.FromArgb(248, 249, 250) : Color.Transparent;

            int[] widths = { 40, 200, 80, 100 };
            string[] texts = { col1, col2, col3, col4 };
            int x = 15;

            if (isHeader)
            {
                Panel rowBg = new Panel { Location = new Point(0, y), Size = new Size(parent.Width, 32), BackColor = bg };
                parent.Controls.Add(rowBg);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                Label lbl = new Label
                {
                    Text = texts[i],
                    Font = f,
                    ForeColor = fg,
                    Location = new Point(x, y + 5),
                    Size = new Size(widths[i], 25),
                    TextAlign = i == 0 ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft
                };
                parent.Controls.Add(lbl);
                lbl.BringToFront();
                x += widths[i] + 10;
            }
        }

        private void AddRecentRow(Panel parent, string wilayah, string jenis, string berat, string status, string tanggal, int y, bool isHeader, int panelWidth)
        {
            Font f = isHeader ? new Font("Segoe UI", 10, FontStyle.Bold) : new Font("Segoe UI", 10);
            Color fg = isHeader ? textDark : Color.FromArgb(80, 80, 80);
            Color bg = isHeader ? Color.FromArgb(248, 249, 250) : Color.Transparent;

            int colW = (panelWidth - 60) / 5;
            string[] texts = { wilayah, jenis, berat, status, tanggal };
            int x = 15;

            if (isHeader)
            {
                Panel rowBg = new Panel { Location = new Point(0, y), Size = new Size(panelWidth, 32), BackColor = bg };
                parent.Controls.Add(rowBg);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                Label lbl = new Label
                {
                    Text = texts[i],
                    Font = f,
                    ForeColor = fg,
                    Location = new Point(x, y + 5),
                    Size = new Size(colW, 25),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // Status badge coloring
                if (!isHeader && i == 3)
                {
                    switch (texts[i])
                    {
                        case "Masuk": lbl.ForeColor = accentRed; break;
                        case "Dipilah": lbl.ForeColor = accentOrange; break;
                        case "Daur Ulang": lbl.ForeColor = accentBlue; break;
                        case "Selesai": lbl.ForeColor = accentGreen; break;
                    }
                    lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                }

                parent.Controls.Add(lbl);
                lbl.BringToFront();
                x += colW;
            }
        }

        private GraphicsPath RoundRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
