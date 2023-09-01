using Microsoft.AspNetCore.Identity;
using Shared.DTO;

namespace Service.Contracts
{
    public interface IAuthenticationService
    {
        Task<IdentityResult> RegisterUser(UserForRegistractionDTO userForRegiatrationDto);
        Task<bool> ValidateUser(UserForAuthenticationDTO userForAuthenticationDTO);

        // Task<string> CreateToken();
        // due to refresh token feature
        Task<TokenDTO> CreateToken(bool populateExp);
        Task<TokenDTO> RefreshToken(TokenDTO tokenDTO);
    }
}
