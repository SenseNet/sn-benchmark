using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SenseNet.Client;

namespace SnBenchmark
{
    internal interface IWebAccess
    {
        int AllRequests { get; }
        int ActiveRequests { get; }
        int RequestsPerSec { get; set; }

        /// <summary>
        /// Resets the logged response times (grouped by speed category) for a period.
        /// </summary>
        /// <param name="speedItems">A list of speed categories (e.g. normal, slow).</param>
        void Initialize(IEnumerable<string> speedItems);

        /// <summary>
        /// Sends an asynchronous request to the server and returns the whole response result as a string.
        /// Errors are logged and time measuring values and counters are updated.
        /// </summary>
        Task<string> RequestAsync(string actionId, ServerContext server, string speedItem, string httpMethod, string url,
            string requestBody);

        /// <summary>
        /// At the end of a period this method resets time values and returns the average
        /// values collected in that period for all speed categories.
        /// </summary>
        Dictionary<string, double> GetPeriodDataAndReset();

    }
}
