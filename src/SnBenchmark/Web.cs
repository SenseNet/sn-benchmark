using SenseNet.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SnBenchmark
{
    /// <summary>
    /// This class is responsible for sending the profile action requests, collecting
    /// response times and numbers and calculating average values for an execution period.
    /// </summary>
    internal class Web
    {
        internal static IWebAccess WebAccess { get; set; } = new BuiltInWebAccess();

        public static int AllRequests => WebAccess.AllRequests;

        public static int RequestsPerSec
        {
            get { return WebAccess.RequestsPerSec; }
            set { WebAccess.RequestsPerSec = value; }
        }

        public static int ActiveRequests => WebAccess.ActiveRequests;

        //public static double AverageResponseTimeInSec { get; private set; }

        //private static readonly object ResponseTimesSync = new object();
        //private static readonly Queue<TimeSpan> ResponseTimes = new Queue<TimeSpan>();

        //private static Dictionary<string, List<TimeSpan>> _periodResponseTimes = new Dictionary<string, List<TimeSpan>>();

        /// <summary>
        /// Resets the logged response times (grouped by speed category) for a period.
        /// </summary>
        /// <param name="speedItems">A list of speed categories (e.g. normal, slow).</param>
        public static void Initialize(IEnumerable<string> speedItems)
        {
            WebAccess.Initialize(speedItems);
        }

        /// <summary>
        /// Sends an asynchronous request to the server and returns the whole response result as a string.
        /// Errors are logged and time measuring values and counters are updated.
        /// </summary>
        public static async Task<string> RequestAsync(string actionId, ServerContext server, string speedItem, string httpMethod, string url, string requestBody)
        {
            return await WebAccess.RequestAsync(actionId, server, speedItem, httpMethod, url, requestBody);
        }

        /// <summary>
        /// Returns with an enumerable path set according to the specified content query.
        /// </summary>
        public static async Task<IEnumerable<string>> QueryPathSetAsync(string query)
        {
            return await WebAccess.QueryPathSetAsync(query);
        }

        /// <summary>
        /// At the end of a period this method resets time values and returns the average
        /// values collected in that period for all speed categories.
        /// </summary>
        public static Dictionary<string, double> GetPeriodDataAndReset()
        {
            return WebAccess.GetPeriodDataAndReset();
        }
    }
}
