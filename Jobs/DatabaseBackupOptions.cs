namespace BussinessCupApi.Jobs
{
    public class DatabaseBackupOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string BackupFolder { get; set; }
    }
}
