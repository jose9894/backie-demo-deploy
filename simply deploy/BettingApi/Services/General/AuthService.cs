using BettingApi.Models;
using BettingApi.Dto;
using BettingApi.Repositories;



//Til Auth
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;


namespace BettingApi.Services;

public interface IAuthService
{
    Task Register(RegisterDto dto);
    Task<TokenDto> Login(LoginDto dto);
    Task LogOut(string refreshToken);

    Task<TokenDto> RefreshJWTToken(string refreshToken);

}

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApiUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    public AuthService(IUserRepository userRepository, IConfiguration configuration, UserManager<ApiUser> userManager,
    IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
    }

    private async Task<string> getJWT(ApiUser user)
    {
        var signingCredentials = new SigningCredentials(
        new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"])), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.Name, user.UserName));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
        claims.AddRange((await _userManager.GetRolesAsync(user))
        .Select(r => new Claim(ClaimTypes.Role, r)));

        var jwtObject = new JwtSecurityToken(
        issuer: _configuration["JWT:Issuer"],
        audience: _configuration["JWT:Audience"],
        claims: claims,
        expires: DateTime.Now.AddSeconds(180),
        signingCredentials: signingCredentials);

        var jwtString = new JwtSecurityTokenHandler()
        .WriteToken(jwtObject);

        return jwtString;
    }

    public async Task Register(RegisterDto dto)
    {
        var UserAccount = new UserAccount
        {
            UserName = dto.UserName,
            ActiveStatus = true
        };

        await _userRepository.AddAsync(UserAccount);

        var newUser = new ApiUser
        {
            UserName = UserAccount.UserName,
            UserAccount = UserAccount
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);

        UserAccount.ApiUser = newUser;

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, "User");
        }
    }

    public async Task<TokenDto> Login(LoginDto dto)
    {
        var Token = "";

        var Result = new TokenDto();


        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user != null && await _userManager.CheckPasswordAsync(user, dto.Password))
        {

            var existingToken = await _refreshTokenRepository.GetRefreshTokenByUserId(user.Id);
            if (existingToken != null)
            {
                _refreshTokenRepository.Delete(existingToken);
                await _refreshTokenRepository.SaveChangesAsync();
      
            }

            Token = await getJWT(user);

            var refresh = new RefreshToken
            {
                Token = CreateRefreshToken(),
                ExpirationDate = DateTime.UtcNow.AddMinutes(10),
                ApiUserId = user.Id,

            };

            await _refreshTokenRepository.AddAsync(refresh);
            await _refreshTokenRepository.SaveChangesAsync();

            Result.JWTtoken = Token;
            Result.RefreshToken = refresh.Token;
        }
        return Result;
    }

    public async Task LogOut(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetRefreshTokenByValue(refreshToken);
        if (token != null)
        {
            _refreshTokenRepository.Delete(token);
            await _refreshTokenRepository.SaveChangesAsync();
        }
    }


    public async Task<TokenDto> RefreshJWTToken(string refreshToken)
    {
        var token = await _refreshTokenRepository.GetRefreshTokenByValue(refreshToken);

        if (token != null && token.ExpirationDate > DateTime.UtcNow)
        {
            await _refreshTokenRepository.UpdateRefreshToken(token.RefreshTokenId, DateTime.UtcNow.AddMinutes(2), CreateRefreshToken());
            var user = await _userManager.FindByIdAsync(token.ApiUserId);
            var jwtToken = await getJWT(user);
            return new TokenDto
            {
                JWTtoken = jwtToken,
                RefreshToken = token.Token
            };

        }
        return null;
    }
    
    private string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

}


