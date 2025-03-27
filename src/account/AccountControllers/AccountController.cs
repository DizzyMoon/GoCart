using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Account.Models;
using Service;

namespace AccountControllers 
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<AccountModel>))]
        public async Task<IActionResult> GetAllAsync()
        {
            var accounts = await _accountService.GetAllAsync();
            return Ok(accounts);
        }

        /// <summary>
        /// Get account by ID 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccountModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id)
        {
            var account = await _accountService.GetByIdAsync(id);
            if (account == null)
                return NotFound();
            
            return Ok(account);
        }

        /// <summary>
        /// Create new account 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AccountModel))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAsync([FromBody] AccountModel account)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            await _accountService.CreateAsync(account);
            return CreatedAtAction(nameof(GetByIdAsync), new {id = account.Id}, account);
        }

        /// <summary>
        /// Update an existing account 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updatedAccount"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAsync([FromRoute] Guid id, [FromBody] AccountModel updatedAccount)
        {
            try
            {
                await _accountService.UpdateAsync(id, updatedAccount);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(new {message = ex.Message});
            }
        }

        /// <summary>
        /// Delete an account 
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteAsync([FromRoute] Guid id )
        {
            try 
            {
                await _accountService.DeleteAsync(id);
                return NoContent();
            }
            catch(Exception ex)
            {
                return NotFound(new { message = ex.Message});
            }
        }







    }
}