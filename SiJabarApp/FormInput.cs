using System;
using System.Windows.Forms;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Runtime.InteropServices;

namespace SiJabarApp
{
    public partial class FormInput : Form
    {
        // Variabel Global
        private IMongoCollection<SampahModel> collection;
        private string currentId = null; // Menyimpan ID jika sedang mode Edit

        // --- SETUP DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // KONSTRUKTOR 1: MODE TAMBAH BARU (Dipanggil saat tombol Tambah diklik)
        public FormInput()
        {
            InitializeComponent();
            ConnectDB();
            lblHeaderTitle.Text = "INPUT DATA SAMPAH"; // Judul Header
        }

        // KONSTRUKTOR 2: MODE EDIT (Dipanggil saat tombol Edit diklik)
        // Ini yang tadi bikin error karena belum ada!
        public FormInput(string id)
        {
            InitializeComponent();
            ConnectDB();
            this.currentId = id; // Simpan ID yang dikirim

            lblHeaderTitle.Text = "EDIT DATA SAMPAH"; // Ganti Judul
            btnSimpan.Text = "UPDATE";           // Ganti Teks Tombol

            LoadDataForEdit(); // Isi form dengan data lama
        }

        // --- KONEKSI DATABASE ---
        private void ConnectDB()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("SiJabarDB");
            collection = db.GetCollection<SampahModel>("Sampah");
        }

        // --- ISI FORM OTOMATIS (KHUSUS EDIT) ---
        private void LoadDataForEdit()
        {
            try
            {
                // Cari data berdasarkan ID
                var filter = Builders<SampahModel>.Filter.Eq(x => x.Id, currentId);
                var data = collection.Find(filter).FirstOrDefault();

                if (data != null)
                {
                    // Masukkan data db ke kotak isian
                    comboWilayah.Text = data.Wilayah; // Dropdown Wilayah
                    comboJenis.Text = data.Jenis;
                    numBerat.Value = (decimal)data.Berat;
                    comboStatus.Text = data.Status;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memuat data: " + ex.Message);
            }
        }

        // --- LOGIKA TOMBOL SIMPAN / UPDATE ---
        private void btnSimpan_Click(object sender, EventArgs e)
        {
            // 1. Validasi: Cek apakah input kosong?
            if (string.IsNullOrEmpty(comboWilayah.Text) || string.IsNullOrEmpty(comboJenis.Text))
            {
                MessageBox.Show("Harap isi semua data!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Siapkan Objek Data
            var sampah = new SampahModel
            {
                Wilayah = comboWilayah.Text,
                Jenis = comboJenis.Text,
                Berat = (double)numBerat.Value,
                Status = comboStatus.Text
            };

            try
            {
                if (currentId == null)
                {
                    // === MODE TAMBAH BARU (INSERT) ===
                    // MongoDB akan otomatis bikin ID baru
                    collection.InsertOne(sampah);
                    MessageBox.Show("Data berhasil disimpan!");
                }
                else
                {
                    // === MODE EDIT (UPDATE) ===
                    // Pasang ID lama ke objek baru agar tidak dianggap data baru
                    sampah.Id = currentId;

                    var filter = Builders<SampahModel>.Filter.Eq(x => x.Id, currentId);
                    collection.ReplaceOne(filter, sampah);
                    MessageBox.Show("Data berhasil diperbarui!");
                }

                this.DialogResult = DialogResult.OK; // Beri sinyal SUKSES ke MainForm
                this.Close(); // Tutup FormInput
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // --- TOMBOL BATAL & CLOSE ---
        private void btnBatal_Click(object sender, EventArgs e) => this.Close();
        private void btnClose_Click(object sender, EventArgs e) => this.Close();

        // --- DRAG WINDOW ---
        private void panelHeader_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }
    }
}