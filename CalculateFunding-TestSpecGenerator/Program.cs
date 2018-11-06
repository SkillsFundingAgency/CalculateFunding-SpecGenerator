using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CalculateFunding.Frontend.Clients;
using CalculateFunding.Frontend.Clients.CalcsClient;
using CalculateFunding.Frontend.Clients.CommonModels;
using CalculateFunding.Frontend.Clients.DatasetsClient;
using CalculateFunding.Frontend.Clients.SpecsClient;
using CalculateFunding.Frontend.Helpers;
using CalculateFunding.Frontend.Interfaces.ApiClient;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CalculateFunding_TestSpecGenerator
{
    class Program
    {
        private const string SfaCorellationId = "sfa-correlationId";
        private const string SfaUsernameProperty = "sfa-username";
        private const string SfaUserIdProperty = "sfa-userid";

        private const string OcpApimSubscriptionKey = "Ocp-Apim-Subscription-Key";

        static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder
                .Build();

            SpecGeneratorConfiguration config = new SpecGeneratorConfiguration()
            {
                SpecificationName = "SpecGenerator " + Guid.NewGuid().ToString().Substring(0, 8),
                NumberOfCalculations = 2,
                NumberOfPolices = 1,
                DatasetDefinitionId = "1221999",
                DatasetFilePath = @"C:\Users\danie\Desktop\PE and Sports premium - Dan 3.xlsx",
            };

            ILogger logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            using (StaticHttpClientFactory httpClientFactory = new StaticHttpClientFactory())
            {
                ISpecsApiClient specsApiClient = GenerateSpecsClient(logger, httpClientFactory);
                IDatasetsApiClient datasetsApiClient = GenerateDatasetsClient(logger, httpClientFactory);
                ICalculationsApiClient calculationsApiClient = GenerateCalculationsApiClient(logger, httpClientFactory);

                SpecGenerator generator = new SpecGenerator(specsApiClient, datasetsApiClient, calculationsApiClient, logger);

                try
                {
                    await generator.Generate(config);

                    logger.Information("Finished generating specification");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception thrown");
                }
            }
        }

        private static ISpecsApiClient GenerateSpecsClient(ILogger logger, StaticHttpClientFactory httpClientFactory)
        {
            Func<HttpClient> httpClientFunc = new Func<HttpClient>(() =>
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(0, 10, 0);

                SetDefaultApiOptions(httpClient, new ApiOptions()
                {
                    ApiEndpoint = "https://localhost:7001/api/specs",
                    ApiKey = "Local",
                });

                return httpClient;
            });

            httpClientFactory.AddClient(HttpClientKeys.Specifications, httpClientFunc);

            return new SpecsApiClient(httpClientFactory, logger, null);
        }

        private static IDatasetsApiClient GenerateDatasetsClient(ILogger logger, StaticHttpClientFactory httpClientFactory)
        {
            Func<HttpClient> httpClientFunc = new Func<HttpClient>(() =>
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(0, 10, 0);

                SetDefaultApiOptions(httpClient, new ApiOptions()
                {
                    ApiEndpoint = "https://localhost:7004/api/datasets",
                    ApiKey = "Local",
                });

                return httpClient;
            });

            httpClientFactory.AddClient(HttpClientKeys.Datasets, httpClientFunc);

            return new DatasetsApiClient(httpClientFactory, logger);
        }

        private static ICalculationsApiClient GenerateCalculationsApiClient(ILogger logger, StaticHttpClientFactory httpClientFactory)
        {
            Func<HttpClient> httpClientFunc = new Func<HttpClient>(() =>
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(0, 10, 0);

                SetDefaultApiOptions(httpClient, new ApiOptions()
                {
                    ApiEndpoint = "https://localhost:7004/api/datasets",
                    ApiKey = "Local",
                });

                return httpClient;
            });

            httpClientFactory.AddClient(HttpClientKeys.Calculations, httpClientFunc);

            return new CalculationsApiClient(httpClientFactory, logger);
        }

        private static void SetDefaultApiOptions(HttpClient httpClient, ApiOptions options)
        {
            Guard.ArgumentNotNull(httpClient, nameof(httpClient));
            Guard.ArgumentNotNull(options, nameof(options));

            if (string.IsNullOrWhiteSpace(options.ApiEndpoint))
            {
                throw new InvalidOperationException("options EndPoint is null or empty string");
            }

            string baseAddress = options.ApiEndpoint;
            if (!baseAddress.EndsWith("/", StringComparison.CurrentCulture))
            {
                baseAddress = $"{baseAddress}/";
            }

            UserProfile userProfile = new UserProfile()
            {
                Id = "specgenerator",
                Firstname = "Spec",
                Lastname = "Generator",
                Username = "specgenerator",
            };

            httpClient.BaseAddress = new Uri(baseAddress, UriKind.Absolute);
            httpClient.DefaultRequestHeaders?.Add(OcpApimSubscriptionKey, options.ApiKey);
            httpClient.DefaultRequestHeaders?.Add(SfaUsernameProperty, userProfile.Fullname);
            httpClient.DefaultRequestHeaders?.Add(SfaUserIdProperty, userProfile.Id);

            httpClient.DefaultRequestHeaders?.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders?.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        }
    }
}
