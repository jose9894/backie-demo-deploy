using BettingApi.Models;
using BettingApi.Dto;
using BettingApi.Repositories;
using System.Linq.Expressions;

namespace BettingApi.Services;

public interface IGameService
{
    Task<CoinFlipResultDto> CoinFlipGamePlay(CoinFlipRequestDto dto, int id);
    Task<CrashGameResultDto> CrashGamePlay(CrashGameRequestDto dto, int id);
}

public class GameService : IGameService
{
    IUserRepository _userRepository;
    ITransactionRepository _transactionRepository;
    public GameService(ITransactionRepository transactionRepository, IUserRepository userRepository)
    {
        _userRepository = userRepository;
        _transactionRepository = transactionRepository;
    }


    //--------------------------------For CoinGame-------------------------------//
    private string CoinResultHelper()
    {
        Random rand = new Random();
        return rand.Next(0, 2) == 0 ? "heads" : "tails";
    }

    public async Task<CoinFlipResultDto> CoinFlipGamePlay(CoinFlipRequestDto dto, int id)
    {
        using var dbOperation = await _userRepository.BeginTransactionAsync();

        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && user.ActiveStatus)
            {

                if (dto.BetAmount <= user.Balance)
                {
                    DateOnly dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

                    await _userRepository.UpdateBalanceByIdAsync(id, -dto.BetAmount);

                    var result = CoinResultHelper();

                    var resultDto = new CoinFlipResultDto
                    {
                        Result = result,
                        Payout = -dto.BetAmount
                    };


                    if (dto.Choice == result)
                    {
                        await _userRepository.UpdateBalanceByIdAsync(id, dto.BetAmount * 2);
                        resultDto.Payout += dto.BetAmount * 2;
                    }

                    var transactions = await _transactionRepository.GetTransactionByGameNameAsync(id, "Coin Flip", dateNow);

                    if (transactions.Any())
                    {
                        foreach (var transaction in transactions)
                        {
                            await _transactionRepository.UpdateGameTransactionByIdAsync(transaction.TransactionId, resultDto.Payout);
                        }

                    }
                    else
                    {
                        var Transaction = new Transaction
                        {
                            UserAccountId = id,
                            Date = dateNow,
                            Amount = resultDto.Payout,
                            GameName = "Coin Flip"
                        };

                        await _transactionRepository.AddAsync(Transaction);
                        await _transactionRepository.SaveChangesAsync();
                    }

                    await dbOperation.CommitAsync();
                    return resultDto;
                }

            }

            throw new Exception("User not found or not active from service");
        }
        catch (Exception ex)
        {
            // Rollback hvis noget fejler
            await dbOperation.RollbackAsync();
            throw new Exception($"Coin flip transaction failed: {ex.Message}");
        }
    }


    //----------------------------------------------------------------------//



    public async Task<CrashGameResultDto> CrashGamePlay(CrashGameRequestDto dto, int id)
    {
        throw new NotImplementedException("Crash game is not yet implemented");
    }

}