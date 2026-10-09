using Titan.Repository;
using MyStake.Models.ApiModels;
using MyStake.Services.Exceptions;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.Backend;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Security.Authentication;
using System.Text;
using Titan.Repository.Database;

namespace MyStake.Services.Implementations
{
    public class GoldenGateXAPITokenService : IGoldenGateXAPITokenService
    {
        private readonly Func<IServiceScope> _serviceScopeFactory;
        private Lazy<GoldenGateXBearerToken> _accessToken = new Lazy<GoldenGateXBearerToken>(() => new GoldenGateXBearerToken(), LazyThreadSafetyMode.ExecutionAndPublication);
        public GoldenGateXAPITokenService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = () => serviceScopeFactory.CreateScope();
        }
        public async Task<GoldenGateXBearerToken> GetAccessToken()
        {
            if (_accessToken.Value.IsExpired)
            {
                await FetchAccessToken("gdgx");
            }

            return _accessToken.Value;
        }
        public async Task FetchAccessToken(string APIName)
        {
            var response = new GoldenGateXBearerToken();
            using var scope = _serviceScopeFactory();
            var _context = scope.ServiceProvider.GetRequiredService<WhitelabelContext>();
            var _logger = scope.ServiceProvider.GetRequiredService<Global.Logging.ILogger>();

            try
            {
                var api = await _context.IntegratedApis.Where(x => x.ApiCode == APIName.ToLower()).FirstOrDefaultAsync();

                if (api == null)
                {
                    throw new IntegrationAPINotFoundException(APIName);
                }

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    string url = api.Endpoint + "/auth/createtoken";

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(url),
                        Headers = {
                            { HeaderNames.Accept, "application/json"  }
                        }
                    };
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        clientId = api.ClientId,
                        clientSecret = api.ClientSecret,
                    }), Encoding.UTF8, "application/json");

                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var s = await httpResponseMessage.Content.ReadAsStringAsync();
                        var tokenResponse = JsonConvert.DeserializeObject<GoldenGateXTokenGenerationResponse>(s);
                        response = new GoldenGateXBearerToken
                        {
                            Token = tokenResponse.Token,
                            Expiration = tokenResponse.Expiration
                        };

                        _accessToken = new Lazy<GoldenGateXBearerToken>(() => response, LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                    else
                    {
                        _logger.Error($"FetchAccessToken IsSuccessStatusCode: {httpResponseMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"FetchAccessToken: {ex}");
            }
        }
    }
}
