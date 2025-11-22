using Microsoft.AspNetCore.Identity;

namespace BettingApi.Models
{
    public class ApiUser : IdentityUser
    {
        public int? UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }
        
        public RefreshToken? RefreshToken { get; set; }
    }

}