
namespace BettingApi.Dto
{
    public class TransactionDto
    {
        public int TransactionId { get; set; }
        public DateOnly Date { get; set; }
        public double Amount { get; set; }
        public string GameName { get; set; }

    }
}
