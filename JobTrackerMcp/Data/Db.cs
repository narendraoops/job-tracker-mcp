using Microsoft.Data.SqlClient;

namespace JobTrackerMcp.Data
{
    public static class Db
    {
        public static SqlConnection CreateConnection()
        {
            string? connectionString = Environment.GetEnvironmentVariable("JOBTRACKER_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("JOBTRACKER_CONNECTION is not set.");
            }

            return new SqlConnection(connectionString);
        }
    }
}
