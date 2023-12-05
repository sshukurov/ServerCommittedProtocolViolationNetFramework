using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ServerCommittedProtocolViolationNetFramework
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            Console.Write("Enter URL to MYOB: ");
            var connectionUrl = Console.ReadLine();

            Console.WriteLine();

            Console.Write("Enter tenant name: ");
            var tenant = Console.ReadLine();

            var oDataBaseUrl = $"{(connectionUrl.EndsWith("/", StringComparison.InvariantCulture) ? connectionUrl : connectionUrl + "/")}OData/{tenant}/";

            Console.WriteLine();

            Console.Write("Enter username: ");
            var username = Console.ReadLine();

            Console.WriteLine();

            Console.Write("Enter password: ");
            var password = Console.ReadLine();

            Console.WriteLine();

            using var httpClientHandler = new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip };

            using var httpClient = new HttpClient(httpClientHandler);
            httpClient.Timeout = Timeout.InfiniteTimeSpan;

            await Task.WhenAll(
                AuthenticateAsync(httpClient, oDataBaseUrl, username, password),
                AuthenticateAsync(httpClient, oDataBaseUrl, username, password));

            bool shouldRetry;
            do
            {
                Console.Write("Re-try authentication? (Y/N): ");
                ConsoleKeyInfo yesOrNo = Console.ReadKey();
                shouldRetry = yesOrNo.KeyChar == 'Y' || yesOrNo.KeyChar == 'y';
                if (shouldRetry)
                {
                    Console.WriteLine();

                    await Task.WhenAll(
                        AuthenticateAsync(httpClient, oDataBaseUrl, username, password),
                        AuthenticateAsync(httpClient, oDataBaseUrl, username, password));
                }

                Console.WriteLine();
            } while (shouldRetry);

            Console.WriteLine();
            Console.WriteLine("Program finished!");

            Console.ReadKey();
        }

        private static async Task AuthenticateAsync(HttpClient httpClient, string oDataBaseUrl, string username, string password)
        {
            Console.WriteLine("Attempting to authenticate with MYOB...");

            try
            {
                var usernamePasswordPairRepresentation = username + ":" + password;
                var authorizationHeaderValue = $"Basic {Convert.ToBase64String(Encoding.Default.GetBytes(usernamePasswordPairRepresentation))}";

                using var request = new HttpRequestMessage(HttpMethod.Get, oDataBaseUrl);
                request.Headers.Add("Authorization", authorizationHeaderValue);

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                Console.WriteLine(response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());

                    var settings = new XmlReaderSettings { XmlResolver = null };
                    var xmlReader = XmlReader.Create(reader, settings);

                    new XmlDocument().Load(xmlReader);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
