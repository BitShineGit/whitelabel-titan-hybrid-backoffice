using Whitelabel_backoffice.Models.ApiModels;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;

namespace Whitelabel_backoffice.Services.Interfaces
{
    public interface IBoltGameService
    {
        Task<GetGamesResponse> GetGameList(GetGameListRequest request);
        Task<GetGamesResponse> GetMiniGameList(GetGameListRequest request);
        Task<GetGameLaunchUrlResponse> GetLaunchURL(GetLaunchUrlRequest request);
        Task<GetGameDetailResponse> GetGameDetail(GetGameDetailRequest request);
        Task<GetProviderListResponseModel> GetProviderList(GetProviderListRequestModel request);
    }
}
