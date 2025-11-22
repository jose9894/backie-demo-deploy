using Microsoft.EntityFrameworkCore;
using BettingApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BettingApi.Data
{
    public class BetAppDbContext : IdentityDbContext<ApiUser>
    {
        public BetAppDbContext(DbContextOptions<BetAppDbContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Deposit> Deposits { get; set; }

    }
}
