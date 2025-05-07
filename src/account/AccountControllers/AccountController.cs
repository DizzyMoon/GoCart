using Microsoft.AspNetCore.Mvc;
using Account.AccountModels;
using Account.AccountService;

namespace Account.AccountControllers 
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }
        /// <summary>
        /// Get all accounts
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [Produces("application/json")]
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetQueryCollection()
        {
            var result = await _accountService.GetQueryCollection();
            return Ok(result);
        }

        /// <summary>
        /// Get account by ID 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            var account = await _accountService.Get(id);
            if (account == null)
                return NotFound();
    
            return Ok(account);
        }

        /// <summary>
        /// Opret en ny Account
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="405">Method Not Allowed</response>
        /// <returns>Nye account returneres</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [ProducesResponseType(StatusCodes.Status405MethodNotAllowed, Type = typeof(AccountModel))]
        [Produces("application/json")]
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Create([FromBody] AccountModelRequest dto)
        {
            try
            {
                var result = await _accountService.Create(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed, ex.Message);
            }
        }
        
        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] AccountUpdateRequest account)
        {
            try
            {
                var updated = await _accountService.Update(id, account);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Slet en Account 
        /// </summary>
        /// <param name="accountId"></param>
        /// <response code="200">Success</response>
        /// <returns>Slettet account returneres</returns>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [Produces("application/json")]
        [HttpDelete]
        [Route("{accountId:int}")]
        public async Task<ActionResult> Delete([FromRoute] int accountId)
        {
            var result = await _accountService.Delete(accountId);
            return Ok(result);
        }
    }
}