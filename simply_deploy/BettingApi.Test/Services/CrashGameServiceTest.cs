using Xunit;
using NSubstitute;
using BettingApi.Services;
using BettingApi.Repositories;
using BettingApi.Models;
using BettingApi.Dto;
using System;
using System.Threading.Tasks;

public class CrashGameServiceTests
{
    private readonly CrashGameService _uut;
    private readonly IUserRepository _userRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICrashRng _rng;

    public CrashGameServiceTests()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _rng = Substitute.For<ICrashRng>();
        _uut = new CrashGameService(_transactionRepo, _userRepo, _rng);
    }

    // ---------- Exception / fejl scenarier ----------
    [Fact]
    public async Task CrashGamePlay_ShouldThrow_WhenUserNotFound()
    {
        _userRepo.GetByIdAsync(1).Returns((UserAccount)null);
        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 1.5 };

        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CrashGamePlay(request, 1));
        Assert.Equal("Crash game transaction failed: User not active", ex.Message);
    }

    [Fact]
    public async Task CrashGamePlay_ShouldThrow_WhenUserInactive()
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = false
        });

        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 1.5 };

        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CrashGamePlay(request, 1));
        Assert.Equal("Crash game transaction failed: User not active", ex.Message);
    }

    // ---------- Input boundary tests ----------
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task CrashGamePlay_BetAmountBoundaryTests(int amount)
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 10,
            ActiveStatus = true
        });

        var request = new CrashGameRequestDto { BetAmount = amount, CashoutMultiplier = 1.5 };

        if (amount <= 0 || amount > 10)
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CrashGamePlay(request, 1));
            Assert.Contains("Insufficient balance", ex.Message);
        }
        else
        {
            _rng.Generate().Returns(2.0);
            var result = await _uut.CrashGamePlay(request, 1);
            Assert.NotNull(result);
        }
    }

    // ---------- Resultat / payout scenarier ----------
    [Fact]
    public async Task CrashGamePlay_ShouldReturnWin_WhenMultiplierIsLessThanCrashPoint()
    {
        var user = new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        };
        _userRepo.GetByIdAsync(1).Returns(user);

        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 1.5 };
        _rng.Generate().Returns(2.0);

        var result = await _uut.CrashGamePlay(request, 1);

        Assert.True(result.IsWin);
        Assert.Equal(50, result.Payout); // winnings = 150, payout = -100 + 150 = 50
        await _userRepo.Received().UpdateBalanceByIdAsync(1, 150);
    }

    [Fact]
    public async Task CrashGamePlay_ShouldReturnLose_WhenMultiplierGreaterOrEqualCrashPoint()
    {
        var user = new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        };
        _userRepo.GetByIdAsync(1).Returns(user);

        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 2.0 };
        _rng.Generate().Returns(1.5);

        var result = await _uut.CrashGamePlay(request, 1);

        Assert.False(result.IsWin);
        Assert.Equal(-100, result.Payout);
        await _userRepo.Received().UpdateBalanceByIdAsync(1, -100);
        await _userRepo.DidNotReceive().UpdateBalanceByIdAsync(1, Arg.Is<int>(x => x > 0));

    }

    // ---------- Transaction tests ----------
    [Fact]
    public async Task CrashGamePlay_ShouldAddTransaction_WhenGamePlayed()
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        });

        _rng.Generate().Returns(1.5);
        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 2.0 };

        var _ = await _uut.CrashGamePlay(request, 1);

        await _transactionRepo.Received(1).AddAsync(Arg.Any<Transaction>());
        await _transactionRepo.Received(1).SaveChangesAsync();
    }

    // ---------- Repo call verification ----------
    [Fact]
    public async Task CrashGamePlay_ShouldCallUpdateBalance_WhenBetPlaced()
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        });

        _rng.Generate().Returns(1.5);
        var request = new CrashGameRequestDto { BetAmount = 100, CashoutMultiplier = 2.0 };

        await _uut.CrashGamePlay(request, 1);

        await _userRepo.Received().UpdateBalanceByIdAsync(1, -100);
    }
}