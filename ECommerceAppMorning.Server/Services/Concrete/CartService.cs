using ECommerceAppMorning.Server.DTOs.Cart;
using ECommerceAppMorning.Server.Entities;
using ECommerceAppMorning.Server.Repositories;
using ECommerceAppMorning.Server.Repository.Abstract;

namespace ECommerceAppMorning.Server.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartService(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart is null)
            {
                return new CartDto
                {
                    Id = 0,
                    UserId = userId,
                    Items = new List<CartItemDto>(),
                    Total = 0
                };
            }

            var itemDtos = cart.Items.Select(x => new CartItemDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                Name = x.Product.Name,
                Price = x.Product.Price,
                ImageUrl = x.Product.ImageUrl,
                Quantity = x.Quantity,
                Subtotal = x.Product.Price * x.Quantity
            }).ToList();

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                Items = itemDtos,
                Total = itemDtos.Sum(x => x.Subtotal)
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> AddToCartAsync(int userId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0)
                return (false, "Quantity must be greater than zero.");

            var product = await _cartRepository.GetProductByIdAsync(dto.ProductId);

            if (product is null)
                return (false, "Product not found.");

            if (product.Stock < dto.Quantity)
                return (false, "Not enough stock.");

            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart is null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepository.AddCartAsync(cart);
            }

            var existingItem = cart.Items.FirstOrDefault(x => x.ProductId == dto.ProductId);

            if (existingItem is not null)
            {
                if (existingItem.Quantity + dto.Quantity > product.Stock)
                    return (false, "Not enough stock.");

                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            await _cartRepository.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateQuantityAsync(int userId, int productId, int quantity)
        {
            if (quantity <= 0)
                return (false, "Quantity must be greater than zero.");

            var cartItem = await _cartRepository.GetCartItemAsync(userId, productId);

            if (cartItem is null)
                return (false, "Cart item not found.");

            if (quantity > cartItem.Product.Stock)
                return (false, "Not enough stock.");

            cartItem.Quantity = quantity;

            await _cartRepository.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RemoveFromCartAsync(int userId, int productId)
        {
            var cartItem = await _cartRepository.GetCartItemAsync(userId, productId);

            if (cartItem is null)
                return (false, "Cart item not found.");

            _cartRepository.RemoveCartItem(cartItem);
            await _cartRepository.SaveChangesAsync();

            return (true, null);
        }

        public async Task ClearCartAsync(int userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            if (cart is null)
                return;

            _cartRepository.RemoveCartItems(cart.Items);
            await _cartRepository.SaveChangesAsync();
        }
    }
}