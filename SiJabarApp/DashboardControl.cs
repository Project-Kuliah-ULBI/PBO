using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Driver;
using SiJabarApp.model;
using SiJabarApp.helper;

using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;

namespace SiJabarApp
{
    public class DashboardControl : UserControl
    {
        // --- DATABASE ---
        private IMongoCollection<SampahModel> collectionSampah;
        private IMongoCollection<MasterLokasiModel> collectionMaster;

        // --- MAP COMPONENT ---
        private WebView2 webViewMap;
        private bool isMapReady = false;

        // --- WARNA TEMA (Reference index.html) ---
        private readonly Color bgColor = Color.FromArgb(249, 250, 251); // var(--light)
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(17, 24, 39);   // var(--dark)
        private readonly Color textGray = Color.FromArgb(107, 114, 128); // var(--gray)
        
        // Colors from CSS
        private readonly Color primary = Color.FromArgb(16, 185, 129);      // Green
        private readonly Color primaryLight = Color.FromArgb(209, 250, 229);
        private readonly Color secondary = Color.FromArgb(59, 130, 246);    // Blue
        private readonly Color secondaryLight = Color.FromArgb(219, 234, 254);
        private readonly Color warning = Color.FromArgb(245, 158, 11);      // Orange
        private readonly Color warningLight = Color.FromArgb(254, 243, 199);
        private readonly Color danger = Color.FromArgb(239, 68, 68);        // Red
        private readonly Color dangerLight = Color.FromArgb(254, 226, 226);

        // Panel utama untuk scroll
        private Panel panelMain;

        public DashboardControl()
        {
            SetupControl();
            ConnectDB();
            BuildUI();
        }

        private void SetupControl()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = bgColor;
            this.DoubleBuffered = true;
            
            // Init Main Panel once
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = bgColor,
                Padding = new Padding(30)
            };
            this.Controls.Add(panelMain);

            this.Resize += (s, e) => { if(this.Width > 0 && panelMain != null) BuildUI(); };
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

        public void ReloadData()
        {
            if (this.Width > 0 && panelMain != null) 
            {
                ConnectDB(); 
                BuildUI();
            }
        }

