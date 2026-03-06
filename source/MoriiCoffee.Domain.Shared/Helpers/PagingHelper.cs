using System.ComponentModel;
using MoriiCoffee.Domain.Shared.Enums.Common;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Domain.Shared.Helpers;

/// <summary>
/// Provides helper methods for paginating in-memory lists and IQueryable data sources.
/// </summary>
public static class PagingHelper
{
    /// <summary>
    /// Paginates an in-memory list of items based on the given filter.
    /// </summary>
    public static Pagination<T> Paginate<T>(List<T> items, PaginationFilter filter)
    {
        int totalCount = items.Count;

        if (filter.Searches != null && filter.Searches.Any())
        {
            items = filter.Searches
                .Aggregate(items, (current, search) =>
                    current.Where(x =>
                            search.SearchValue != null
                            && search.SearchBy != null
                            && ((string?)TypeDescriptor
                                .GetProperties(typeof(T))
                                .Find(search.SearchBy, true)?
                                .GetValue(x) ?? "")
                            .Contains(search.SearchValue, StringComparison.CurrentCultureIgnoreCase))
                        .ToList());
        }

        if (filter.Orders != null && filter.Orders.Any())
        {
            items = filter.Orders.Aggregate(items, (current, order) =>
                order.OrderDirection switch
                {
                    EPageOrder.ASC => current
                        .OrderBy(x => TypeDescriptor
                            .GetProperties(typeof(T))
                            .Find(order.OrderBy ?? "", true)?
                            .GetValue(x))
                        .ToList(),
                    EPageOrder.DESC => current
                        .OrderByDescending(x => TypeDescriptor
                            .GetProperties(typeof(T))
                            .Find(order.OrderBy ?? "", true)?
                            .GetValue(x))
                        .ToList(),
                    _ => current
                });
        }

        if (!filter.TakeAll)
        {
            items = items
                .Skip((filter.Page - 1) * filter.Size)
                .Take(filter.Size)
                .ToList();
        }

        return new Pagination<T>
        {
            Items = items,
            Metadata = new Metadata(totalCount, filter.Page, filter.Size, filter.TakeAll)
        };
    }

    /// <summary>
    /// Paginates an IQueryable data source based on the given filter.
    /// </summary>
    public static Pagination<T> QueryPaginate<T>(PaginationFilter filter, IQueryable<T> query)
    {
        if (filter.Searches != null && filter.Searches.Any())
        {
            query = query
                .AsEnumerable()
                .Where(x =>
                {
                    bool condition = false;
                    foreach (Search search in filter.Searches)
                    {
                        if (search.SearchBy != null)
                        {
                            PropertyDescriptor? property = TypeDescriptor
                                .GetProperties(typeof(T))
                                .Find(search.SearchBy, true);

                            if (property != null)
                            {
                                condition = condition || ((string)(property.GetValue(x) ?? ""))
                                    .Contains(search.SearchValue ?? "", StringComparison.CurrentCultureIgnoreCase);
                            }
                        }
                    }
                    return condition;
                })
                .AsQueryable();
        }

        int totalCount = query.Count();

        if (filter.Orders != null && filter.Orders.Any())
        {
            foreach (Order order in filter.Orders)
            {
                if (order.OrderBy != null)
                {
                    PropertyDescriptor? property = TypeDescriptor
                        .GetProperties(typeof(T))
                        .Find(order.OrderBy, true);

                    if (property != null)
                    {
                        query = order.OrderDirection switch
                        {
                            EPageOrder.ASC => query.AsEnumerable().OrderBy(x => property.GetValue(x)).AsQueryable(),
                            EPageOrder.DESC => query.AsEnumerable().OrderByDescending(x => property.GetValue(x)).AsQueryable(),
                            _ => query
                        };
                    }
                }
            }
        }

        var items = new List<T>();
        if (!filter.TakeAll)
        {
            items = query
                .Skip((filter.Page - 1) * filter.Size)
                .Take(filter.Size)
                .ToList();
        }
        else
        {
            items = query.ToList();
        }

        return new Pagination<T>
        {
            Items = items,
            Metadata = new Metadata(totalCount, filter.Page, filter.Size, filter.TakeAll)
        };
    }
}
