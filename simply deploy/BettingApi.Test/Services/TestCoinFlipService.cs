using Xunit;
using NSubstitute;
using BettingApi.Services;
using BettingApi.Repositories;
using BettingApi.Models;
using BettingApi.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Xml.Serialization;

public class CoinFlipServiceTests
{
    private readonly CoinFlipService _uut;
    private readonly IUserRepository _userRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICoinFlipRandommizer _coinRandommizer;



    public CoinFlipServiceTests()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _coinRandommizer = Substitute.For<ICoinFlipRandommizer>();
        _uut = new CoinFlipService(_transactionRepo, _userRepo, _coinRandommizer);
    }

    // ---------- Exception / fejl scenarier ----------
    [Fact]
    public async Task CoinFlipGamePlay_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns((UserAccount)null);
        var request = new CoinFlipRequestDto { BetAmount = 100, Choice = "heads" };
        // Act + Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CoinFlipGamePlay(request, 1));
        Assert.Equal("Coin flip transaction failed: User not found or inactive", ex.Message);
    }

    [Fact]
    public async Task CoinFlipGamePlay_ShouldThrow_WhenUserInactive()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = false
        });

        var request = new CoinFlipRequestDto { BetAmount = 100, Choice = "heads" };
        // Act + Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CoinFlipGamePlay(request, 1));
        Assert.Equal("Coin flip transaction failed: User not found or inactive", ex.Message);
    }



    [Theory]// bva og ep
    [InlineData(-1)]   // negative → invalid
    [InlineData(0)]    // zero → invalid
    [InlineData(1)]    // min valid → valid
    [InlineData(10)]   // max valid → valid
    [InlineData(11)]   // over balance → invalid
    public async Task CoinFlipGamePlay_BetAmountBoundaryTests(int amount)
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 10,
            ActiveStatus = true
        });

        var request = new CoinFlipRequestDto { BetAmount = amount, Choice = "heads" };

        if (amount <= 0 || amount > 10)
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => _uut.CoinFlipGamePlay(request, 1));
            Assert.Contains("Insufficient funds", ex.Message);
        }
        else
        {
            var result = await _uut.CoinFlipGamePlay(request, 1);
            Assert.NotNull(result); //at spillet succeded
        }
    }

    // // ---------- Resultat / payout scenarier ----------

    [Fact]
    public async Task CoinFlipGamePlay_ShouldReturnWin_WhenChoiceMatches()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        });

        var request = new CoinFlipRequestDto { BetAmount = 100, Choice = "heads" };
        // Act + Assert
        _coinRandommizer.GetCoinResult().Returns("heads");
        var pay = await _uut.CoinFlipGamePlay(request,1);
        Assert.Equal(request.BetAmount,pay.Payout);

    }

    [Fact]
    public async Task CoinFlipGamePlay_ShouldReturnLose_WhenChoiceDoesNotMatch()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        });

        var request = new CoinFlipRequestDto { BetAmount = 100, Choice = "heads" };
        // Act + Assert
        _coinRandommizer.GetCoinResult().Returns("tails");
        var pay = await _uut.CoinFlipGamePlay(request, 1);
        Assert.Equal(-request.BetAmount,pay.Payout);
    }

 
    [Fact]
    public async Task CoinFlipGamePlay_ShouldAddNewTransaction_WhenGamePlayed()
    {
                // Arrange
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = true
        });

        var request = new CoinFlipRequestDto { BetAmount = 100, Choice = "heads" };
        // Act + Assert
        _coinRandommizer.GetCoinResult().Returns("tails");
        var pay = await _uut.CoinFlipGamePlay(request,1);
        await _transactionRepo.Received(1).AddAsync(Arg.Any<Transaction>());

    }

}