        private void BuildUI()
        {
            // Reset Controls
            panelMain.Controls.Clear();

            // --- DATA LOADING ---
            List<SampahModel> allData = new List<SampahModel>();
            List<MasterLokasiModel> allTPS = new List<MasterLokasiModel>();
            try
            {
                if (collectionSampah != null) allData = collectionSampah.Find(_ => true).ToList();
                if (collectionMaster != null) allTPS = collectionMaster.Find(_ => true).ToList();
            }
            catch { }

            // Calculate Dimensions
            int scrollWidth = 20;
            int totalWidth = panelMain.Width - panelMain.Padding.Left - panelMain.Padding.Right - scrollWidth;
            if (totalWidth < 900) totalWidth = 900; 

            int currentY = 0;
            int gap = 24;

            // ============================================================
            // 1. HEADER REMOVED (As requested)
            // ============================================================
            currentY = 20;


            // ============================================================
            // 2. STATS GRID (4 Cards)
            // ============================================================
            int cardWidth = (totalWidth - (3 * gap)) / 4;
            int cardHeight = 160; // Increased height from 140

            var stats = new[] {
                new { Title = "Total Sampah Terkumpul", Val = $"{allData.Sum(x=>x.Berat):N0} kg", Icon = "⚖️", Bg=primaryLight, Fg=primary, Trend="+12%" },
                new { Title = "Jadwal Pengangkutan", Val = allData.Count(x=>x.JadwalAngkut >= DateTime.Today).ToString(), Icon = "🚚", Bg=secondaryLight, Fg=secondary, Trend="48" },
                new { Title = "Tempat Sampah Aktif", Val = allTPS.Count.ToString(), Icon = "🗑️", Bg=warningLight, Fg=warning, Trend="156" },
                new { Title = "Tempat Sampah Penuh", Val = "12", Icon = "⚠️", Bg=dangerLight, Fg=danger, Trend="! Low" } // Dummy logic for "Full"
            };

            for(int i=0; i<4; i++)
            {
                Panel card = CreateStatCard(stats[i].Title, stats[i].Val, stats[i].Icon, stats[i].Bg, stats[i].Fg, stats[i].Trend, 
                    new Point(i * (cardWidth + gap), currentY), new Size(cardWidth, cardHeight));
                panelMain.Controls.Add(card);
            }

            currentY += cardHeight + gap + 10; // Extra gap

            // ============================================================
            // 3. MAP SECTION (LIVE MAP)
            // ============================================================
            Panel mapCard = CreateCardWithHeader("Peta Lokasi Tempat Sampah", new Point(0, currentY), new Size(totalWidth, 400));
            Panel mapContent = (Panel)mapCard.Controls[1]; // Get the content container

            // WebView2 Integration
            webViewMap = new WebView2();
            webViewMap.Dock = DockStyle.Fill;
            webViewMap.DefaultBackgroundColor = Color.White;
            mapContent.Controls.Add(webViewMap);
            
            // Initialize Map
            InitMapWebView();

            panelMain.Controls.Add(mapCard);

            currentY += 400 + gap;

            // ============================================================
            // 4. CHARTS ROW
            // ============================================================
            int chartHeight = 350;
            int widthLeft = (int)(totalWidth * 0.65) - gap/2;
            int widthRight = totalWidth - widthLeft - gap;

            // -- CHART 1: Statistik Mingguan (Bar) --
            Panel chart1 = CreateCardWithHeader("Statistik Sampah", new Point(0, currentY), new Size(widthLeft, chartHeight));
            DrawBarChart(chart1, allData);
            panelMain.Controls.Add(chart1);

            // -- CHART 2: Komposisi Sampah (Pie) --
            Panel chart2 = CreateCardWithHeader("Komposisi Sampah", new Point(widthLeft + gap, currentY), new Size(widthRight, chartHeight));
            DrawPieChart(chart2, allData);
            panelMain.Controls.Add(chart2);

            currentY += chartHeight + gap;

            // ============================================================
            // 5. BOTTOM ROW (Table & Activity)
            // ============================================================
            int boxHeight = 400;

            // -- TABLE: Jadwal Pengangkutan --
            Panel tableCard = CreateCardWithHeader("Jadwal Pengangkutan Terbaru", new Point(0, currentY), new Size(widthLeft, boxHeight));
            CreateScheduleTable(tableCard, allData);
            panelMain.Controls.Add(tableCard);

            // -- LIST: Aktivitas Terbaru --
            Panel activityCard = CreateCardWithHeader("Aktivitas Terbaru", new Point(widthLeft + gap, currentY), new Size(widthRight, boxHeight));
            CreateActivityList(activityCard, allData); // Pass data for activity items
            panelMain.Controls.Add(activityCard);

            currentY += boxHeight + 50;
            
            // Spacer bottom
            Panel spacer = new Panel { Size = new Size(10, 50), Location = new Point(0, currentY) };
            panelMain.Controls.Add(spacer);
        }

        // ================================================================
        // WIDGET CREATORS
        // ================================================================

        private void SetRoundedEdges(Control c, int radius)
        {
            c.Region = new Region(RoundRect(new Rectangle(0, 0, c.Width, c.Height), radius));
            c.Resize += (s, e) => {
                c.Region = new Region(RoundRect(new Rectangle(0, 0, c.Width, c.Height), radius));
            };
        }

