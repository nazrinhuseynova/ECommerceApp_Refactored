using ECommerceAppMorning.Server.DTOs.Cart;

namespace ECommerceAppMorning.Server.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<(bool Success, string? ErrorMessage)> AddToCartAsync(int userId, AddToCartDto dto);
        Task<(bool Success, string? ErrorMessage)> UpdateQuantityAsync(int userId, int productId, int quantity);
        Task<(bool Success, string? ErrorMessage)> RemoveFromCartAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
    }
}