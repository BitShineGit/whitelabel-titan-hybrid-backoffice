using Whitelabel_backoffice.Models.ApiModels;

namespace Whitelabel_backoffice.Services.Interfaces
{
    public interface IBoltAPITokenService
    {
        Task<GoldenGateXBearerToken> GetAccessToken();
    }
}