        private Panel CreateStatCard(string title, string value, string icon, Color bg, Color fg, string trend, Point loc, Size size)
        {
            Panel p = new Panel 
            { 
                Location = loc, Size = size, BackColor = cardBg, 
                Padding = new Padding(20)
            };
            SetRoundedEdges(p, 16); // Use local method

            // Icon Box
            Label lblIcon = new Label {
                Text = icon, Font = new Font("Segoe UI Emoji", 18),
                Size = new Size(48, 48), Location = new Point(20, 20),
                BackColor = bg, ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleCenter
            };
            // Circle/Rounded shape for icon
            lblIcon.Paint += (s, e) => {
                 using(SolidBrush b = new SolidBrush(bg)) 
                     e.Graphics.FillEllipse(b, 0, 0, 48, 48);
                 TextRenderer.DrawText(e.Graphics, icon, lblIcon.Font, new Rectangle(0,0,48,48), Color.Black, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            };
            // Note: Transparency hack not easy in WinForms, so we just use square rounded or simple panel
            
            Label lblVal = new Label {
                Text = value,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                Location = new Point(20, 70), // Was 75
                AutoSize = true,
                ForeColor = textDark
            };
            
            Label lblTitle = new Label {
                Text = title,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(20, 115), // Was 110, now clearer
                AutoSize = true,
                ForeColor = textGray
            };

            // Trend Label (Top Right)
            Label lblTrend = new Label {
                Text = trend,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(size.Width - 60, 20),
                AutoSize = true,
                ForeColor = fg,
                BackColor = bg
            };

            p.Controls.Add(lblIcon);
            p.Controls.Add(lblVal);
            p.Controls.Add(lblTitle);
            p.Controls.Add(lblTrend);

            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(229, 231, 235), ButtonBorderStyle.Solid);
            
            return p;
        }

