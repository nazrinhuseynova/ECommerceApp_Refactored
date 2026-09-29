using ECommerceAppMorning.Server.DTOs.Cart;
using ECommerceAppMorning.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAppMorning.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("{userId:int}/items")]
        public async Task<IActionResult> AddToCart(int userId, AddToCartDto dto)
        {
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

            if (!success)
                return BadRequest(errorMessage);

            return Ok();
        }

        [HttpPut("{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> UpdateQuantity(int userId, int productId, [FromQuery] int quantity)
        {
            var (success, errorMessage) = await _cartService.UpdateQuantityAsync(userId, productId, quantity);

            if (!success)
                return BadRequest(errorMessage);

            return Ok();
        }

        [HttpDelete("{userId:int}/items/{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(int userId, int productId)
        {
            var (success, errorMessage) = await _cartService.RemoveFromCartAsync(userId, productId);

            if (!success)
                return NotFound(errorMessage);

            return NoContent();
        }

        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            await _cartService.ClearCartAsync(userId);
            return NoContent();
        }
    }
}