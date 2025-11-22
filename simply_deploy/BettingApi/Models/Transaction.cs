
namespace BettingApi.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public DateOnly Date { get; set; }
        public int Amount { get; set; }
        public string GameName {get; set;}

        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }
    }
}
