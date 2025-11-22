
namespace BettingApi.Models
{
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }
        public string Token {get;set;}
        public DateTime ExpirationDate { get; set; }
        public string ApiUserId { get; set; }
        public ApiUser ApiUser { get; set; }
    }
}
