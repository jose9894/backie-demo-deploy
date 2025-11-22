using BettingApi.Models;
using Microsoft.EntityFrameworkCore;
using BettingApi.Data;
using BettingApi.Dto;

namespace BettingApi.Repositories;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<TransactionDto>> GetAllTransactionByUserIdAsync(int id);
    Task<IEnumerable<Transaction>> GetTransactionByGameNameAsync(int id, string GameName, DateOnly date);
    Task UpdateGameTransactionByIdAsync(int id, int amount);
    Task<IEnumerable<TransactionDto>> GetAllTransactionForAllUserAsync();

}

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(BetAppDbContext context) : base(context)
    {

    }

    public async Task<IEnumerable<TransactionDto>> GetAllTransactionByUserIdAsync(int id)
    {
        var transactions = await _dbSet.Where(t => t.UserAccountId == id)
       .Select(TI => new TransactionDto
       {
           TransactionId = TI.TransactionId,
           Date = TI.Date,
           Amount = TI.Amount,
           GameName = TI.GameName

       }).ToListAsync();

        return transactions;
    }

    public async Task<IEnumerable<Transaction>> GetTransactionByGameNameAsync(int id, string GameName, DateOnly date)
    {
        var transactions = await _dbSet.Where(t => t.GameName == GameName && t.UserAccountId == id && t.Date == date).ToListAsync();
        return transactions;
    }

    public async Task UpdateGameTransactionByIdAsync(int id, int amount)
    {
        var transaction = await GetByIdAsync(id);
        if (transaction != null)
        {
            transaction.Amount += amount;
            await SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<TransactionDto>> GetAllTransactionForAllUserAsync()
    {
        var transactions = await _dbSet
       .Select(TI => new TransactionDto
       {
           TransactionId = TI.TransactionId,
           Date = TI.Date,
           Amount = TI.Amount,
           GameName = TI.GameName

       }).ToListAsync();

        return transactions;
    }


}