        private Panel CreateCardWithHeader(string title, Point loc, Size size)
        {
            Panel p = new Panel { Location = loc, Size = size, BackColor = cardBg, Padding = new Padding(0) };
            
            Label lblTitle = new Label {
                Text = title, Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Location = new Point(20, 20), AutoSize = true, ForeColor = textDark
            };
            p.Controls.Add(lblTitle);
            
            // Content Container
            Panel content = new Panel {
                Location = new Point(0, 50),
                Size = new Size(size.Width, size.Height - 50),
                BackColor = Color.Transparent
            };
            p.Controls.Add(content);

            p.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, Color.FromArgb(229, 231, 235), ButtonBorderStyle.Solid);
            return p;
        }

        // --- CHARTS ---

        private void DrawBarChart(Panel container, List<SampahModel> data)
        {
            // Simple visual representation
             Panel chartArea = (Panel)container.Controls[1]; // The content panel
             
             // Group by Type
             var groups = data.GroupBy(x => x.Jenis ?? "Lainnya").Select(g => new { Name = g.Key, Val = g.Sum(x => x.Berat) }).OrderByDescending(x => x.Val).Take(5).ToList();
             double max = groups.Any() ? groups.Max(x => x.Val) : 100;

             int barWidth = 40;
             int spacing = (chartArea.Width - (groups.Count * barWidth)) / (groups.Count + 1);
             int startX = spacing;
             int maxHeight = chartArea.Height - 40;

             foreach(var g in groups)
             {
                 int h = (int)((g.Val / max) * (maxHeight - 30));
                 
                 Panel bar = new Panel {
                     Size = new Size(barWidth, h),
                     Location = new Point(startX, maxHeight - h),
                     BackColor = primary
                 };
                 
                 Label lbl = new Label {
                     Text = g.Name,
                     Location = new Point(startX - 10, maxHeight + 5),
                     Size = new Size(barWidth + 20, 20),
                     TextAlign = ContentAlignment.MiddleCenter,
                     Font = new Font("Segoe UI", 8),
                     ForeColor = textGray
                 };

                 Label lblVal = new Label {
                     Text = g.Val.ToString("N0"),
                     Location = new Point(startX - 10, maxHeight - h - 20),
                     Size = new Size(barWidth + 20, 20),
                     TextAlign = ContentAlignment.MiddleCenter,
                     Font = new Font("Segoe UI", 8, FontStyle.Bold),
                     ForeColor = textDark
                 };

                 chartArea.Controls.Add(bar);
                 chartArea.Controls.Add(lbl);
                 chartArea.Controls.Add(lblVal);
                 
                 startX += barWidth + spacing;
             }
        }

        private void DrawPieChart(Panel container, List<SampahModel> data)
        {
             Panel chartArea = (Panel)container.Controls[1];
             
             var groups = data.GroupBy(x => x.Jenis ?? "Lainnya").Select(g => new { Name = g.Key, Val = g.Sum(x => x.Berat) }).ToList();
             double total = groups.Sum(x => x.Val);
             if (total == 0) return;

             // Use Paint event to draw Pie
             chartArea.Paint += (s, e) => {
                 e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                 float startAngle = 0;
                 Rectangle rect = new Rectangle((chartArea.Width - 180)/2, (chartArea.Height - 180)/2, 180, 180);
                 
                 Color[] colors = { primary, secondary, warning, danger, Color.Purple };
                 int i = 0;

                 foreach(var g in groups)
                 {
                     float sweepAngle = (float)((g.Val / total) * 360);
                     using(SolidBrush b = new SolidBrush(colors[i % colors.Length]))
                     {
                         e.Graphics.FillPie(b, rect, startAngle, sweepAngle);
                     }
                     startAngle += sweepAngle;
                     i++;
                 }
                 
                 // Donut hole
                 using(SolidBrush b = new SolidBrush(Color.White))
                     e.Graphics.FillEllipse(b, rect.X + 50, rect.Y + 50, 80, 80);
             };
             
             // Legend
             Panel legend = new Panel { Location = new Point(20, chartArea.Height - 60), Size = new Size(chartArea.Width - 40, 60) };
             int lx = 0;
             Color[] lColors = { primary, secondary, warning, danger, Color.Purple };
             int li = 0;
             foreach(var g in groups.Take(3)) // Show max 3 in legend
             {
                 Panel dot = new Panel { Size = new Size(10,10), BackColor = lColors[li%lColors.Length], Location = new Point(lx, 5) };
                 Label lbl = new Label { Text = $"{g.Name} ({(g.Val/total*100):F0}%)", AutoSize=true, Location = new Point(lx + 15, 2), Font = new Font("Segoe UI", 8) };
                 legend.Controls.Add(dot);
                 legend.Controls.Add(lbl);
                 lx += 100;
                 li++;
             }
             chartArea.Controls.Add(legend);
        }

        // --- BOTTOM ROW ---
         private void CreateScheduleTable(Panel container, List<SampahModel> data)
        {
            Panel content = (Panel)container.Controls[1];
            
            // Header
            int y = 10;
            string[] headers = { "ID", "Wilayah", "Jadwal", "Status" };
            int[] widths = { 50, 150, 100, 80 };
            
            for(int i=0; i<headers.Length; i++)
            {
               Label l = new Label { Text = headers[i], Location = new Point(20 + (i==0?0:widths.Take(i).Sum()), y), Size = new Size(widths[i], 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = textGray };
               content.Controls.Add(l);
            }
            
            y += 30;
            
            // Rows
            foreach(var item in data.Take(5))
            {
                Label l1 = new Label { Text = "..." + item.Id.ToString().Substring(18), Location = new Point(20, y), Size = new Size(widths[0], 20) };
                Label l2 = new Label { Text = item.Wilayah, Location = new Point(20 + widths[0], y), Size = new Size(widths[1], 20) };
                Label l3 = new Label { Text = item.JadwalAngkut.ToString("dd MMM"), Location = new Point(20 + widths[0] + widths[1], y), Size = new Size(widths[2], 20) };
                
                Label l4 = new Label { Text = item.Status, Location = new Point(20 + widths[0] + widths[1] + widths[2], y), Size = new Size(widths[3], 20), ForeColor = (item.Status=="Selesai" ? primary : warning), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                
                content.Controls.Add(l1); content.Controls.Add(l2); content.Controls.Add(l3); content.Controls.Add(l4);
                
                y += 35;
                
                Panel line = new Panel { Size = new Size(content.Width - 40, 1), Location = new Point(20, y-5), BackColor = Color.FromArgb(243, 244, 246) };
                content.Controls.Add(line);
            }
        }
        
        private void CreateActivityList(Panel container, List<SampahModel> data)
        {
             Panel content = (Panel)container.Controls[1];
             int y = 10;
             
             var activities = data.OrderByDescending(x => x.Tanggal).Take(4).ToList();
             foreach(var item in activities)
             {
                 // Icon Box
                 Panel iconBox = new Panel { Size = new Size(40,40), Location = new Point(20, y), BackColor = primaryLight };
                 // Simplified icon
                 Label ico = new Label { Text = "📝", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Emoji", 12) };
                 iconBox.Controls.Add(ico);
                 
                 Label lblTitle = new Label { Text = "Laporan Baru", Font = new Font("Segoe UI", 9, FontStyle.Bold), Location = new Point(70, y), AutoSize = true };
                 Label lblDesc = new Label { Text = $"Input dari {item.Wilayah}", Font = new Font("Segoe UI", 9), ForeColor = textGray, Location = new Point(70, y+20), AutoSize = true };
                 Label lblTime = new Label { Text = item.Tanggal.ToString("HH:mm"), Font = new Font("Segoe UI", 8), ForeColor = textGray, Location = new Point(content.Width - 60, y), AutoSize = true };
                 
                 content.Controls.Add(iconBox);
                 content.Controls.Add(lblTitle);
                 content.Controls.Add(lblDesc);
                 content.Controls.Add(lblTime);
                 
                 y += 60;
             }
        }
        // ================================================================
        // MAP LOGIC
        // ================================================================

        private async void InitMapWebView()
        {
            if (webViewMap == null) return;
            try {
                await webViewMap.EnsureCoreWebView2Async();
                string htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "map.html");
                if (File.Exists(htmlPath))
                {
                    webViewMap.Source = new Uri(htmlPath);
                    webViewMap.NavigationCompleted += WebViewMap_NavigationCompleted;
                }
            } catch (Exception) { /* Handle or ignore if WebView2 not present */ }
        }

        private async void WebViewMap_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                isMapReady = true;
                // Init Map (Bandung)
                await webViewMap.ExecuteScriptAsync("initMap(-6.9175, 107.6191, 13)");
                // Load Markers
                await Task.Delay(1000); // Wait for map to settle
                await LoadMapMarkers();
            }
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "-";
            return input.Replace("'", "\\'").Replace("\"", "").Replace("\n", " ").Trim();
        }

        private async Task LoadMapMarkers()
        {
             if (!isMapReady || collectionMaster == null || collectionSampah == null) return;
             try {
                // Clear existing
                await webViewMap.ExecuteScriptAsync("clearMarkers()");

                // 1. MASTER LOCATIONS (BLUE)
                var listMaster = collectionMaster.Find(_ => true).ToList();
                foreach (var tps in listMaster)
                {
                    if (tps.Latitude != 0 && tps.Longitude != 0)
                    {
                         string lat = tps.Latitude.ToString(CultureInfo.InvariantCulture);
                         string lon = tps.Longitude.ToString(CultureInfo.InvariantCulture);
                         string judul = CleanText(tps.NamaTPS);
                         string script = $"addMarker({lat}, {lon}, '{judul}', 'Lokasi TPS Resmi', '#3498db', 0)";
                         await webViewMap.ExecuteScriptAsync(script);
                    }
                }

                // 2. ACTIVE REPORTS (RED/YELLOW/GREEN)
                var listSampah = collectionSampah.Find(_ => true).ToList(); // Show all on dashboard
                foreach (var item in listSampah)
                {
                    if (item.Latitude != 0 && item.Longitude != 0)
                    {
                        double latOffset = item.Latitude + 0.00015; // Offset to not overlap exactly with TPS
                        string lat = latOffset.ToString(CultureInfo.InvariantCulture);
                        string lon = item.Longitude.ToString(CultureInfo.InvariantCulture);
                        string judul = CleanText(item.Wilayah);
                        string desc = CleanText($"{item.Jenis} - {item.Berat}kg ({item.Status})");
                        
                        string color = "#e74c3c"; // Red
                        if (item.Status == "Selesai") color = "#2ecc71";
                        else if (item.Status == "Dipilah" || item.Status == "Daur Ulang") color = "#f1c40f";

                        string script = $"addMarker({lat}, {lon}, '{judul}', '{desc}', '{color}', 1000)";
                        await webViewMap.ExecuteScriptAsync(script);
                    }
                }
             } catch {}
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
