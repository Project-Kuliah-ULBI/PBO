using MongoDB.Driver;
using System;
using System.Linq;

namespace SiJabarApp
{
    public class MongoHelper
    {
        private IMongoCollection<User> usersCollection;

        public MongoHelper()
        {
            var client = new MongoClient("mongodb://127.0.0.1:27017");
            var database = client.GetDatabase("SiJabarDB");
            usersCollection = database.GetCollection<User>("Users");
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

                // 3. Hash Password
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // 4. Simpan
                var newUser = new User
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
                message = "Gagal terhubung ke Database! Error: " + ex.Message;
                return false;
            }
        }

        public bool LoginUser(string email, string password, out string message, out string userName)
        {
            userName = "";
            try
            {
                // Cari user dengan email lowercase
                string cleanEmail = email.Trim().ToLower();
                var user = usersCollection.Find(u => u.Email == cleanEmail).FirstOrDefault();

                if (user == null)
                {
                    message = "Email tidak ditemukan!";
                    return false;
                }

                bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (validPassword)
                {
                    userName = user.Fullname;
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