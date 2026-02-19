using HouseKeeper.Components.Features.Chores.Contracts;
using HouseKeeper.Components.Features.Chores.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;

namespace HouseKeeper.Components.Features.Chores.State;

public sealed class ChoresState
{
    private readonly ChoresService _service;
    private readonly ILogger<ChoresState> _logger;

    public IReadOnlyList<ChoreVm> Items { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    public event Action? Changed;

    public ChoresState(ChoresService service, ILogger<ChoresState> logger)
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
            _logger.LogError(ex, "Failed to load chores.");
            Error = UserFacingError.FromException(ex, "Unable to load chores right now.");
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    public async Task AddAsync(CreateChoreRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.CreateAsync(request, cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a chore.");
            Error = UserFacingError.FromException(ex, "Unable to add the chore.");
            NotifyChanged();
        }
    }

    public async Task ToggleCompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.ToggleCompleteAsync(id, cancellationToken);
            await RefreshAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chore completion.");
            Error = UserFacingError.FromException(ex, "Unable to update chore status.");
            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
