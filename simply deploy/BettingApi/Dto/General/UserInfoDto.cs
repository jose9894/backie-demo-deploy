
namespace BettingApi.Dto
{
    public class UserInfoDto
    {
        public int UserAccountId { get; set; }
        public string UserName { get; set; }
        public int Balance { get; set; } = 0;
        public int? DepositLimit { get; set; }
        public bool ActiveStatus { get; set; } = true;

    }
}
