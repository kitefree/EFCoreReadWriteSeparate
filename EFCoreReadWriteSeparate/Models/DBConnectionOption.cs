namespace EFCoreReadWriteSeparate.Models
{
    public class DBConnectionOption
    {
        public string WriteConnection { get; set; }

        public List<string> ReadConnectionList { get; set; }

    }
}
