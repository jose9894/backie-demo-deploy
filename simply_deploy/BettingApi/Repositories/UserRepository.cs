using BettingApi.Models;
using BettingApi.Dto;
using BettingApi.Data;
using Microsoft.EntityFrameworkCore;
namespace BettingApi.Repositories;

public interface IUserRepository : IRepository<UserAccount>
{
    Task SetDepositLimitByIdAsync(int id, int depositLimit);
    Task UpdateBalanceByIdAsync(int id, int amount);
    Task SetActiveStatusByIdAsync(int id, bool activeStatus);
    Task DeleteUserByIdAsync(int id);
    Task<IEnumerable<UserTagDto>> GetAllUserTagsAsync();
    Task<IEnumerable<UserInfoDto>> GetAllUserInfoAsync();


}


public class UserRepository : Repository<UserAccount>, IUserRepository
{
    public UserRepository(BetAppDbContext context) : base(context)
    {

    }

    public async Task UpdateBalanceByIdAsync(int id, int amount)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.Balance += amount;
            await SaveChangesAsync();
        }

    }

    public async Task SetDepositLimitByIdAsync(int id, int depositLimit)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.DepositLimit = depositLimit;
            await SaveChangesAsync();
        }
    }
    
    public async Task SetActiveStatusByIdAsync(int id, bool activeStatus)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            user.ActiveStatus = activeStatus;
            await SaveChangesAsync();
        }
    }

    public async Task DeleteUserByIdAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            Delete(user);
            await SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<UserTagDto>> GetAllUserTagsAsync()
    {
        var users = await _dbSet
       .Select(UT => new UserTagDto
       {
           UserAccountId = UT.UserAccountId,
           UserName = UT.UserName
       }).ToListAsync();

        return users;
    }

    public async Task<IEnumerable<UserInfoDto>> GetAllUserInfoAsync()
    {
        var users = await _dbSet
       .Select(UI => new UserInfoDto
       {
           UserAccountId = UI.UserAccountId,
           UserName = UI.UserName,
           Balance = UI.Balance,
           DepositLimit = UI.DepositLimit,
           ActiveStatus = UI.ActiveStatus

       }).ToListAsync();

        return users;
    }


}
