using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using PDFCompressor.Core.Models;

namespace PDFCompressor.Core.Services
{
    public class HistoryStatistics
    {
        public int TotalFilesCompressed { get; set; }
        public long TotalOriginalBytes { get; set; }
        public long TotalCompressedBytes { get; set; }
        public long TotalSpaceSavedBytes { get; set; }
        public double AverageReductionPercentage { get; set; }
    }

    public class HistoryService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public HistoryService(string? customDbPath = null)
        {
            if (string.IsNullOrWhiteSpace(customDbPath))
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PDFCompressor");
                Directory.CreateDirectory(dir);
                _dbPath = Path.Combine(dir, "history.db");
            }
            else
            {
                _dbPath = customDbPath;
            }

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            InitializeDatabase();
        }

        public string DatabasePath => _dbPath;

        private void InitializeDatabase()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    CREATE TABLE IF NOT EXISTS CompressionHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        FileName TEXT NOT NULL,
                        OriginalPath TEXT NOT NULL,
                        OutputPath TEXT NOT NULL,
                        OriginalSizeBytes INTEGER NOT NULL,
                        CompressedSizeBytes INTEGER NOT NULL,
                        SpaceSavedBytes INTEGER NOT NULL,
                        ReductionPercentage REAL NOT NULL,
                        PresetName TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        DurationSeconds REAL NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS idx_history_timestamp ON CompressionHistory(Timestamp DESC);
                    CREATE INDEX IF NOT EXISTS idx_history_filename ON CompressionHistory(FileName);
                ";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize SQLite history database.", ex);
            }
        }

        public bool AddRecord(HistoryRecord record)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    INSERT INTO CompressionHistory 
                    (Timestamp, FileName, OriginalPath, OutputPath, OriginalSizeBytes, CompressedSizeBytes, SpaceSavedBytes, ReductionPercentage, PresetName, Status, DurationSeconds)
                    VALUES 
                    (@Timestamp, @FileName, @OriginalPath, @OutputPath, @OriginalSizeBytes, @CompressedSizeBytes, @SpaceSavedBytes, @ReductionPercentage, @PresetName, @Status, @DurationSeconds);
                    SELECT last_insert_rowid();
                ";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("o"));
                cmd.Parameters.AddWithValue("@FileName", record.FileName ?? string.Empty);
                cmd.Parameters.AddWithValue("@OriginalPath", record.OriginalPath ?? string.Empty);
                cmd.Parameters.AddWithValue("@OutputPath", record.OutputPath ?? string.Empty);
                cmd.Parameters.AddWithValue("@OriginalSizeBytes", record.OriginalSizeBytes);
                cmd.Parameters.AddWithValue("@CompressedSizeBytes", record.CompressedSizeBytes);
                cmd.Parameters.AddWithValue("@SpaceSavedBytes", record.SpaceSavedBytes);
                cmd.Parameters.AddWithValue("@ReductionPercentage", record.ReductionPercentage);
                cmd.Parameters.AddWithValue("@PresetName", record.PresetName ?? string.Empty);
                cmd.Parameters.AddWithValue("@Status", record.Status ?? "Succeeded");
                cmd.Parameters.AddWithValue("@DurationSeconds", record.DurationSeconds);

                var id = cmd.ExecuteScalar();
                if (id != null && long.TryParse(id.ToString(), out long newId))
                {
                    record.Id = newId;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to add history record.", ex);
                return false;
            }
        }

        public List<HistoryRecord> GetAll(string? searchQuery = null, string sortBy = "DateDesc")
        {
            var results = new List<HistoryRecord>();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string orderClause = sortBy switch
                {
                    "DateAsc" => "Timestamp ASC",
                    "SizeSavedDesc" => "SpaceSavedBytes DESC",
                    "RatioDesc" => "ReductionPercentage DESC",
                    "FileNameAsc" => "FileName ASC",
                    _ => "Timestamp DESC"
                };

                string sql = "SELECT Id, Timestamp, FileName, OriginalPath, OutputPath, OriginalSizeBytes, CompressedSizeBytes, SpaceSavedBytes, ReductionPercentage, PresetName, Status, DurationSeconds FROM CompressionHistory";
                
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    sql += " WHERE FileName LIKE @Query OR OriginalPath LIKE @Query OR OutputPath LIKE @Query";
                }

                sql += $" ORDER BY {orderClause} LIMIT 500";

                using var cmd = new SqliteCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    cmd.Parameters.AddWithValue("@Query", $"%{searchQuery.Trim()}%");
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new HistoryRecord
                    {
                        Id = reader.GetInt64(0),
                        Timestamp = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.UtcNow,
                        FileName = reader.GetString(2),
                        OriginalPath = reader.GetString(3),
                        OutputPath = reader.GetString(4),
                        OriginalSizeBytes = reader.GetInt64(5),
                        CompressedSizeBytes = reader.GetInt64(6),
                        SpaceSavedBytes = reader.GetInt64(7),
                        ReductionPercentage = reader.GetDouble(8),
                        PresetName = reader.GetString(9),
                        Status = reader.GetString(10),
                        DurationSeconds = reader.GetDouble(11)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to query history records.", ex);
            }
            return results;
        }

        public List<HistoryRecord> GetRecent(int count = 5)
        {
            var results = new List<HistoryRecord>();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = "SELECT Id, Timestamp, FileName, OriginalPath, OutputPath, OriginalSizeBytes, CompressedSizeBytes, SpaceSavedBytes, ReductionPercentage, PresetName, Status, DurationSeconds FROM CompressionHistory ORDER BY Timestamp DESC LIMIT @Count";

                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Count", count);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(new HistoryRecord
                    {
                        Id = reader.GetInt64(0),
                        Timestamp = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.UtcNow,
                        FileName = reader.GetString(2),
                        OriginalPath = reader.GetString(3),
                        OutputPath = reader.GetString(4),
                        OriginalSizeBytes = reader.GetInt64(5),
                        CompressedSizeBytes = reader.GetInt64(6),
                        SpaceSavedBytes = reader.GetInt64(7),
                        ReductionPercentage = reader.GetDouble(8),
                        PresetName = reader.GetString(9),
                        Status = reader.GetString(10),
                        DurationSeconds = reader.GetDouble(11)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to query recent history.", ex);
            }
            return results;
        }

        public bool DeleteRecord(long id)
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = "DELETE FROM CompressionHistory WHERE Id = @Id";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete history record {id}.", ex);
                return false;
            }
        }

        public bool ClearAll()
        {
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = "DELETE FROM CompressionHistory";
                using var cmd = new SqliteCommand(sql, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to clear history.", ex);
                return false;
            }
        }

        public HistoryStatistics GetStatistics()
        {
            var stats = new HistoryStatistics();
            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                string sql = @"
                    SELECT 
                        COUNT(*), 
                        TOTAL(OriginalSizeBytes), 
                        TOTAL(CompressedSizeBytes), 
                        TOTAL(SpaceSavedBytes),
                        AVG(ReductionPercentage)
                    FROM CompressionHistory 
                    WHERE Status = 'Succeeded';
                ";

                using var cmd = new SqliteCommand(sql, conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    stats.TotalFilesCompressed = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    stats.TotalOriginalBytes = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetDouble(1));
                    stats.TotalCompressedBytes = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetDouble(2));
                    stats.TotalSpaceSavedBytes = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetDouble(3));
                    stats.AverageReductionPercentage = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to calculate history statistics.", ex);
            }
            return stats;
        }
    }
}
