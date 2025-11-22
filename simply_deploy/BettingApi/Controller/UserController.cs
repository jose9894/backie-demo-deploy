using Microsoft.AspNetCore.Mvc;
using BettingApi.Models;
using BettingApi.Repositories;
using BettingApi.Dto;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BettingApi.Services;

namespace BettingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IDepositService _depositService;
        private readonly UserManager<ApiUser> _userManager;
        private readonly ITransactionRepository _transactionRepository;

        public UserController(IUserRepository userRepository, IDepositService depositService, 
        ITransactionRepository transactionRepository, UserManager<ApiUser> userManager)
        {
            _userRepository = userRepository;
            _depositService = depositService;
            _userManager = userManager;
            _transactionRepository = transactionRepository;
        }


        [Authorize(Roles = "User")]
        [HttpPost("deposit")]
        public async Task<ActionResult> Deposit(int amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                await _depositService.AddDepositAsync(amount, user.UserAccountId ?? 0);
                return StatusCode(201);
            }

            return NotFound();

        }

        [Authorize(Roles = "User")]
        [HttpPut("depositlimit")]
        public async Task<ActionResult> SetUserDepositLimit(int amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                await _userRepository.SetDepositLimitByIdAsync(user.UserAccountId ?? 0, amount);
                return NoContent();
            }

            return NotFound("User not found");

        }

        [Authorize(Roles = "User")]
        [HttpDelete("account")]
        public async Task<ActionResult> DeleteUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
              var userAccount = await _userRepository.GetByIdAsync(user.UserAccountId ?? 0);
                userAccount.UserName ="DELETED";
                userAccount.Balance = 0;
                userAccount.DepositLimit = null;
                userAccount.ActiveStatus = false;

               await _userRepository.SaveChangesAsync();


                await _userManager.DeleteAsync(user);

                return NoContent();
            }
            return NotFound("User not found");
        }


        [Authorize(Roles = "User")]
        [HttpGet("/deposit")]
        public async Task<ActionResult<IEnumerable<DepositResultDto>>> GetAllUserDepositAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                var deposit = await _depositService.GetAllDepositByUserIdAsync(user.UserAccountId ?? 0);
                return Ok(deposit);

            }
            else
                return NotFound($"User not found");

        }

        [Authorize(Roles = "User")]
        [HttpGet("/transaction")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllUserTransactionAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                var deposit = await _transactionRepository.GetAllTransactionByUserIdAsync(user.UserAccountId ?? 0);
                return Ok(deposit);

            }
            else
                return NotFound($"User not found");

        }

    }
}