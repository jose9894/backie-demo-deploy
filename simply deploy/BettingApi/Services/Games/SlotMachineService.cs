using BettingApi.Dto;
using BettingApi.Models;
using BettingApi.Repositories;

namespace BettingApi.Services;

public class Payline
{
    public (int, int)[] Coords { get; set; }
    public string Type { get; set; }
}

public class SymbolPayout
{
    public int Horizontal3 { get; set; }
    public int Vertical3 { get; set; }
    public int Diagonal3 { get; set; }
}

public interface IGenerateGrid
{
    string[][] MakeGrid();
}

public class GenerateGrid : IGenerateGrid
{
    private readonly Random _random = new();
    private static readonly string[] SYMBOLS = { "🍒", "🍀", "9️⃣", "🪙" };

    public string[][] MakeGrid()
    {
        var grid = new string[3][];
        for (int r = 0; r < 3; r++)
        {
            grid[r] = new string[3];
            for (int c = 0; c < 3; c++)
            {
                grid[r][c] = SYMBOLS[_random.Next(SYMBOLS.Length)];
            }
        }

        return grid;
    }
}

public interface ICalculatePayout
{
    int CalcPayout(string[][] grid, int bet);
}


public class CalculatePayout : ICalculatePayout
{
    private readonly Dictionary<string, SymbolPayout> PAYOUTS = new()
    {
        ["🍒"] = new SymbolPayout { Horizontal3 = 10, Vertical3 = 15, Diagonal3 = 12 },
        ["🍀"] = new SymbolPayout { Horizontal3 = 12, Vertical3 = 15, Diagonal3 = 14 },
        ["9️⃣"] = new SymbolPayout { Horizontal3 = 50, Vertical3 = 60, Diagonal3 = 55 },
        ["🪙"] = new SymbolPayout { Horizontal3 = 25, Vertical3 = 30, Diagonal3 = 28 }
    };
    private readonly List<Payline> PAYLINES = new()
    {
        // Horizontal
        new Payline { Coords = new[]{(0,0),(0,1),(0,2)}, Type = "horizontal3" },
        new Payline { Coords = new[]{(1,0),(1,1),(1,2)}, Type = "horizontal3" },
        new Payline { Coords = new[]{(2,0),(2,1),(2,2)}, Type = "horizontal3" },
        // Vertical
        new Payline { Coords = new[]{(0,0),(1,0),(2,0)}, Type = "vertical3" },
        new Payline { Coords = new[]{(0,1),(1,1),(2,1)}, Type = "vertical3" },
        new Payline { Coords = new[]{(0,2),(1,2),(2,2)}, Type = "vertical3" },
        // Diagonal
        new Payline { Coords = new[]{(0,0),(1,1),(2,2)}, Type = "diagonal3" },
        new Payline { Coords = new[]{(0,2),(1,1),(2,0)}, Type = "diagonal3" },


    };

    public int CalcPayout(string[][] grid, int bet)
    {
        int total = 0;

        foreach (var line in PAYLINES)
        {
            var firstCoord = line.Coords[0];
            var first = grid[firstCoord.Item1][firstCoord.Item2];

            bool allSame = line.Coords.All(p => grid[p.Item1][p.Item2] == first);

            if (allSame && PAYOUTS.ContainsKey(first))
            {
                var symbolPayout = PAYOUTS[first];
                int multiplier = line.Type switch
                {
                    "horizontal3" => symbolPayout.Horizontal3,
                    "vertical3" => symbolPayout.Vertical3,
                    "diagonal3" => symbolPayout.Diagonal3,
                    _ => 0
                };

                total += (int)Math.Round(multiplier * (bet / 10.0));
            }
        }

        return total;
    }
}

public interface ISlotMachineService
{
    Task<SlotMachineResultDto> SlotMachinePlay(int bet, int id);
}

public class SlotMachineService : ISlotMachineService
{
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IGenerateGrid _generateGrid;
    private readonly ICalculatePayout _calculatePayout;

    public SlotMachineService(
        IUserRepository userRepository,
        ITransactionRepository transactionRepository,
        IGenerateGrid generateGrid,
        ICalculatePayout calculatePayout)
    {
        _userRepository=userRepository;
        _transactionRepository=transactionRepository;
        _generateGrid=generateGrid;
        _calculatePayout=calculatePayout;
    }

    public async Task<SlotMachineResultDto> SlotMachinePlay(int bet, int id)
        {
             
            using var dbOperation = await _userRepository.BeginTransactionAsync(); //gotta add try catch and end of transaction if we have this line
            
            try
            {
                var user=await _userRepository.GetByIdAsync(id);

                if(user!=null && user.ActiveStatus)
                {
                    if(bet<=user.Balance && bet > 0)
                    {
                        DateOnly dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
                        await _userRepository.UpdateBalanceByIdAsync(id, -bet);

                        var grid = _generateGrid.MakeGrid();
                        var payout = _calculatePayout.CalcPayout(grid, bet);
                        
                        var result = new SlotMachineResultDto
                        {
                            FinalGrid = grid,
                            Payout = -bet
                        };

                        if(payout>0)
                        {
                            await _userRepository.UpdateBalanceByIdAsync(id, payout); 
                            result.Payout+=payout;
                        }

                        // var transactions= await _transacationRepository.GetTransactionByGameNameAsync(id, "Slot", dateNow);

                        // if(transactions.Any())
                        // {
                        //     foreach(var transaction in transactions)
                        //     {
                        //         await _transacationRepository.UpdateGameTransactionByIdAsync(transaction.TransactionId, result.Payout);
                        //     }
                        // }
                        // else
                        // {
                            var Transaction = new Transaction
                            {
                                UserAccountId=id, 
                                Date=dateNow,
                                Amount=result.Payout,
                                GameName="Slot"
                            };

                            await _transactionRepository.AddAsync(Transaction);
                            await _transactionRepository.SaveChangesAsync();
                        // }

                        await dbOperation.CommitAsync();
                        return result;

                    }

                    else
                    {
                        throw new Exception("Insufficient funds");
                    }

                }

                throw new Exception ("User not found or inactive");

            }
            catch(Exception ex)
            {
                await dbOperation.RollbackAsync();
                throw new Exception($"Slotmachine transaction failed: {ex.Message}");
            }
            
        }


}


