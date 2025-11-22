namespace BettingApi.Dto
{
    public class DepositResultDto
    {
        public int DepositId { get; set; }

        public DateOnly Date { get; set; }
        public int Amount { get; set; }
    }
}
