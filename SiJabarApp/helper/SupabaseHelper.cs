using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Pgvector;
using Newtonsoft.Json;

namespace SiJabarApp.helper
{
    public class SupabaseHelper
    {
        private readonly string _connString = "Server=aws-1-ap-northeast-2.pooler.supabase.com;" +
                                      "Port=5432;" +
                                      "Database=postgres;" +
                                      "User Id=postgres.pglfuhnonsimzqfrjphb;" +
                                      "Password=Yuji@543GM875;" +
                                      "Ssl Mode=Require;";

        public SupabaseHelper()
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connString);
            dataSourceBuilder.UseVector();
        }

        public async Task InsertDocumentAsync(string content, string userId, float[] vector)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connString);
            dataSourceBuilder.UseVector();
            await using var dataSource = dataSourceBuilder.Build();
            await using var conn = await dataSource.OpenConnectionAsync();

            string sql = "INSERT INTO documents (content, metadata, embedding) VALUES (@c, @m, @e)";
            await using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("c", content);
            cmd.Parameters.AddWithValue("m", NpgsqlTypes.NpgsqlDbType.Jsonb, JsonConvert.SerializeObject(new { userId = userId }));
            cmd.Parameters.AddWithValue("e", new Vector(vector));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> SearchSimilarAsync(float[] queryVector, string userId)
        {
            var results = new List<string>();
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connString);
            dataSourceBuilder.UseVector();
            await using var dataSource = dataSourceBuilder.Build();
            await using var conn = await dataSource.OpenConnectionAsync();

            string[] uids = { userId, "system_pdf", "system_admin" };
            
            foreach (var uid in uids)
            {
                if (string.IsNullOrEmpty(uid)) continue;

                string sql = "SELECT content FROM match_documents(@qv, @thresh, @cnt, @uid)";
                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("qv", new Vector(queryVector));
                cmd.Parameters.AddWithValue("thresh", 0.5); 
                cmd.Parameters.AddWithValue("cnt", uid == "system_pdf" ? 10 : 5);
                cmd.Parameters.AddWithValue("uid", uid);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string content = reader.GetString(0);
                    if (!results.Contains(content))
                    {
                        results.Add(content);
                    }
                }
            }
            
            return results;
        }
    }
}
