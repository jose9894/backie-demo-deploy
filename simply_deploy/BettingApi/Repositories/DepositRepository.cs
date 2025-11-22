using BettingApi.Models;
using Microsoft.EntityFrameworkCore;
using BettingApi.Data;
using BettingApi.Dto;

namespace BettingApi.Repositories;
public interface IDepositRepository:IRepository<Deposit>{
Task<IEnumerable<DepositResultDto>> GetAllDepositByUserIdAsync(int id);
Task<IEnumerable<DepositResultDto>> GetAllDepositForAllUsersAsync();
}


public class DepositRepository: Repository<Deposit>, IDepositRepository
{
    public DepositRepository(BetAppDbContext context) : base(context)
    {

    }
    
    public async Task<IEnumerable<DepositResultDto>> GetAllDepositByUserIdAsync(int id)
    {
        var deposits = await _dbSet.Where(d => d.UserAccountId == id)
        .Select(Dt => new DepositResultDto
        {
            DepositId = Dt.DepositId,
            Date = Dt.Date,
            Amount = Dt.Amount
            
        }).ToListAsync();

        return deposits;
    }

    public async Task<IEnumerable<DepositResultDto>> GetAllDepositForAllUsersAsync()
    {
        var deposits = await _dbSet
        .Select(Dt => new DepositResultDto
        {
            DepositId = Dt.DepositId,
            Date = Dt.Date,
            Amount = Dt.Amount
            
        }).ToListAsync();

        return deposits;
    }
    
}
