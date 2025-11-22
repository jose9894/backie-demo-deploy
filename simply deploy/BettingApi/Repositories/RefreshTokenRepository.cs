using BettingApi.Models;
using Microsoft.EntityFrameworkCore;
using BettingApi.Data;
using BettingApi.Dto;

namespace BettingApi.Repositories;
public interface IRefreshTokenRepository:IRepository<RefreshToken>{

    Task<RefreshToken> GetRefreshTokenByValue(string token);
    Task UpdateRefreshToken(int tokenId, DateTime time, string newVal);

    Task<RefreshToken> GetRefreshTokenByUserId(string userId);
    

}


public class RefreshTokenRepository: Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(BetAppDbContext context) : base(context)
    {

    }
    
    public async Task<RefreshToken> GetRefreshTokenByValue(string token)
    {
        var Refresh = await _dbSet.FirstOrDefaultAsync(t => t.Token == token);

        return Refresh;
    }

    public async Task UpdateRefreshToken(int tokenId, DateTime time, string newVal)
    {
        var token = await GetByIdAsync(tokenId);
        if (token != null)
        {
            token.Token = newVal;
            token.ExpirationDate = time;
            await SaveChangesAsync();
        }
    }

    public async Task<RefreshToken> GetRefreshTokenByUserId(string userId)
    {
         var Refresh = await _dbSet.FirstOrDefaultAsync(t => t.ApiUserId == userId);

        return Refresh;
    }
    
    
}
