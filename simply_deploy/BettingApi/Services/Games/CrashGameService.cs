using BettingApi.Dto;
using BettingApi.Models;
using BettingApi.Repositories;

namespace BettingApi.Services;

public interface ICrashRng
{
    double Generate();
}

public class CrashRng : ICrashRng
{
    private readonly Random _rng = new();

    public double Generate()
    {
        double bias = 1.0;
        double r = _rng.NextDouble();
        double val = -Math.Log(1 - r) / bias;
        double rounded = Math.Round(val * 10) / 10;
        return Math.Max(1.03, rounded);
    }
}

public interface ICrashGameService
{
    Task<CrashGameResultDto> CrashGamePlay(CrashGameRequestDto dto, int userAccountId);
}

public class CrashGameService : ICrashGameService
{
    private readonly ICrashRng _rng;
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;

    public CrashGameService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        ICrashRng rng)
    {
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _rng = rng;
    }

    public async Task<CrashGameResultDto> CrashGamePlay(CrashGameRequestDto dto, int userAccountId)
    {
        using var dbOperation = await _userRepository.BeginTransactionAsync();

        try
        {
            var user = await _userRepository.GetByIdAsync(userAccountId);
            if (user == null || !user.ActiveStatus)
                throw new Exception("User not active");

            if (dto.BetAmount <= 0 || dto.BetAmount > user.Balance)
                throw new Exception("Insufficient balance");

            DateOnly dateNow = DateOnly.FromDateTime(DateTime.UtcNow);

            await _userRepository.UpdateBalanceByIdAsync(userAccountId, -dto.BetAmount);

            double crashPoint = _rng.Generate();
            bool isWin = dto.CashoutMultiplier < crashPoint;
            int payout = -dto.BetAmount;

            if (isWin)
            {
                int winnings = (int)(dto.BetAmount * dto.CashoutMultiplier);
                payout += winnings;
                await _userRepository.UpdateBalanceByIdAsync(userAccountId, winnings);
            }

            var transaction = new Transaction
            {
                UserAccountId = userAccountId,
                Date = dateNow,
                Amount = payout,
                GameName = "Crash"
            };

            await _transactionRepository.AddAsync(transaction);
            await _transactionRepository.SaveChangesAsync();

            await dbOperation.CommitAsync();

            return new CrashGameResultDto
            {
                CrashPoint = crashPoint,
                IsWin = isWin,
                Payout = payout
            };
        }
        catch (Exception ex)
        {
            await dbOperation.RollbackAsync();
            throw new Exception($"Crash game transaction failed: {ex.Message}");
        }
    }
}