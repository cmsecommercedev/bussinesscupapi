using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace BussinessCupApi.Jobs
{
    public class DbBackupJob
    {
        private readonly CloudflareR2Manager _r2Service;
        private readonly DatabaseBackupOptions _dbOptions;
        private readonly ILogger<DbBackupJob> _logger;
        private readonly IHostEnvironment _env;

        public DbBackupJob(
            ILogger<DbBackupJob> logger,
            CloudflareR2Manager r2Service,
            IOptions<DatabaseBackupOptions> dbOptions,
            IHostEnvironment env)
        {
            _logger = logger;
            _r2Service = r2Service;
            _dbOptions = dbOptions.Value;
            _env = env;
        }

        public async Task RunAsync()
        {
            if (!_env.IsProduction())
            {
                _logger.LogWarning("DbBackupJob skipped because environment is not Production.");
                return;
            }

            var dbBackupFile = await CreateAndZipSqlBackupAsync();

            await using var stream = File.OpenRead(dbBackupFile);
            var key = $"db-backups/{Path.GetFileName(dbBackupFile)}";
            await _r2Service.UploadFileAsync(key, stream, "application/octet-stream");
        }


        private async Task<string> CreateAndZipSqlBackupAsync()
        {
            var fileName = $"{_dbOptions.DatabaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
            var backupPath = Path.Combine(_dbOptions.BackupFolder, fileName);
            var zipPath = Path.ChangeExtension(backupPath, ".zip");

            try
            {
                _logger.LogInformation("Starting backup for database {DatabaseName}...", _dbOptions.DatabaseName);

                var sql = $@"BACKUP DATABASE [{_dbOptions.DatabaseName}] TO DISK = N'{backupPath}' WITH FORMAT";

                using var conn = new SqlConnection(_dbOptions.ConnectionString);
                using var cmd = new SqlCommand(sql, conn);
                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Database backup completed: {BackupPath}", backupPath);

                // Zip the backup
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath), CompressionLevel.Optimal);
                }

                _logger.LogInformation("Backup zipped at: {ZipPath}", zipPath);

                // Optionally delete original .bak file
                // File.Delete(backupPath);

                return zipPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating or zipping the database backup.");

                // İstersen hata fırlatmayı sürdür, istersen null veya özel mesaj dönebilirsin
                throw;
            }
        }

        private async Task<string> CreateSqlBackupAsync()
        {
            Directory.CreateDirectory(_dbOptions.BackupFolder);

            var fileName = $"{_dbOptions.DatabaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
            var backupPath = Path.Combine(_dbOptions.BackupFolder, fileName);

            var sql = $@"BACKUP DATABASE [{_dbOptions.DatabaseName}] TO DISK = N'{backupPath}' WITH FORMAT";

            using var conn = new SqlConnection(_dbOptions.ConnectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return backupPath;
        }
    }

}
