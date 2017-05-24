using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SNBConfigChecker
{
    class SnDatabase
    {
        public static int GetLastIndexingActivityId(string connectionString)
        {
            var sql = "SELECT TOP 1 IndexingActivityId FROM IndexingActivity ORDER BY IndexingActivityId DESC";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(sql, connection) {CommandType = CommandType.Text};
                var queryResult = command.ExecuteScalar();
                return Convert.ToInt32(queryResult);
            }
        }
    }
}
