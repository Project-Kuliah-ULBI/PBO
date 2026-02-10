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
    public partial class FormChart : Form
    {
        private IMongoCollection<SampahModel> collectionSampah;
        private Chart chartSampah;

        public FormChart()
        {
            // Inisialisasi Form
            this.Text = "Statistik Sampah Jawa Barat";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            InitializeChartControl();
            ConnectDB();
            LoadDataToChart();
        }

        private void InitializeChartControl()
        {
            // Membuat instance Chart baru
            chartSampah = new Chart();
            chartSampah.Dock = DockStyle.Fill;

            // Membuat Area Grafik
            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.AxisX.Title = "Jenis Sampah";
            chartArea.AxisX.TitleFont = new Font("Segoe UI", 10, FontStyle.Bold);
            chartArea.AxisX.Interval = 1;

            chartArea.AxisY.Title = "Total Berat (Kg)";
            chartArea.AxisY.TitleFont = new Font("Segoe UI", 10, FontStyle.Bold);

            chartSampah.ChartAreas.Add(chartArea);

            // Membuat Judul Grafik
            Title chartTitle = new Title("Grafik Jumlah Sampah per Jenis");
            chartTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            chartTitle.ForeColor = Color.DarkSlateGray;
            chartSampah.Titles.Add(chartTitle);

            // Membuat Legenda
            Legend legend = new Legend("Legend1");
            legend.Docking = Docking.Bottom;
            chartSampah.Legends.Add(legend);

            // Menambahkan chart ke dalam form
            this.Controls.Add(chartSampah);
        }

        private void ConnectDB()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("SiJabarDB");
                collectionSampah = database.GetCollection<SampahModel>("Sampah");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi Database Gagal: " + ex.Message);
            }
        }

        private void LoadDataToChart()
        {
            try
            {
                // Mengambil data dari MongoDB
                var dataList = collectionSampah.Find(_ => true).ToList();

                // Mengelompokkan data berdasarkan Jenis dan menjumlahkan Beratnya
                var ringkasanData = dataList
                    .GroupBy(x => x.Jenis)
                    .Select(g => new {
                        Jenis = g.Key,
                        TotalBerat = g.Sum(x => x.Berat)
                    })
                    .OrderByDescending(x => x.TotalBerat)
                    .ToList();

                // Bersihkan series lama jika ada
                chartSampah.Series.Clear();

                // Membuat Series Baru (Grafik Batang/Column)
                Series series = new Series("Berat Total")
                {
                    ChartType = SeriesChartType.Column,
                    XValueType = ChartValueType.String,
                    YValueType = ChartValueType.Double,
                    IsValueShownAsLabel = true, // Menampilkan angka di atas batang
                    Font = new Font("Segoe UI", 9),
                    Color = Color.MediumSeaGreen,
                    LabelFormat = "{0} Kg"
                };

                // Menambahkan data ke dalam series
                foreach (var item in ringkasanData)
                {
                    series.Points.AddXY(item.Jenis, item.TotalBerat);
                }

                // Menambahkan series ke dalam chart
                chartSampah.Series.Add(series);

                // Refresh tampilan
                chartSampah.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat visualisasi data: " + ex.Message);
            }
        }
    }
}