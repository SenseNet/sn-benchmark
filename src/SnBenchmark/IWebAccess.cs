using System.Collections.Generic;
using System.IO;
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
        /// Uploads a stream nchronous request to the server and returns with the Content.
        /// Errors are logged and time measuring values and counters are updated.
        /// </summary>
        Task<Content> UploadAsync(string actionId, ServerContext server, string speedItem, string targetContainerPath,
            string fileName, Stream stream);

        /// <summary>
        /// Returns with an enumerable path set according to the specified content query.
        /// </summary>
        Task<IEnumerable<string>> QueryPathSetAsync(string query);

        /// <summary>
        /// At the end of a period this method resets time values and returns the average
        /// values collected in that period for all speed categories.
        /// </summary>
        Dictionary<string, double> GetAverageResponseStringAndReset();

        string[] GetRequestLog();
    }
}
