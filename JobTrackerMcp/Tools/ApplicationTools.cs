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
    }
}
