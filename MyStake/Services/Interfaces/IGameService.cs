using MyStake.Services.Messaging.IntegratedApi;

namespace MyStake.Services.Interfaces
{
    public interface IGameService
    {
        Task<GetGamesResponse> GetGameList(GetGameListRequest request);
        Task<GetGamesResponse> GetMiniGameList(GetGameListRequest request);
        Task<GetGameLaunchUrlResponse> GetLaunchURL(GetLaunchUrlRequest request);
        Task<GetGameDetailResponse> GetGameDetail(GetGameDetailRequest request);
    }
}
