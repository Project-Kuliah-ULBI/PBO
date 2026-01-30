using Microsoft.VisualBasic.ApplicationServices;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto.Generators;
using SiJabarApp.model;
using System;
using System.Linq;

namespace SiJabarApp.helper
{
    public class MongoHelper
    {
        private IMongoCollection<model.User> usersCollection;
        // String koneksi MongoDB Atlas Anda
        // private readonly string connectionString = "mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB";
        private readonly string connectionString = "mongodb://localhost:27017";
        private readonly string databaseName = "SiJabarDB";

        public MongoHelper()
        {
            try
            {
                // Inisialisasi client dengan koneksi Atlas
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                usersCollection = database.GetCollection<model.User>("Users");
            }
            catch (Exception ex)
            {
                // Log error jika inisialisasi gagal
                System.Diagnostics.Debug.WriteLine("Gagal inisialisasi MongoDB Atlas: " + ex.Message);
            }
        }

        public bool RegisterUser(string name, string email, string password, out string message)
        {
            try
            {
                // 1. Standarisasi Email ke Huruf Kecil (Agar UNIK tidak peduli besar/kecil)
                string cleanEmail = email.Trim().ToLower();

                // 2. CEK APAKAH EMAIL SUDAH ADA (UNIQUE CHECK)
                var existingUser = usersCollection.Find(u => u.Email == cleanEmail).FirstOrDefault();

                if (existingUser != null)
                {
                    message = "Email sudah terdaftar! Silakan gunakan email lain.";
                    return false;
                }

                // 3. Hash Password menggunakan BCrypt
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // 4. Simpan User Baru
                var newUser = new model.User
                {
                    Fullname = name,
                    Email = cleanEmail, // Simpan yang sudah lowercase
                    Password = passwordHash
                };

                usersCollection.InsertOne(newUser);
                message = "Registrasi Berhasil! Silakan Login.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Gagal terhubung ke Database Atlas! Error: " + ex.Message;
                return false;
            }
        }

        public bool LoginUser(string email, string password, out string message, out string userName, out string userId)
        {
            userName = "";
            userId = ""; // Default kosong
            try
            {
                string cleanEmail = email.Trim().ToLower();
                var user = usersCollection.Find(u => u.Email == cleanEmail).FirstOrDefault();

                if (user == null)
                {
                    message = "Email tidak ditemukan!";
                    return false;
                }

                // Verifikasi password yang di-hash
                bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (validPassword)
                {
                    userName = user.Fullname;
                    userId = user.Id; // Mengambil ID User
                    message = "Login Berhasil!";
                    return true;
                }
                else
                {
                    message = "Password Salah!";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = "Gagal Login. Error: " + ex.Message;
                return false;
            }
        }
    }
}