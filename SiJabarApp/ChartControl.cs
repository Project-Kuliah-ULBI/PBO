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
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(10);

            tableLayout = new TableLayoutPanel();
            tableLayout.Dock = DockStyle.Fill;
            tableLayout.ColumnCount = 2;
            tableLayout.RowCount = 2;
            tableLayout.Padding = new Padding(5);
            
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            this.Controls.Add(tableLayout);

            chartBar = CreateChart("Grafik Batang: Total Berat per Jenis", "Jenis Sampah", "Berat (kg)");
            chartPie = CreateChart("Grafik Lingkaran: Persentase Jenis Sampah", "", "");
            chartLine = CreateChart("Statistik Bulanan: Tren Sampah per Wilayah", "Hari", "Berat (kg)");

            tableLayout.Controls.Add(CreateDataCard(chartBar), 0, 0); 
            tableLayout.Controls.Add(CreateDataCard(chartPie), 1, 0);
            
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
            card.Margin = new Padding(10);
            
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
                var client = new MongoClient(MongoHelper.ConnectionString);
                var database = client.GetDatabase(MongoHelper.DatabaseName);
                collectionSampah = database.GetCollection<SampahModel>("Sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi Database Gagal: " + ex.Message);
            }
        }

        public async void LoadDataToChart()
        {
            if (collectionSampah == null) return;

            try
            {
                var dataList = await collectionSampah.Find(_ => true).ToListAsync();
                if (dataList.Count == 0) return;

                // Debug: tampilkan jumlah data dan jenis unik
                var allJenis = dataList.Select(x => x.Jenis).Where(j => !string.IsNullOrEmpty(j)).Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"Total data: {dataList.Count}, Jenis unik: {string.Join(", ", allJenis)}");

                var dataPerJenis = dataList
                    .Where(x => !string.IsNullOrEmpty(x.Jenis))
                    .GroupBy(x => x.Jenis.Trim())
                    .Select(g => new { Jenis = g.Key, Total = Math.Round(g.Sum(x => x.Berat), 1) })
                    .OrderByDescending(x => x.Total)
                    .ToList();

                // Palet warna untuk konsistensi warna antara Bar dan Pie
                Color[] chartColors = {
                    Color.FromArgb(54, 162, 235),    // biru
                    Color.FromArgb(255, 159, 64),    // oranye
                    Color.FromArgb(255, 99, 132),    // merah/pink
                    Color.FromArgb(0, 100, 80),      // hijau tua
                    Color.FromArgb(201, 203, 207),   // abu-abu
                    Color.FromArgb(153, 102, 255),   // ungu
                    Color.FromArgb(255, 205, 86),    // kuning
                    Color.FromArgb(75, 192, 192),    // teal
                };

                // ============ BAR CHART ============
                chartBar.Series.Clear();
                chartBar.Legends["Legend1"].Enabled = false; // sembunyikan legend bar
                chartBar.ChartAreas["MainArea"].AxisX.LabelStyle.Angle = -30;
                chartBar.ChartAreas["MainArea"].AxisX.LabelStyle.Font = new Font("Segoe UI", 8);

                Series seriesBar = new Series("Berat")
                {
                    ChartType = SeriesChartType.Column,
                    IsValueShownAsLabel = true,
                    LabelFormat = "N1",
                };
                seriesBar.SmartLabelStyle.Enabled = true;
                seriesBar["PointWidth"] = "0.6";

                for (int i = 0; i < dataPerJenis.Count; i++)
                {
                    var item = dataPerJenis[i];
                    var pt = new DataPoint();
                    pt.SetValueXY(i + 1, item.Total);
                    pt.AxisLabel = item.Jenis;
                    pt.Color = chartColors[i % chartColors.Length];
                    seriesBar.Points.Add(pt);
                }
                chartBar.Series.Add(seriesBar);

                // ============ PIE CHART ============
                chartPie.Series.Clear();
                Series seriesPie = new Series("Persentase")
                {
                    ChartType = SeriesChartType.Pie,
                    IsValueShownAsLabel = true,
                    LabelFormat = "{0:P0}",
                    LegendText = "#VALX"
                };
                seriesPie["PieLabelStyle"] = "Outside"; 
                seriesPie.SmartLabelStyle.Enabled = true;

                for (int i = 0; i < dataPerJenis.Count; i++)
                {
                    var item = dataPerJenis[i];
                    int idx = seriesPie.Points.AddXY(item.Jenis, item.Total);
                    seriesPie.Points[idx].Color = chartColors[i % chartColors.Length];
                }
                chartPie.Series.Add(seriesPie);

                // ============ LINE CHART ============
                chartLine.Series.Clear();
                chartLine.Titles[0].Text = chartLine.Tag?.ToString(); 
                
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                
                var rawDataBulanan = dataList
                    .Where(x => x.Tanggal.Month == currentMonth && x.Tanggal.Year == currentYear)
                    .Select(x => new { 
                        Wilayah = x.Wilayah ?? "Unknown", 
                        Tanggal = x.Tanggal.Day, 
                        Berat = x.Berat 
                    })
                    .ToList();

                var dataPerWilayah = rawDataBulanan.GroupBy(x => x.Wilayah).ToList();
                foreach (var wilayahGroup in dataPerWilayah)
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

                    foreach (var d in dailyData) seriesLine.Points.AddXY(d.Hari, d.Total);
                    chartLine.Series.Add(seriesLine);
                }

                if (!rawDataBulanan.Any())
                {
                    chartLine.Titles[0].Text += $" (Tidak ada data untuk {currentMonth}/{currentYear})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load chart: " + ex.Message);
            }
        }
    }
}
