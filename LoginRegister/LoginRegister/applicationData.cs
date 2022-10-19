using MySql.Data.Types;

namespace ApiEndPoints
{
    public class applicationData
    {
        public MySqlDateTime? date_created { get; set; }
        public string? name { get; set; }
        public string? userId { get; set; }
        public string? applicationId { get; set; }
        public string? createdBy { get; set; }
        public string? category { get; set; }
        public bool? reward { get; set; }
        public float? stake { get; set; }
        public string? finished { get; set; }
        public string? description { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public string? localizationName { get; set; }

    }
}