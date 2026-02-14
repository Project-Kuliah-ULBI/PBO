using MongoDB.Driver;
using SiJabarApp.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiJabarApp.helper
{
    public class MongoHelper
    {
        private IMongoCollection<model.User> usersCollection;
        public const string ConnectionString = "mongodb+srv://root:root123@sijabardb.ak2nw4q.mongodb.net/?appName=SiJabarDB";
        public const string DatabaseName = "SiJabarDB";

        public MongoHelper()
        {
            try
            {
                var client = new MongoClient(ConnectionString);
                var database = client.GetDatabase(DatabaseName);
                usersCollection = database.GetCollection<model.User>("Users");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database initialization failed: " + ex.Message);
            }
        }

        public bool RegisterUser(string name, string email, string password, out string message)
        {
            try
            {
                string cleanEmail = email.Trim().ToLower();
                var existingUser = usersCollection.Find(u => u.Email == cleanEmail).FirstOrDefault();

                if (existingUser != null)
                {
                    message = "Email already registered.";
                    return false;
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                var newUser = new model.User
                {
                    Fullname = name,
                    Email = cleanEmail,
                    Password = passwordHash,
                    Role = "Masyarakat"
                };

                usersCollection.InsertOne(newUser);
                message = "Registration successful.";
                return true;
            }
            catch (Exception ex)
            {
                message = "Database connection error: " + ex.Message;
                return false;
            }
        }

        public bool LoginUser(string email, string password, out string message, out string userName, out string userId, out string role)
        {
            userName = "";
            userId = "";
            role = "";

            try
            {
                string cleanEmail = email.Trim().ToLower();
                var user = usersCollection.Find(u => u.Email == cleanEmail).FirstOrDefault();

                if (user == null)
                {
                    message = "Email not found.";
                    return false;
                }

                bool validPassword = BCrypt.Net.BCrypt.Verify(password, user.Password);
                if (validPassword)
                {
                    userName = user.Fullname;
                    userId = user.Id;
                    role = user.Role;
                    message = "Login successful.";
                    return true;
                }
                else
                {
                    message = "Invalid password.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = "Login failed: " + ex.Message;
                return false;
            }
        }

        public bool UpdateUserLocation(string userId, double lat, double lon)
        {
            try
            {
                if (usersCollection == null) return false;
                var filter = Builders<model.User>.Filter.Eq(u => u.Id, userId);
                var update = Builders<model.User>.Update
                    .Set(u => u.Latitude, lat)
                    .Set(u => u.Longitude, lon);
                
                var result = usersCollection.UpdateOne(filter, update);
                return result.ModifiedCount > 0;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateUserLocationAsync(string userId, double lat, double lon)
        {
            try
            {
                if (usersCollection == null) return false;
                var filter = Builders<model.User>.Filter.Eq(u => u.Id, userId);
                var update = Builders<model.User>.Update
                    .Set(u => u.Latitude, lat)
                    .Set(u => u.Longitude, lon);

                var result = await usersCollection.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch { return false; }
        }

        public List<model.User> GetAllUsers()
        {
            try
            {
                if (usersCollection == null) return new List<model.User>();
                return usersCollection.Find(u => u.Latitude != 0 && u.Longitude != 0).ToList();
            }
            catch { return new List<model.User>(); }
        }

        public async Task<List<model.User>> GetAllUsersAsync()
        {
            try
            {
                if (usersCollection == null) return new List<model.User>();
                return await usersCollection.Find(u => u.Latitude != 0 && u.Longitude != 0).ToListAsync();
            }
            catch { return new List<model.User>(); }
        }
    }
}
