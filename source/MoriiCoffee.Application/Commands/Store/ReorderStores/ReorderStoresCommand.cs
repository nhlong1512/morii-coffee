using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Store.ReorderStores;

/// <summary>Command to batch-update the display order of multiple stores in a single operation.</summary>
public class ReorderStoresCommand : ICommand<bool>
{
    public ReorderStoresCommand(ReorderStoresDto dto) => Items = dto.Items;

    /// <summary>The list of store ID / display order pairs to update.</summary>
    public List<ReorderStoreItem> Items { get; }
}
