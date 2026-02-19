using HouseKeeper.Components.Features.TownHall.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;

namespace HouseKeeper.Components.Features.TownHall.State;

public sealed class TownHallState
{
    private readonly TownHallService _service;
    private readonly ILogger<TownHallState> _logger;

    public IReadOnlyList<HouseholdActivityVm> Items { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    public event Action? Changed;

    public TownHallState(TownHallService service, ILogger<TownHallState> logger)
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
            Items = await _service.GetRecentAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load town hall activity.");
            Error = UserFacingError.FromException(ex, "Unable to load activity right now.");
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
