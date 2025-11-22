using Microsoft.AspNetCore.Mvc;
using BettingApi.Models;
using BettingApi.Repositories;
using BettingApi.Dto;
using Microsoft.AspNetCore.Authorization;

namespace BettingApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IDepositRepository _depositRepository;
        private readonly ITransactionRepository _transactionRepository;



        public AdminController(IUserRepository userRepository, IDepositRepository depositRepository, ITransactionRepository transactionRepository)
        {
            _userRepository = userRepository;
            _depositRepository = depositRepository;
            _transactionRepository = transactionRepository;

        }

        [Authorize(Roles = "Admin")]
        [HttpPut("/[controller]/activeStatus")]
        public async Task<ActionResult> SetUserActiveState(int id, bool status)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null) return NotFound("User does not exist");

            await _userRepository.SetActiveStatusByIdAsync(id, status);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/[controller]/user")]
        public async Task<ActionResult<IEnumerable<UserInfoDto>>> GetAllUserInfoAsync()
        {
            var users = await _userRepository.GetAllUserInfoAsync();

            if (users != null)
            {
                return Ok(users);
            }
            else
                return NotFound($"Users was not found");

        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/[controller]/deposit")]
        public async Task<ActionResult<IEnumerable<DepositResultDto>>> GetAllUserDepositAsync()
        {
            var deposit = await _depositRepository.GetAllDepositForAllUsersAsync();

            if (deposit.Any())
            {
                return Ok(deposit);

            }
            else
                return NotFound($"No deposit was found");

        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/[controller]/transaction")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetAllUserTransactionAsync()
        {
            var transaction = await _transactionRepository.GetAllTransactionForAllUserAsync();

            if (transaction.Any())
            {

                return Ok(transaction);

            }
            else
                return NotFound($"No transaction was found");

        }

        [Authorize(Roles = "Admin")]
        [HttpGet("/[controller]/transaction/{id}")]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactionByIdAsync(int id)
        {
            var transaction = await _transactionRepository.GetAllTransactionByUserIdAsync(id);

            if (transaction.Any())
            {

                return Ok(transaction);

            }
            else
                return NotFound($"No transaction was found");

        }


        [Authorize(Roles = "Admin")]
        [HttpGet("/[controller]/deposit/{id}")]
        public async Task<ActionResult<IEnumerable<DepositResultDto>>> GetUserDepositByIdAsync(int id)
        {
            var deposit = await _depositRepository.GetAllDepositByUserIdAsync(id);

            if (deposit.Any())
            {
                return Ok(deposit);
            }
            else
                return NotFound($"No transaction was found");

        }

    }
}