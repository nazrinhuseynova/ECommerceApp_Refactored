using ECommerceAppMorning.Server.DTOs.Cart;
using ECommerceAppMorning.Server.Entities;
using ECommerceAppMorning.Server.Repositories;
using ECommerceAppMorning.Server.Repository.Abstract;
using ECommerceAppMorning.Server.Services;
using Moq;
using Xunit;

namespace ECommerceAppMorning.Tests.Services
{
    public class CartServiceTests
    {
        private readonly Mock<ICartRepository> _cartRepoMock;
        private readonly CartService _cartService;

        public CartServiceTests()
        {
            _cartRepoMock = new Mock<ICartRepository>();
            _cartService = new CartService(_cartRepoMock.Object);
        }

        #region GetCartAsync Tests

        [Fact]
        public async Task GetCartAsync_WhenCartDoesNotExist_ReturnsEmptyCartDto()
        {
           
            int userId = 1;
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync((Cart?)null);

          
            var result = await _cartService.GetCartAsync(userId);

          
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        [Fact]
        public async Task GetCartAsync_WhenCartExists_ReturnsMappedCartDtoWithCorrectTotal()
        {
            
            int userId = 1;
            var cart = new Cart
            {
                Id = 10,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem
                    {
                        Id = 1,
                        ProductId = 100,
                        Quantity = 2,
                        Product = new Product { Name = "iPhone 15", Price = 1000, ImageUrl = "iphone.png" }
                    },
                    new CartItem
                    {
                        Id = 2,
                        ProductId = 101,
                        Quantity = 1,
                        Product = new Product { Name = "AirPods", Price = 200, ImageUrl = "airpods.png" }
                    }
                }
            };

            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cart);

         
            var result = await _cartService.GetCartAsync(userId);

          
            Assert.NotNull(result);
            Assert.Equal(10, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2200, result.Total); 
            Assert.Equal("iPhone 15", result.Items[0].Name);
            Assert.Equal(2000, result.Items[0].Subtotal);
        }

        #endregion

        #region AddToCartAsync Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5)]
        public async Task AddToCartAsync_WhenQuantityIsZeroOrNegative_ReturnsBadRequest(int invalidQuantity)
        {
          
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = invalidQuantity };

           
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

         
            Assert.False(success);
            Assert.Equal("Quantity must be greater than zero.", errorMessage);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddToCartAsync_WhenProductNotFound_ReturnsNotFoundError()
        {
      
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 99, Quantity = 2 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId))
                         .ReturnsAsync((Product?)null);

          
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

           
            Assert.False(success);
            Assert.Equal("Product not found.", errorMessage);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddToCartAsync_WhenStockIsInsufficient_ReturnsStockError()
        {
            
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 10 };
            var product = new Product { Id = 1, Stock = 5 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId))
                         .ReturnsAsync(product);

           
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

          
            Assert.False(success);
            Assert.Equal("Not enough stock.", errorMessage);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddToCartAsync_WhenCartDoesNotExist_CreatesNewCartAndAddsItem()
        {
          
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
            var product = new Product { Id = 1, Stock = 10 };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

            
            Assert.True(success);
            Assert.Null(errorMessage);
            _cartRepoMock.Verify(r => r.AddCartAsync(It.Is<Cart>(c => c.UserId == userId)), Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_WhenItemAlreadyExistsInCart_IncreasesQuantity()
        {
            
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 2 };
            var product = new Product { Id = 1, Stock = 10 };
            var existingCart = new Cart
            {
                Id = 1,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 3 }
                }
            };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync(existingCart);

           
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.Equal(5, existingCart.Items.First().Quantity); 
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToCartAsync_WhenExistingItemExceedsStock_ReturnsStockError()
        {
          
            int userId = 1;
            var dto = new AddToCartDto { ProductId = 1, Quantity = 3 };
            var product = new Product { Id = 1, Stock = 5 };
            var existingCart = new Cart
            {
                Id = 1,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem { ProductId = 1, Quantity = 4 }
                }
            };

            _cartRepoMock.Setup(r => r.GetProductByIdAsync(dto.ProductId)).ReturnsAsync(product);
            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId)).ReturnsAsync(existingCart);

         
            var (success, errorMessage) = await _cartService.AddToCartAsync(userId, dto);

          
            Assert.False(success);
            Assert.Equal("Not enough stock.", errorMessage);
            Assert.Equal(4, existingCart.Items.First().Quantity); // dəyişməməlidir
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region UpdateQuantityAsync Tests

        [Fact]
        public async Task UpdateQuantityAsync_WhenCartItemNotFound_ReturnsNotFoundError()
        {
        
            int userId = 1, productId = 10, quantity = 3;
            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync((CartItem?)null);

          
            var (success, errorMessage) = await _cartService.UpdateQuantityAsync(userId, productId, quantity);

           
            Assert.False(success);
            Assert.Equal("Cart item not found.", errorMessage);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateQuantityAsync_WhenQuantityExceedsStock_ReturnsStockError()
        {
          
            int userId = 1, productId = 10, newQuantity = 10;
            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = 1,
                Product = new Product { Id = productId, Stock = 5 }
            };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

        
            var (success, errorMessage) = await _cartService.UpdateQuantityAsync(userId, productId, newQuantity);

           
            Assert.False(success);
            Assert.Equal("Not enough stock.", errorMessage);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateQuantityAsync_WhenValidQuantity_UpdatesAndSaves()
        {
          
            int userId = 1, productId = 10, newQuantity = 4;
            var cartItem = new CartItem
            {
                ProductId = productId,
                Quantity = 1,
                Product = new Product { Id = productId, Stock = 10 }
            };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

           
            var (success, errorMessage) = await _cartService.UpdateQuantityAsync(userId, productId, newQuantity);

           
            Assert.True(success);
            Assert.Null(errorMessage);
            Assert.Equal(4, cartItem.Quantity);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region RemoveFromCartAsync Tests

        [Fact]
        public async Task RemoveFromCartAsync_WhenCartItemExists_RemovesAndSaves()
        {
           
            int userId = 1, productId = 5;
            var cartItem = new CartItem { Id = 12, ProductId = productId };

            _cartRepoMock.Setup(r => r.GetCartItemAsync(userId, productId))
                         .ReturnsAsync(cartItem);

           
            var (success, errorMessage) = await _cartService.RemoveFromCartAsync(userId, productId);

            Assert.True(success);
            Assert.Null(errorMessage);
            _cartRepoMock.Verify(r => r.RemoveCartItem(cartItem), Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region ClearCartAsync Tests

        [Fact]
        public async Task ClearCartAsync_WhenCartExists_RemovesAllItemsAndSaves()
        {
         
            int userId = 1;
            var items = new List<CartItem>
            {
                new CartItem { Id = 1, ProductId = 10 },
                new CartItem { Id = 2, ProductId = 11 }
            };
            var cart = new Cart { Id = 5, UserId = userId, Items = items };

            _cartRepoMock.Setup(r => r.GetCartByUserIdAsync(userId))
                         .ReturnsAsync(cart);

        
            await _cartService.ClearCartAsync(userId);

          
            _cartRepoMock.Verify(r => r.RemoveCartItems(items), Times.Once);
            _cartRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}