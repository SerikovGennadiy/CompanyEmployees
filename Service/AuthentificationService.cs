using AutoMapper;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Exceptions;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Contracts;
using Shared.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Service
{
    // сервис похож на остальные только имеет 
    // return type IdentityResult и UserManager
    internal sealed class AuthentificationService : IAuthenticationService
    {
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;
        // build-in MS.AspNetCore.Identity.UserManager class
        // про как обеспечить API управление user'ами в постоянном хранилищем
        // а не про конкретно как хранить (у нас EF Core)
        private readonly UserManager<User> _userManager;

        //configuration binding
        //private readonly IConfiguration _configuration;
        private readonly JwtConfiguration _jwtConfiguration;

        // configuration options pattern
        // private readonly IOptions<JwtConfiguration> _configuration;
        private readonly IOptionsSnapshot<JwtConfiguration> _configuration;
        
        private User? _user;

        public AuthentificationService(ILoggerManager logger, IMapper mapper, UserManager<User> userManager, /*IConfiguration*/ /*IOptions<JwtConfiguration>*/ IOptionsSnapshot<JwtConfiguration> configuration)
        {
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _configuration = configuration;
            
            // configurtion binding
                //_jwtConfiguration = new JwtConfiguration();
                //_configuration.Bind(_jwtConfiguration.Section, _jwtConfiguration);
            
            // configuration options pattern
                _jwtConfiguration = configuration.Value; // ...Value (DI) extract registered JwtConfiguration.cs (см. SE.ConfigureJwtConfiguration()...)
        }

        public async Task<IdentityResult> RegisterUser(UserForRegistractionDTO userForRegiatrationDto)
        {
            var user = _mapper.Map<User>(userForRegiatrationDto);

            var result = await _userManager.CreateAsync(user, userForRegiatrationDto.Password);

            // If we want before calling AddToRoleAsync or AddToRolesAync check 
            // if roles exists in db. We must to add RoleMaanger<TRole> and use RoleExistsAsync()..

            if (result.Succeeded)
                await _userManager.AddToRolesAsync(user, userForRegiatrationDto.Roles);

            return result;
        }

        public async Task<bool> ValidateUser(UserForAuthenticationDTO userForAuth)
        {
            // fetch user from db 
            _user = await _userManager.FindByNameAsync(userForAuth.UserName);
            // check user exist and verify user password against the hashed password from the database
            var result = (_user != null && await _userManager.CheckPasswordAsync(_user, userForAuth.Password));
            // log if user validation failed
            if (!result)
                _logger.LogWarn($"{nameof(ValidateUser)}: Authentication failed. Wrong user name or password");

            return result;
        }

        /*
            collect information from private methods and serialized this token options 
         */
        // after added refresh token feature
        /*public async Task<string> CreateToken()
        {
            // get our secret key (env) as byte array with security algorithm
            var signinigCredentials = GetSignInCredentials();
            // get list of claims related with user and roles belongs to
            var claims = await GetClaims();
            // create JwtSecurityToken object with all options
            var tokenOptions = GenerateTokenOptions(signinigCredentials, claims);

            return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        }*/

        public async Task<TokenDTO> CreateToken(bool populateExp)
        {
            var signingCreadentials = GetSignInCredentials();
            var claims = await GetClaimsAsync();
            var tokenOptions = GenerateTokenOptions(signingCreadentials, claims);

            var refreshToken = GenerateRefreshToken();

            _user.RefreshToken = refreshToken;
            if (populateExp)
            {
                _user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            }

            await _userManager.UpdateAsync(_user);
            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return new TokenDTO(accessToken, refreshToken);
        }

        private SigningCredentials GetSignInCredentials()
        {
            var key = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET"));
            var secret = new SymmetricSecurityKey(key);

            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaimsAsync()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, _user.UserName)
            };

            var roles = await _userManager.GetRolesAsync(_user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }

        private SecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            //var jwtSettings = _configuration.GetSection("jwtSettings");

            var tokenOptions = new JwtSecurityToken
                                        (
                                            //issuer: jwtSettings["validIssuer"],
                                            //audience: jwtSettings["validAudience"],
                                            issuer: _jwtConfiguration.ValidIssuer,
                                            audience: _jwtConfiguration.ValidAudience,
                                            claims: claims,
                                            //expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["expires"])),
                                            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtConfiguration.Expires)),
                                            signingCredentials: signingCredentials
                                        );

            return tokenOptions;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            // to generate cryptographic random number
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // ClaimsPrincipal по сути HttpContext.User : IPrincipal. Расширяет стандартные признаки пользователя (login role)
        // Содержит много признаков (claims (англ. утверждений), присущих user'у, по которым авторизация более гибкая
        /*
            Пример List<Claims> user'a:
            возраст, город, страна проживания, любимая музыкальная группа (отдельные объекты claim)
            Claim.Issuer  : string        - система издатель (ее назв-е) выдавшая claim
            Claim.Subject : ClaimIdentity - инф о пользователе
            Claim.Type    : string        - тип объекта claim
            Claim.Value   : string        - значение объекта claim
         */
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            //var jwtSettings = _configuration.GetSection("JwtSettings");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                                            Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET"))),
                ValidateLifetime = true,
                //ValidIssuer = jwtSettings["validIssuer"],
                //ValidAudience = jwtSettings["validAudience"]
                ValidIssuer = _jwtConfiguration.ValidIssuer,
                ValidAudience = _jwtConfiguration.ValidAudience
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            // Extract ClaimsPrincipal object from received token
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            // Check token validation result
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256
                                                      , StringComparison.InvariantCultureIgnoreCase)) {
                throw new SecurityTokenException("Invalid token");
            }

            // Return User (principal) info
            return principal;
        }

        public async Task<TokenDTO> RefreshToken(TokenDTO tokenDTO)
        {
            // extract principal from exprired token
            var principal = GetPrincipalFromExpiredToken(tokenDTO.AccessToken);
            
            // get user from db
            var user = await _userManager.FindByNameAsync(principal.Identity.Name);
            
            // check user exists, equal DB-refreshtoken and received.refreshtoken
            // if false stop flow, return BadRequest to user
            if (user == null || user.RefreshToken != tokenDTO.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.Now)
                    throw new RefreshTokenBadRequest();
            
            // update _user
            _user = user;

            return await CreateToken(populateExp: false);
        }
    }
}
