using System.ComponentModel;
using JobTrackerMcp.Data;
using JobTrackerMcp.Models;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;

namespace JobTrackerMcp.Tools
{
    [McpServerToolType]
    public static class ApplicationTools
    {
        private const int MaxRows = 50;

        private static readonly string[] ValidStatuses =
            { "Applied", "Screening", "Interview", "Offer", "Rejected", "Withdrawn" };

        [McpServerTool]
        [Description("Gets one job application by its numeric Id. Returns nothing if the Id does not exist.")]
        public static async Task<Application?> GetApplication(
            [Description("The numeric Id of the application.")] int id)
        {
            string sql = "SELECT " + Application.Columns + " FROM dbo.Applications WHERE Id = @id";

            using (SqlConnection connection = Db.CreateConnection())
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            return null;
                        }

                        return Application.FromReader(reader);
                    }
                }
            }
        }

        [McpServerTool]
        [Description("Lists job applications, newest first. Optionally filter by status. Returns at most 50 rows.")]
        public static async Task<List<Application>> ListApplications(
            [Description("Optional status filter: Applied, Screening, Interview, Offer, Rejected or Withdrawn. Leave empty for all.")] string? status = null,
            [Description("Maximum rows to return, from 1 to 50. Default 50.")] int limit = 50)
        {
            string? cleanStatus = null;

            if (!string.IsNullOrWhiteSpace(status))
            {
                cleanStatus = ValidStatuses.FirstOrDefault(
                    s => string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));

                if (cleanStatus == null)
                {
                    throw new ArgumentException(
                        "Unknown status '" + status + "'. Valid values: " + string.Join(", ", ValidStatuses));
                }
            }

            if (limit < 1) limit = 1;
            if (limit > MaxRows) limit = MaxRows;

            string sql =
                "SELECT TOP (@limit) " + Application.Columns +
                " FROM dbo.Applications" +
                " WHERE (@status IS NULL OR Status = @status)" +
                " ORDER BY AppliedOn DESC, Id DESC";

            using (SqlConnection connection = Db.CreateConnection())
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@limit", limit);
                    command.Parameters.AddWithValue("@status", (object?)cleanStatus ?? DBNull.Value);

                    return await ReadApplications(command);
                }
            }
        }

        [McpServerTool]
        [Description("Lists open applications whose follow-up date is today or already passed, oldest follow-up first. Rejected and Withdrawn applications are left out. Returns at most 50 rows.")]
        public static async Task<List<Application>> GetFollowupsDue()
        {
            string sql =
                "SELECT TOP (@limit) " + Application.Columns +
                " FROM dbo.Applications" +
                " WHERE FollowUpOn IS NOT NULL" +
                " AND FollowUpOn <= CAST(GETDATE() AS DATE)" +
                " AND Status NOT IN ('Rejected', 'Withdrawn')" +
                " ORDER BY FollowUpOn ASC, Id ASC";

            using (SqlConnection connection = Db.CreateConnection())
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@limit", MaxRows);

                    return await ReadApplications(command);
                }
            }
        }

        private static async Task<List<Application>> ReadApplications(SqlCommand command)
        {
            List<Application> results = new List<Application>();

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(Application.FromReader(reader));
                }
            }

            return results;
        }
    }
}
