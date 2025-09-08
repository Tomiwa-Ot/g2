using G2.Infrastructure.Model;
using G2.Service.Transaction;
using G2.Service.Transaction.Dto.Receiving;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2.API.Controllers
{
    [Route("api/transaction")]
    [ApiController]
    public class TransactionController: ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        [Authorize]
        public async Task<Response> AddTransaction(AddTransactionDto addTransactionDto)
        {
            return await _transactionService.AddTransaction(addTransactionDto);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<Response> GetTransactionById(long id)
        {
            return await _transactionService.GetTransactionById(id);
        }

        [HttpGet]
        [Authorize]
        public async Task<Response> GetUserTransactions([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            return await _transactionService.GetUserTransactions(page, limit);
        }

        [HttpGet("all")]
        [Authorize]
        public async Task<Response> GetTransactions([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? query = null)
        {
            return await _transactionService.GetTransactions(page, limit, query);
        }

        [HttpPost("ercaspay")]
        public async Task<Response> UpdateErcaspayTransaction(UpdateErcaspayTransactionDto updateTransactionDto)
        {
            return await _transactionService.UpdateErcaspayTransaction(updateTransactionDto);
        }

        [HttpPost("flutterwave")]
        public async Task<Response> UpdateFlutterwaveTransaction(UpdateFlutterwaveTransactionDto updateFlutterwave)
        {
            return await _transactionService.UpdateFlutterwaveTransaction(updateFlutterwave);
        }

        [HttpPost("flutterwave/verify")]
        public async Task<Response> VerifyFlutterwave(VerifyFlutterwave verifyFlutterwave)
        {
            return await _transactionService.VerifyFlutterwave(verifyFlutterwave);
        }
    }
}