using System;
using System.Windows.Forms;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Runtime.InteropServices;
using SiJabarApp.helper;

namespace SiJabarApp
{
    public partial class FormInput : Form
    {
        // Variabel Global
        private IMongoCollection<SampahModel> collection;
        private string currentId = null; // Menyimpan ID jika sedang mode Edit
        private string ownerUserId;

        // --- SETUP DRAG WINDOW ---
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // KONSTRUKTOR 1: MODE TAMBAH BARU (Dipanggil saat tombol Tambah diklik)
        public FormInput(string userId)
        {
            InitializeComponent();
            ConnectDB();

            this.ownerUserId = userId; // Simpan ID User

            lblHeaderTitle.Text = "INPUT DATA SAMPAH";
        }

        // KONSTRUKTOR 2: MODE EDIT (Terima ID Data Sampah)
        public FormInput(string sampahId, bool isEditMode = true)
        {
            InitializeComponent();
            ConnectDB();

            this.currentId = sampahId;

            lblHeaderTitle.Text = "EDIT DATA SAMPAH";
            btnSimpan.Text = "UPDATE DATA";

            LoadDataForEdit();
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
                var filter = Builders<SampahModel>.Filter.Eq(x => x.Id, currentId);
                var data = collection.Find(filter).FirstOrDefault();

                if (data != null)
                {
                    comboWilayah.Text = data.Wilayah;
                    comboJenis.Text = data.Jenis;
                    numBerat.Value = (decimal)data.Berat;
                    comboStatus.Text = data.Status;

                    // PENTING: Ambil UserId asli agar tidak hilang saat update
                    this.ownerUserId = data.UserId;
                }
            }
            catch (Exception ex) { MessageBox.Show("Gagal memuat data: " + ex.Message); }
        }

        private async void btnSimpan_ClickAsync(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(comboWilayah.Text) || string.IsNullOrEmpty(comboJenis.Text))
            {
                MessageBox.Show("Harap isi semua data!");
                return;
            }

            btnSimpan.Enabled = false;
            this.Cursor = Cursors.WaitCursor;

            var sampah = new SampahModel
            {
                UserId = this.ownerUserId,
                Wilayah = comboWilayah.Text,
                Jenis = comboJenis.Text,
                Berat = (double)numBerat.Value,
                Status = comboStatus.Text
            };

            try
            {
                // 1. SIMPAN KE MONGODB (LOKAL)
                if (currentId == null) collection.InsertOne(sampah);
                else
                {
                    sampah.Id = currentId;
                    var filter = Builders<SampahModel>.Filter.Eq(x => x.Id, currentId);
                    collection.ReplaceOne(filter, sampah);
                }

                // 2. SIMPAN KE SUPABASE (CLOUD)
                try
                {
                    string textRAG = $"User di {sampah.Wilayah} membuang sampah {sampah.Jenis} " +
                                     $"seberat {sampah.Berat} kg dengan status {sampah.Status}.";

                    // Cek Embedding dulu
                    float[] vector = await MistralHelper.GetEmbedding(textRAG);

                    if (vector == null)
                    {
                        throw new Exception("Gagal mendapatkan Vector dari Mistral AI. Cek API Key Anda.");
                    }

                    var supaHelper = new SupabaseHelper();
                    await supaHelper.InsertDocumentAsync(textRAG, this.ownerUserId, vector);

                    MessageBox.Show("Sukses! Data masuk ke MongoDB Lokal & Supabase Cloud.");
                }
                catch (Exception exSupa)
                {
                    // TAMPILKAN ERROR SUPABASE DI LAYAR
                    MessageBox.Show($"Data masuk ke MongoDB, TAPI GAGAL ke Supabase.\n\nPenyebab: {exSupa.Message}",
                                    "Error Koneksi Cloud", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Database Lokal: " + ex.Message);
            }
            finally
            {
                btnSimpan.Enabled = true;
                this.Cursor = Cursors.Default;
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