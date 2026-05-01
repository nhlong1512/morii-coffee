using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using OrderItemDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderItemDto;

namespace MoriiCoffee.Application.Commands.Order.PlaceOrder;

/// <summary>
/// Handles <see cref="PlaceOrderCommand"/> by converting the user's cart into a persisted order,
/// optionally saving the delivery profile, and clearing the cart on success.
/// </summary>
public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly IOrderIdGenerator _orderIdGenerator;

    /// <summary>Initialises the handler with its required dependencies.</summary>
    public PlaceOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICartService cartService,
        IOrderIdGenerator orderIdGenerator)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _orderIdGenerator = orderIdGenerator;
    }

    /// <inheritdoc />
    public async Task<OrderDto> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        // 1. Load current cart
        var cart = await _cartService.GetCartAsync(command.UserId);

        if (cart.Items.Count == 0)
            throw new BadRequestException("Cart is empty.");

        // 2. Build order items from cart snapshot (prices already validated at add-to-cart time)
        var orderItems = cart.Items
            .Select(item => OrderItem.Create(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.VariantId,
                item.VariantLabel))
            .ToList();

        // 3. Generate order number
        var orderNumber = await _orderIdGenerator.GenerateAsync();

        // 4. Create domain objects
        var deliveryInfo = new DeliveryInfo(command.FullName, command.PhoneNumber, command.Address);

        var order = OrderEntity.Create(
            orderNumber,
            command.UserId,
            deliveryInfo,
            orderItems,
            command.PaymentMethod,
            command.Notes);

        // 5. Persist inside a transaction
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Orders.CreateAsync(order);

            if (command.SaveDeliveryProfile)
            {
                var existingProfile = await _unitOfWork.UserDeliveryProfiles.GetByUserIdAsync(command.UserId);

                if (existingProfile is null)
                {
                    var newProfile = UserDeliveryProfile.Create(
                        command.UserId,
                        command.FullName,
                        command.PhoneNumber,
                        command.Address);
                    await _unitOfWork.UserDeliveryProfiles.UpsertAsync(newProfile);
                }
                else
                {
                    existingProfile.Update(command.FullName, command.PhoneNumber, command.Address);
                    await _unitOfWork.UserDeliveryProfiles.UpsertAsync(existingProfile);
                }
            }
        });

        // 6. Clear cart after successful transaction
        await _cartService.ClearCartAsync(command.UserId);

        return MapToDto(order);
    }

    /// <summary>Maps an <see cref="OrderEntity"/> aggregate to its full <see cref="OrderDto"/> representation.</summary>
    private static OrderDto MapToDto(OrderEntity order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        UserId = order.UserId,
        DeliveryFullName = order.DeliveryInfo.FullName,
        DeliveryPhoneNumber = order.DeliveryInfo.PhoneNumber,
        DeliveryAddress = order.DeliveryInfo.Address,
        Notes = order.Notes,
        PaymentMethod = order.PaymentMethod,
        Subtotal = order.Subtotal,
        Tax = order.Tax,
        Shipping = order.Shipping,
        Discount = order.Discount,
        Total = order.Total,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            VariantId = i.VariantId,
            VariantLabel = i.VariantLabel,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.LineTotal
        }).ToList()
    };
}
