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


//malleballe
public class SlotMachineServiceTest
{
    private readonly SlotMachineService _uut;
    private readonly ICalculatePayout _calculatePayout;
    private readonly IGenerateGrid _generateGrid;
    private readonly IUserRepository _userRepo;
    private readonly ITransactionRepository _transactionRepo;

    public SlotMachineServiceTest()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _transactionRepo = Substitute.For<ITransactionRepository>();
        _calculatePayout = Substitute.For<ICalculatePayout>();
        _generateGrid = Substitute.For<IGenerateGrid>();
        _uut = new SlotMachineService(_userRepo, _transactionRepo, _generateGrid, _calculatePayout);
    }

    // ---------- Exception / fejl scenarier ----------
    [Fact]
    public async Task SlotMachineGamePlay_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns((UserAccount)null);
        // Act + Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.SlotMachinePlay(100, 1));
        Assert.Equal("Slotmachine transaction failed: User not found or inactive", ex.Message);
    }

    [Fact]
    public async Task SlotMachineGamePlay_ShouldThrow_WhenUserInactive()
    {
        // Arrange
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 1000,
            ActiveStatus = false
        });

        // Act + Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _uut.SlotMachinePlay(100, 1));
        Assert.Equal("Slotmachine transaction failed: User not found or inactive", ex.Message);
    }

    [Theory]// bva og ep
    [InlineData(-1)]   // negative → invalid
    [InlineData(0)]    // zero → invalid
    [InlineData(1)]    // min valid → valid
    [InlineData(10)]   // max valid → valid
    [InlineData(11)]   // over balance → invalid
    public async Task SlotMachineGamePlay_BetAmountBoundaryTests(int amount)
    {
        _userRepo.GetByIdAsync(1).Returns(new UserAccount
        {
            UserAccountId = 1,
            UserName = "Hans",
            Balance = 10,
            ActiveStatus = true
        });

        if (amount <= 0 || amount > 10)
        {
            var ex = await Assert.ThrowsAsync<Exception>(() => _uut.SlotMachinePlay(amount, 1));
            Assert.Contains("Insufficient funds", ex.Message);
        }
        else
        {
            var result = await _uut.SlotMachinePlay(amount, 1);
            Assert.NotNull(result); //at spillet succeded
        }
    }


    // ---------- Resultat / payout scenarier ----------
    [Fact]
    public async Task SlotMacginePlay_ShouldReturnNegativeResult_WhenNoWin()
    {
        var grid = new[] { new[]{"🍒","🍋","🪙"},
                           new[]{"🍀","9️⃣","🍋"},
                           new[]{"🪙","🍒","🍀"}};

        var user = new UserAccount { UserAccountId = 1, Balance = 100, ActiveStatus = true };
        _userRepo.GetByIdAsync(1).Returns(user);
        _generateGrid.MakeGrid().Returns(grid);
        _calculatePayout.CalcPayout(grid, 10).Returns(0);

        var result = await _uut.SlotMachinePlay(10, 1);

        Assert.Equal(-10, result.Payout);
        await _userRepo.Received().UpdateBalanceByIdAsync(1, -10);
        await _userRepo.DidNotReceive().UpdateBalanceByIdAsync(1, 10);

    }

    [Fact]
    public async Task SlotMacginePlay_ShouldReturnPositiveResult_WhenWin()
    {
        var grid = new[] { new[]{"🍒","🍋","🪙"},
                           new[]{"🍒","9️⃣","🍋"},
                           new[]{"🍒","🍒","🍀"}};

        var user = new UserAccount { UserAccountId = 1, Balance = 100, ActiveStatus = true };
        _userRepo.GetByIdAsync(1).Returns(user);
        _generateGrid.MakeGrid().Returns(grid);
        _calculatePayout.CalcPayout(grid, 10).Returns(15);

        var result = await _uut.SlotMachinePlay(10, 1);

        Assert.Equal(5, result.Payout);
        await _userRepo.Received().UpdateBalanceByIdAsync(1, -10);
        await _userRepo.Received().UpdateBalanceByIdAsync(1, 15);
    }

    //Test af CalculatePayout
    // Bet med 10. Skal give det samme tilbage, som står iexpect
    [Theory]
    [InlineData("🍒", "horizontal3", 10)]
    [InlineData("🍒", "vertical3", 15)]
    [InlineData("🍒", "diagonal3", 12)]
    [InlineData("🍒", "fullgrid", (10 * 3) + (15 * 3) + (12 * 2))]
    [InlineData("🍀", "horizontal3", 12)]
    [InlineData("🍀", "vertical3", 15)]
    [InlineData("🍀", "diagonal3", 14)]
    [InlineData("🍀", "fullgrid", (12 * 3) + (15 * 3) + (14 * 2))]
    [InlineData("9️⃣", "horizontal3", 50)]
    [InlineData("9️⃣", "vertical3", 60)]
    [InlineData("9️⃣", "diagonal3", 55)]
    [InlineData("9️⃣", "fullgrid", (50 * 3) + (60 * 3) + (55 * 2))]
    [InlineData("🪙", "horizontal3", 25)]
    [InlineData("🪙", "vertical3", 30)]
    [InlineData("🪙", "diagonal3", 28)]
    [InlineData("🪙", "fullgrid", (25 * 3) + (30 * 3) + (28 * 2))]
    [InlineData("🍒", "WhenTopRowAndFirstColumnAreCherries", 10 + 15)]// CalcPayout_ShouldReturn25_WhenTopRowAndFirstColumnAreCherries
    [InlineData("9️⃣", "pluslines_symbol9", 50 + 60)] // Testing a plus sign combination
    [InlineData("🍀", "xlinesTrekløver", 14 * 2)] // Testing a X sign combination
    public void CalcPayout_ShouldReturnExpected_WhenThreeOfSameSymbol(string symbol, string type, int expected)
    {
        var calc = new CalculatePayout();

        string[][] grid = type switch
        {
            "horizontal3" => new[]
            {
                new[] { symbol, symbol, symbol },
                new[] { "🍀","9️⃣","🪙" },
                new[] { "🍋","🍒","🍒" }
            },
            "vertical3" => new[]
            {
                new[] { symbol,"🍀","9️⃣" },
                new[] { symbol,"🍒","🪙" },
                new[] { symbol,"🍋","🍀" }
            },
            "diagonal3" => new[]
            {
                new[] { symbol,"🍀","🪙" },
                new[] { "🍀",symbol,"🍋" },
                new[] { "9️⃣","🍒",symbol }
            },
            "fullgrid" => new[]
            {
                new[] { symbol,symbol,symbol},
                new[] { symbol,symbol,symbol },
                new[] { symbol,symbol,symbol }
            },
            "WhenTopRowAndFirstColumnAreCherries" => new[]
            {
                new[]{symbol,symbol,symbol},
                new[]{symbol,"🍀","🪙"},
                new[]{symbol,"🍀","🍀"}
            },
            "pluslines_symbol9" => new[]
            {
                new[] { "🍒",symbol,"🍒"},
                new[] { symbol,symbol,symbol },
                new[] { "🍋",symbol,"🍒"}
            },
            "xlinesTrekløver" => new[]
            {
                new[] { symbol,"🍒",symbol},
                new[] { "🍒",symbol,"🍒" },
                new[] { symbol,"🍒",symbol}
            },
            _ => throw new ArgumentException("Invalid type")
        };

        int result = calc.CalcPayout(grid, 10);

        Assert.Equal(expected, result);
    }

    // Bet med 20. Skal give det dobbelte tilbage, som står i expected
    [Theory]
    [InlineData("🍒", "horizontal3", 10)]
    [InlineData("🍒", "vertical3", 15)]
    [InlineData("🍒", "diagonal3", 12)]
    [InlineData("🍒", "fullgrid", (10 * 3) + (15 * 3) + (12 * 2))]
    [InlineData("🍀", "horizontal3", 12)]
    [InlineData("🍀", "vertical3", 15)]
    [InlineData("🍀", "diagonal3", 14)]
    [InlineData("🍀", "fullgrid", (12 * 3) + (15 * 3) + (14 * 2))]
    [InlineData("9️⃣", "horizontal3", 50)]
    [InlineData("9️⃣", "vertical3", 60)]
    [InlineData("9️⃣", "diagonal3", 55)]
    [InlineData("9️⃣", "fullgrid", (50 * 3) + (60 * 3) + (55 * 2))]
    [InlineData("🪙", "horizontal3", 25)]
    [InlineData("🪙", "vertical3", 30)]
    [InlineData("🪙", "diagonal3", 28)]
    [InlineData("🪙", "fullgrid", (25 * 3) + (30 * 3) + (28 * 2))]
    [InlineData("🍒", "WhenTopRowAndFirstColumnAreCherries", 10 + 15)]// CalcPayout_ShouldReturn25_WhenTopRowAndFirstColumnAreCherries
    [InlineData("9️⃣", "pluslines_symbol9", 50 + 60)] // Testing a plus sign combination
    [InlineData("🍀", "xlinesTrekløver", 14 * 2)] // Testing a X sign combination
    public void CalcPayout_ShouldReturnExpected_WhenThreeOfSameSymbol_WithBet20(string symbol, string type, int expected)
    {
        var calc = new CalculatePayout();

        string[][] grid = type switch
        {
            "horizontal3" => new[]
            {
                new[] { symbol, symbol, symbol },
                new[] { "🍀","9️⃣","🪙" },
                new[] { "🍋","🍒","🍒" }
            },
            "vertical3" => new[]
            {
                new[] { symbol,"🍀","9️⃣" },
                new[] { symbol,"🍒","🪙" },
                new[] { symbol,"🍋","🍀" }
            },
            "diagonal3" => new[]
            {
                new[] { symbol,"🍀","🪙" },
                new[] { "🍀",symbol,"🍋" },
                new[] { "9️⃣","🍒",symbol }
            },
            "fullgrid" => new[]
            {
                new[] { symbol,symbol,symbol},
                new[] { symbol,symbol,symbol },
                new[] { symbol,symbol,symbol }
            },
            "WhenTopRowAndFirstColumnAreCherries" => new[]
            {
                new[]{symbol,symbol,symbol},
                new[]{symbol,"🍀","🪙"},
                new[]{symbol,"🍀","🍀"}
            },
            "pluslines_symbol9" => new[]
            {
                new[] { "🍒",symbol,"🍒"},
                new[] { symbol,symbol,symbol },
                new[] { "🍋",symbol,"🍒"}
            },
            "xlinesTrekløver" => new[]
            {
                new[] { symbol,"🍒",symbol},
                new[] { "🍒",symbol,"🍒" },
                new[] { symbol,"🍒",symbol}
            },
            _ => throw new ArgumentException("Invalid type")
        };

        int result = calc.CalcPayout(grid, 20);

        Assert.Equal(expected * 2, result);
    }

    [Fact]
    public void CalcPayout_ShouldReturn35_WhenToLinesCherryAndCoin()
    {
        var calc = new CalculatePayout();
        var grid = new[]
        {
            new[]{"🍒","🍒","🍒"},
            new[]{"🪙","🪙","🪙"},
            new[]{"🍒","🍀","🍀"}
        };

        int result = calc.CalcPayout(grid, 10);

        Assert.Equal(35, result);
    }

    [Fact]
    public void CalcPayout_ShouldReturn75_WhenToLinesCherryAndCoin()
    {
        var calc = new CalculatePayout();
        var grid = new[]
        {
            new[]{"9️⃣","🍀","🍒"},
            new[]{"9️⃣","🍀","🪙"},
            new[]{"9️⃣","🍀","🍀"}
        };

        int result = calc.CalcPayout(grid, 10);

        Assert.Equal(75, result);
    }


    //Test af GenerateGrid
    [Fact]
    public void MakeGrid_ShouldReturn3x3Grid()
    {
        var gen = new GenerateGrid();
        var grid = gen.MakeGrid();

        Assert.NotNull(grid);
        Assert.Equal(3, grid.Length);
        Assert.All(grid, row => Assert.Equal(3, row.Length));
    }

    [Fact]
    public void MakeGrid_ShouldOnlyContainValidSymbols()
    {
        var gen = new GenerateGrid();
        var grid = gen.MakeGrid();

        var validSymbols = new[] { "🍒", "🍀", "9️⃣", "🪙" };

        Assert.All(grid.SelectMany(row => row),
            symbol => Assert.Contains(symbol, validSymbols));
    }
}
