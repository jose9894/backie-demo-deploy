
using BettingApi.Repositories;
using BettingApi.Models;

using BettingApi.Dto;

namespace BettingApi.Services;

public interface IDepositService
{
    Task<IEnumerable<DepositResultDto>> GetAllDepositByUserIdAsync(int id);

    Task<IEnumerable<DepositResultDto>> GetAllDepositAsync();

    Task AddDepositAsync(int amount, int id);
}

public class DepositService : IDepositService
{
    private readonly IDepositRepository _depositrepository;
    private readonly IUserRepository _userRepository;
    public DepositService(IDepositRepository depositrepository, IUserRepository userRepository)
    {
        _depositrepository = depositrepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<DepositResultDto>> GetAllDepositByUserIdAsync(int id)
    {
        var deposits = await _depositrepository.GetAllDepositByUserIdAsync(id); 

        return deposits;
    }
    
    public async Task AddDepositAsync(int amount, int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            if (user.DepositLimit >= amount || user.DepositLimit == null)
            {
                var deposit = new Deposit
                {
                    Amount = amount,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    UserAccountId = id
                };
                await _depositrepository.AddAsync(deposit);
                user.Balance += amount;
                await _depositrepository.SaveChangesAsync();
            }
            else
            {
             throw new Exception("Amount exceeds depositLimit");
            }
        }
    }

    public async Task<IEnumerable<DepositResultDto>> GetAllDepositAsync()
    {
        var deposits = await _depositrepository.GetAllDepositForAllUsersAsync();
        return deposits; 
    }


}