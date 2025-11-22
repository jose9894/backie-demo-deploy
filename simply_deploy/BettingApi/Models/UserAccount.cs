namespace BettingApi.Models
{
    public class UserAccount
    {
        public int UserAccountId { get; set; }
        public string UserName { get; set; }
        public int Balance { get; set; } = 0;
        public int? DepositLimit { get; set; }
        public bool ActiveStatus { get; set; } = true;

        public ApiUser? ApiUser { get; set; }

        // 1:N relation til Transaction
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Deposit> Deposits { get; set; } = new List<Deposit>();

    }
}
