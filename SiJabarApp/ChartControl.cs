using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using MongoDB.Driver;
using SiJabarApp.helper;
using SiJabarApp.model;

namespace SiJabarApp
{
    public partial class ChartControl : UserControl
    {
        private IMongoCollection<SampahModel> collectionSampah;
        private Chart chartBar;
        private Chart chartPie;
        private Chart chartLine;
        private TableLayoutPanel tableLayout;

        public ChartControl()
        {
            InitializeComponent();
            ConnectDB();
            LoadDataToChart();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245); // Light Gray Background
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(10);

            // 1. Table Layout (2 Rows, 2 Columns)
            tableLayout = new TableLayoutPanel();
            tableLayout.Dock = DockStyle.Fill;
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = 2;
            tableLayout.Padding = new Padding(5);
            
            // Kolom 50:50
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            // Baris 1 (50%), Baris 2 (50%)
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            this.Controls.Add(tableLayout);

            // 2. Initialize Charts
            chartBar = CreateChart("Grafik Batang: Total Berat per Jenis", "Jenis Sampah", "Berat (Kg)");
            chartPie = CreateChart("Pie Chart: Persentase Jenis Sampah", "", "");
            chartLine = CreateChart("Statistik Bulanan: Tren Sampah per Wilayah", "Tanggal (Hari)", "Berat (Kg)");

            // 3. Add to Layout with CARDS (Panels)
            // Top Left
            tableLayout.Controls.Add(CreateDataCard(chartBar), 0, 0); 
            // Top Right
            tableLayout.Controls.Add(CreateDataCard(chartPie), 1, 0);
            // Bottom (Span 2 Cols)
            Panel pnlBottom = CreateDataCard(chartLine);
            tableLayout.Controls.Add(pnlBottom, 0, 1);
            tableLayout.SetColumnSpan(pnlBottom, 2);
        }

        private Panel CreateDataCard(Control content)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = Color.White;
            card.Padding = new Padding(10);
            card.Margin = new Padding(10); // Spacing between cards
            
            // Rounded Border effect handled by padding/margin visually
            content.Dock = DockStyle.Fill;
            card.Controls.Add(content);
            
            return card;
        }

        private Chart CreateChart(string titleText, string xTitle, string yTitle)
        {
            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;
            
            ChartArea area = new ChartArea("MainArea");
            area.AxisX.Title = xTitle;
            area.AxisX.TitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
            area.AxisX.Interval = 1;
            area.AxisX.MajorGrid.LineColor = Color.WhiteSmoke;
            
            area.AxisY.Title = yTitle;
            area.AxisY.TitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
            area.AxisY.MajorGrid.LineColor = Color.WhiteSmoke;
            
            chart.ChartAreas.Add(area);

            // Simpan Judul Asli di Tag untuk reset
            Title title = new Title(titleText);
            title.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            title.ForeColor = Color.DarkSlateGray;
            chart.Titles.Add(title);
            chart.Tag = titleText; 

            Legend legend = new Legend("Legend1");
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            chart.Legends.Add(legend);

            return chart;
        }

        private void ConnectDB()
        {
            try
            {
                // var client = new MongoClient("mongodb://localhost:27017");
                var client = new MongoClient("mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB");
                var database = client.GetDatabase("SiJabarDB");
                collectionSampah = database.GetCollection<SampahModel>("Sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi Database Gagal: " + ex.Message);
            }
        }

        public void LoadDataToChart()
        {
            if (collectionSampah == null) return;

            try
            {
                var dataList = collectionSampah.Find(_ => true).ToList();
                if (dataList.Count == 0) return;

                // ---------------------------------------------
                // 1. CHART BATANG (Total Berat per Jenis)
                // ---------------------------------------------
                var dataPerJenis = dataList
                    .GroupBy(x => x.Jenis)
                    .Select(g => new { Jenis = g.Key, Total = g.Sum(x => x.Berat) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                chartBar.Series.Clear();
                Series seriesBar = new Series("Berat")
                {
                    ChartType = SeriesChartType.Column,
                    IsValueShownAsLabel = true,
                    Color = Color.FromArgb(16, 185, 129) // Emerald
                };
                foreach (var item in dataPerJenis) seriesBar.Points.AddXY(item.Jenis, item.Total);
                chartBar.Series.Add(seriesBar);

                // ---------------------------------------------
                // 2. PIE CHART (Persentase per Jenis)
                // ---------------------------------------------
                chartPie.Series.Clear();
                Series seriesPie = new Series("Persentase")
                {
                    ChartType = SeriesChartType.Pie,
                    IsValueShownAsLabel = true,
                    LabelFormat = "{0:P0}", // Menampilkan angka persen (misal 30%)
                    LegendText = "#VALX" // Show Name in Legend
                };
                // Make pie labels outside to avoid overlap
                seriesPie["PieLabelStyle"] = "Outside"; 
                
                foreach (var item in dataPerJenis) seriesPie.Points.AddXY(item.Jenis, item.Total);
                chartPie.Series.Add(seriesPie);

                // ---------------------------------------------
                // 3. LINE CHART (Statistik Bulanan per Wilayah)
                // ---------------------------------------------
                chartLine.Series.Clear();
                // Reset Title
                chartLine.Titles[0].Text = chartLine.Tag?.ToString(); 
                
                // Filter bulan ini
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                
                // Group by Wilayah & Tanggal (Day)
                var dataBulanan = dataList
                    .Where(x => x.Tanggal.Month == currentMonth && x.Tanggal.Year == currentYear)
                    .Select(x => new { 
                        Wilayah = x.Wilayah, 
                        Tanggal = x.Tanggal.Day, 
                        Berat = x.Berat 
                    })
                    .GroupBy(x => x.Wilayah)
                    .ToList();

                // Generate Series per Wilayah
                foreach (var wilayahGroup in dataBulanan)
                {
                    Series seriesLine = new Series(wilayahGroup.Key) 
                    {
                        ChartType = SeriesChartType.Line,
                        BorderWidth = 3,
                        MarkerStyle = MarkerStyle.Circle,
                        MarkerSize = 7
                    };

                    var dailyData = wilayahGroup
                        .GroupBy(x => x.Tanggal)
                        .Select(g => new { Hari = g.Key, Total = g.Sum(x => x.Berat) })
                        .OrderBy(x => x.Hari)
                        .ToList();

                    foreach (var d in dailyData)
                    {
                        seriesLine.Points.AddXY(d.Hari, d.Total);
                    }

                    chartLine.Series.Add(seriesLine);
                }

                // Jika data kosong
                if (dataBulanan.Count == 0)
                {
                    chartLine.Titles[0].Text += $" (Data Bulan {currentMonth}/{currentYear} Kosong)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat chart: " + ex.Message);
            }
        }
    }
}
