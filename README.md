# 🗺️ SI - JABAR (Sistem Informasi Sampah Jawa Barat)

Aplikasi **desktop Windows** untuk pengelolaan dan pemantauan sampah di wilayah Jawa Barat. Dibangun dengan **C# WinForms (.NET 8)** menggunakan arsitektur berbasis Object-Oriented Programming.

> 📚 Tugas AAS Kelompok Mata Kuliah **Pemrograman Berorientasi Objek (PBO)** — Semester 3, ULBI

---

## ✨ Fitur Utama

### 📊 Dashboard
- Ringkasan statistik: total sampah, jadwal pengangkutan, lokasi aktif, TPS/TPA penuh
- Grafik bar chart & pie chart interaktif
- Peta lokasi pengguna terintegrasi (WebView2 + Leaflet.js)

### 🗃️ Manajemen Data Sampah (CRUD)
- Tambah, edit, dan hapus laporan sampah
- Filter data berdasarkan role pengguna
- DataGridView dengan styling modern

### 🗺️ Peta Sebaran Sampah
- Peta interaktif berbasis **Leaflet.js** melalui **WebView2**
- Marker lokasi TPS/TPA dan titik laporan sampah
- Mode pick lokasi untuk input koordinat

### 📈 Statistik & Visualisasi
- Grafik batang jumlah sampah per jenis
- Grafik pie distribusi sampah per wilayah
- Data real-time dari database

### 🤖 AI Chatbot (RAG)
- Chatbot pintar menggunakan **Mistral AI**
- Retrieval-Augmented Generation (RAG) dengan **Supabase pgvector**
- Menjawab berdasarkan data aktual dari database
- Riwayat percakapan per pengguna

### 📥 Import & Export
- **Import CSV** — bulk import data sampah
- **Import PDF** — ingest dokumen ke RAG knowledge base
- **Export PDF** — laporan sampah dalam format PDF (menggunakan iText7)

### 🔐 Autentikasi & Role-Based Access
| Role | Akses |
|---|---|
| **Admin** | Full access + Sync RAG + Import CSV |
| **Petugas** | Tambah & lihat data |
| **Masyarakat** | Map & chatbot |

---

## 🛠️ Tech Stack

| Komponen | Teknologi |
|---|---|
| **Framework** | .NET 8, Windows Forms |
| **Bahasa** | C# |
| **Database** | MongoDB Atlas |
| **Peta** | Leaflet.js via WebView2 |
| **AI/LLM** | Mistral AI API |
| **Vector DB** | Supabase (PostgreSQL + pgvector) |
| **PDF Export** | iText7 |
| **Icons** | FontAwesome.Sharp |
| **Auth** | BCrypt.Net (password hashing) |

---

## 📁 Struktur Project

```
PBO/
├── SiJabarApp/
│   ├── Program.cs                  # Entry point
│   ├── FormAuth.cs                 # Halaman login & register
│   ├── MainForm.cs                 # Form utama + navigasi
│   ├── DashboardControl.cs         # Tab Dashboard
│   ├── MapControl.cs               # Tab Peta Sebaran
│   ├── ChartControl.cs             # Tab Statistik
│   ├── FileIOControl.cs            # Tab Import/Export
│   ├── Chatbot.cs                  # Popup AI Chatbot
│   ├── FormMap.cs                  # Form peta fullscreen
│   ├── FormInput.cs                # Form input data sampah
│   ├── FormMasterLokasi.cs         # CRUD lokasi TPS/TPA
│   ├── map.html                    # Template peta Leaflet.js
│   ├── helper/
│   │   ├── MongoHelper.cs          # Koneksi & operasi MongoDB
│   │   ├── MistralHelper.cs        # Integrasi Mistral AI API
│   │   ├── SupabaseHelper.cs       # Vector search (RAG)
│   │   ├── StyleHelper.cs          # Utilities: styling, CleanText, RepairCoordinate
│   │   ├── CsvIngestionHelper.cs   # Import CSV ke MongoDB
│   │   └── PdfIngestionHelper.cs   # Import PDF ke RAG
│   └── model/
│       ├── User.cs                 # Model pengguna
│       ├── SampahModel.cs          # Model data sampah
│       ├── MasterLokasiModel.cs    # Model lokasi TPS/TPA
│       └── ChatLog.cs              # Model riwayat chat
├── SiJabarApp.sln                  # Solution file
├── SiJabarApp.iss                  # Script Inno Setup (installer)
├── INNO_SETUP_TUTORIAL.md          # Tutorial pembuatan installer
└── README.md
```

---

## 🚀 Cara Menjalankan

### Prasyarat
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (biasanya sudah terinstall di Windows 10/11)
- Koneksi internet (untuk MongoDB Atlas & Mistral AI)

### Menjalankan dari Source Code
```powershell
cd SiJabarApp
dotnet restore
dotnet run
```
