using MyStake.Models.ApiModels;

namespace MyStake.Services.Interfaces
{
    public interface IGoldenGateXAPITokenService
    {
        Task<GoldenGateXBearerToken> GetAccessToken();
    }
}
