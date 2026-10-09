using Whitelabel_backoffice.Constants.Enums;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Services.Exceptions;
using Whitelabel_backoffice.Services.Interfaces;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Security.Authentication;
using System.Text;

namespace Whitelabel_backoffice.Services.Implementations
{
    public class GameService : IGameService
    {
        private readonly Global.Logging.ILogger _logger = null;
        private readonly IGoldenGateXAPITokenService _goldenGateXAPITokenService = null;
        private readonly IWhitelabelDBStorage _dbStorage = null;

        public GameService(Global.Logging.ILogger logger, IWhitelabelDBStorage dbStorage, IGoldenGateXAPITokenService goldenGateXAPITokenService)
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _goldenGateXAPITokenService = goldenGateXAPITokenService;
        }

        public async Task<GetGameDetailResponse> GetGameDetail(GetGameDetailRequest request)
        {
            var response = new GetGameDetailResponse();
            var api = await _dbStorage.Context.IntegratedApis.Where(x => x.ApiCode == request.ApiCode.ToLower()).FirstOrDefaultAsync();

            if (api == null)
            {
                throw new IntegrationAPINotFoundException(request.ApiCode);
            }
            try
            {
                var token = await _goldenGateXAPITokenService.GetAccessToken();

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
                    string url = api.Endpoint + "/game/detail";

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(url),
                        Headers = {
                        { HeaderNames.Accept, "application/json"  }
                    },
                    };
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        vendorCode = request.VendorCode,
                        gameCode = request.GameCode
                    }), Encoding.UTF8, "application/json");

                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var s = await httpResponseMessage.Content.ReadAsStringAsync();
                        response = JsonConvert.DeserializeObject<GetGameDetailResponse>(s);
                    }
                    else
                    {
                        _logger.Error($"GetGameDetail IsSuccessStatusCode: {httpResponseMessage}");
                        response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"GetGameDetail: {e}");
                response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
            }
            return response;
        }

        public async Task<GetGamesResponse> GetGameList(GetGameListRequest request)
        {
            var response = new GetGamesResponse();
            var api = await _dbStorage.Context.IntegratedApis.Where(x => x.ApiCode == request.ApiCode.ToLower()).FirstOrDefaultAsync();

            if (api == null)
            {
                throw new IntegrationAPINotFoundException(request.ApiCode);
            }

            try
            {
                var token = await _goldenGateXAPITokenService.GetAccessToken();

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
                    string url = api.Endpoint + "/games/list";

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(url),
                        Headers = {
                        { HeaderNames.Accept, "application/json"  }
                    },
                    };
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        vendorCode = request.VendorCode,
                        language = request.Language
                    }), Encoding.UTF8, "application/json");

                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var s = await httpResponseMessage.Content.ReadAsStringAsync();
                        response = JsonConvert.DeserializeObject<GetGamesResponse>(s);
                        //response.Message.ForEach(x => x.Thumbnail = $"https://common-static.ppgames.net/game_pic/square/200/{x.GameCode}.png");
                        response.Message = response.Message.OrderByDescending(x => x.UpdatedAt).ToList();
                    }
                    else
                    {
                        _logger.Error($"FetchAccessToken IsSuccessStatusCode: {httpResponseMessage}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"PragmaticService GetGameList: {e}");
            }

            return response;
        }

        public async Task<GetGameLaunchUrlResponse> GetLaunchURL(GetLaunchUrlRequest request)
        {
            var response = new GetGameLaunchUrlResponse();
            var api = await _dbStorage.Context.IntegratedApis.Where(x => x.ApiCode == request.ApiCode.ToLower()).FirstOrDefaultAsync();
            if (api == null)
            {
                throw new IntegrationAPINotFoundException(request.ApiCode);
            }

            try
            {
                var token = await _goldenGateXAPITokenService.GetAccessToken();

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
                    string url = api.Endpoint + "/game/launch-url";

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(url),
                        Headers = {
                        { HeaderNames.Accept, "application/json"  }
                    },
                    };
                    httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        vendorCode = request.VendorCode,
                        gameCode = request.GameCode,
                        userCode = request.UserCode,
                        language = request.Language,
                        currency = request.Currency
                    }), Encoding.UTF8, "application/json");

                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var s = await httpResponseMessage.Content.ReadAsStringAsync();
                        response = JsonConvert.DeserializeObject<GetGameLaunchUrlResponse>(s);
                    }
                    else
                    {
                        _logger.Error($"GetLaunchURL IsSuccessStatusCode: {httpResponseMessage}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"GetLaunchURL: {e}");
            }
            return response;
        }
        public async Task<GetGamesResponse> GetMiniGameList(GetGameListRequest request)
        {
            var response = new GetGamesResponse();
            var api = await _dbStorage.Context.IntegratedApis.Where(x => x.ApiCode == request.ApiCode.ToLower()).FirstOrDefaultAsync();

            if (api == null)
            {
                throw new IntegrationAPINotFoundException(request.ApiCode);
            }

            try
            {
                var token = await _goldenGateXAPITokenService.GetAccessToken();

                HttpClientHandler clientHandler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };
                using (var httpClient = new HttpClient(clientHandler))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
                    string url = api.Endpoint + "/games/mini/list";

                    var httpRequestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(url),
                        Headers = {
                            { HeaderNames.Accept, "application/json"  }
                        }
                    };

                    var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        var s = await httpResponseMessage.Content.ReadAsStringAsync();
                        response = JsonConvert.DeserializeObject<GetGamesResponse>(s);
                        response.Message = response.Message.OrderByDescending(x => x.UpdatedAt).ToList();
                    }
                    else
                    {
                        _logger.Error($"GetMiniGameList IsSuccessStatusCode: {httpResponseMessage}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"GetMiniGameList GetGameList: {e}");
            }

            return response;
        }
    }
}
