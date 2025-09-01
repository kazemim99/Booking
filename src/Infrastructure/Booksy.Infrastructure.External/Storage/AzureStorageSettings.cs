namespace Booksy.Infrastructure.External.Storage
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DefaultContainerName { get; set; } = "booksy-files";
    }
}