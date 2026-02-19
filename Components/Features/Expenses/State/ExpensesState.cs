using HouseKeeper.Components.Features.Expenses.Contracts;
using HouseKeeper.Components.Features.Expenses.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;

namespace HouseKeeper.Components.Features.Expenses.State;

public sealed class ExpensesState
{
    private readonly ExpensesService _service;
    private readonly ILogger<ExpensesState> _logger;

    public IReadOnlyList<ExpenseVm> Items { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    public event Action? Changed;

    public ExpensesState(ExpensesService service, ILogger<ExpensesState> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        Error = null;
        NotifyChanged();

        try
        {
            Items = await _service.GetAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load expenses.");
            Error = UserFacingError.FromException(ex, "Unable to load expenses right now.");
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    public async Task AddAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.CreateAsync(request, cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add an expense.");
            Error = UserFacingError.FromException(ex, "Unable to add the expense.");
            NotifyChanged();
        }
    }

    public async Task ToggleSettledAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.ToggleSettledAsync(id, cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update expense settlement status.");
            Error = UserFacingError.FromException(ex, "Unable to update expense status.");
            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
