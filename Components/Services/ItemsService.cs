using HouseKeeper.Components.Features.Chores.Services;
using HouseKeeper.Components.Features.Expenses.Services;
using HouseKeeper.Components.Models;

namespace HouseKeeper.Components.Services;

public sealed class ItemsService
{
    private readonly ChoresService _choresService;
    private readonly ExpensesService _expensesService;

    public ItemsService(ChoresService choresService, ExpensesService expensesService)
    {
        _choresService = choresService;
        _expensesService = expensesService;
    }

    public Task<IReadOnlyList<ChoreVm>> GetChoresAsync(CancellationToken cancellationToken = default)
    {
        return _choresService.GetAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ExpenseVm>> GetExpensesAsync(CancellationToken cancellationToken = default)
    {
        return _expensesService.GetAsync(cancellationToken);
    }
}
