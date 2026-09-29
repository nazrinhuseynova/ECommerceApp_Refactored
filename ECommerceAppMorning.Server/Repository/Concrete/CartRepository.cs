using ECommerceAppMorning.Server.Data;
using ECommerceAppMorning.Server.Entities;
using ECommerceAppMorning.Server.Repository.Abstract;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAppMorning.Server.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<CartItem?> GetCartItemAsync(int userId, int productId)
        {
            return await _context.CartItems
                .Include(x => x.Cart)
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Cart.UserId == userId && x.ProductId == productId);
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            return await _context.Products
                .FirstOrDefaultAsync(x => x.Id == productId);
        }

        public async Task AddCartAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public void RemoveCartItem(CartItem cartItem)
        {
            _context.CartItems.Remove(cartItem);
        }

        public void RemoveCartItems(IEnumerable<CartItem> cartItems)
        {
            _context.CartItems.RemoveRange(cartItems);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}