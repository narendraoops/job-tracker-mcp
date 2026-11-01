using Microsoft.Data.SqlClient;

namespace JobTrackerMcp.Models
{
    public class Application
    {
        // Same column order every query must use.
        public const string Columns =
            "Id, Company, Role, Status, Location, Source, AppliedOn, FollowUpOn, Notes";

        public int Id { get; set; }
        public string Company { get; set; } = "";
        public string Role { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Location { get; set; }
        public string? Source { get; set; }
        public string AppliedOn { get; set; } = "";
        public string? FollowUpOn { get; set; }
        public string? Notes { get; set; }

        public static Application FromReader(SqlDataReader reader)
        {
            Application app = new Application();
            app.Id = reader.GetInt32(0);
            app.Company = reader.GetString(1);
            app.Role = reader.GetString(2);
            app.Status = reader.GetString(3);
            app.Location = reader.IsDBNull(4) ? null : reader.GetString(4);
            app.Source = reader.IsDBNull(5) ? null : reader.GetString(5);
            app.AppliedOn = reader.GetDateTime(6).ToString("yyyy-MM-dd");
            app.FollowUpOn = reader.IsDBNull(7) ? null : reader.GetDateTime(7).ToString("yyyy-MM-dd");
            app.Notes = reader.IsDBNull(8) ? null : reader.GetString(8);
            return app;
        }
    }
}
