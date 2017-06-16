using System.Data;

namespace Database
{
    public class Parameter
    {
        public string name { get; set; }
        public string value { get; set; }
        public DataTable table { get; set; }
        public SqlDbType type { get; set; }
    }
}
