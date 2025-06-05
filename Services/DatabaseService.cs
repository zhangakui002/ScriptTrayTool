using Microsoft.Data.Sqlite;
using ScriptTrayTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ScriptTrayTool.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScriptTrayTool");
            Directory.CreateDirectory(appDataPath);

            _databasePath = Path.Combine(appDataPath, "scripttray.db");
            _connectionString = $"Data Source={_databasePath}";
        }

        public async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createScriptsTable = @"
                CREATE TABLE IF NOT EXISTS Scripts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    ScriptType TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    LastExecuted DATETIME
                )";

            var createSettingsTable = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                )";

            using var command = new SqliteCommand(createScriptsTable, connection);
            await command.ExecuteNonQueryAsync();

            command.CommandText = createSettingsTable;
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Script>> GetAllScriptsAsync()
        {
            var scripts = new List<Script>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Id, Name, ScriptType, Content, LastExecuted FROM Scripts ORDER BY Name";
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                scripts.Add(new Script
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    ScriptType = Enum.Parse<ScriptType>(reader.GetString(2)),
                    Content = reader.GetString(3),
                    LastExecuted = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                });
            }

            return scripts;
        }

        public async Task<Script?> GetScriptByIdAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Id, Name, ScriptType, Content, LastExecuted FROM Scripts WHERE Id = @id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Script
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    ScriptType = Enum.Parse<ScriptType>(reader.GetString(2)),
                    Content = reader.GetString(3),
                    LastExecuted = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                };
            }

            return null;
        }

        public async Task<int> SaveScriptAsync(Script script)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            if (script.Id == 0)
            {
                // Insert new script
                var sql = @"
                    INSERT INTO Scripts (Name, ScriptType, Content, LastExecuted) 
                    VALUES (@name, @scriptType, @content, @lastExecuted);
                    SELECT last_insert_rowid();";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", script.Name);
                command.Parameters.AddWithValue("@scriptType", script.ScriptType.ToString());
                command.Parameters.AddWithValue("@content", script.Content);
                command.Parameters.AddWithValue("@lastExecuted", script.LastExecuted?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            else
            {
                // Update existing script
                var sql = @"
                    UPDATE Scripts 
                    SET Name = @name, ScriptType = @scriptType, Content = @content, LastExecuted = @lastExecuted 
                    WHERE Id = @id";

                using var command = new SqliteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", script.Id);
                command.Parameters.AddWithValue("@name", script.Name);
                command.Parameters.AddWithValue("@scriptType", script.ScriptType.ToString());
                command.Parameters.AddWithValue("@content", script.Content);
                command.Parameters.AddWithValue("@lastExecuted", script.LastExecuted?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
                return script.Id;
            }
        }

        public async Task DeleteScriptAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM Scripts WHERE Id = @id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateScriptLastExecutedAsync(int id, DateTime lastExecuted)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "UPDATE Scripts SET LastExecuted = @lastExecuted WHERE Id = @id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@lastExecuted", lastExecuted.ToString("yyyy-MM-dd HH:mm:ss"));

            await command.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT Value FROM Settings WHERE Key = @key";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@key", key);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task SetSettingAsync(string key, string value)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT OR REPLACE INTO Settings (Key, Value) 
                VALUES (@key, @value)";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@value", value);

            await command.ExecuteNonQueryAsync();
        }
    }
}
