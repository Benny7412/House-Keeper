using HouseKeeper.Components.Features.Household.Contracts;
using HouseKeeper.Components.Features.Household.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Components.Services;

namespace HouseKeeper.Components.Features.Household.State;

public sealed class HouseholdState
{
    private readonly HouseholdService _service;
    private readonly ILogger<HouseholdState> _logger;

    public bool HasHousehold { get; private set; }
    public string HouseholdName { get; private set; } = "Household";
    public bool IsCurrentUserAdmin { get; private set; }
    public bool IsChoreMutationsLocked { get; private set; } = true;
    public IReadOnlyList<HouseholdMemberVm> Members { get; private set; } = [];
    public IReadOnlyList<HouseholdInviteVm> PendingInvites { get; private set; } = [];
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    public event Action? Changed;

    public HouseholdState(HouseholdService service, ILogger<HouseholdState> logger)
    {
        _service = service;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        Error = null;
        NotifyChanged();

        try
        {
            var snapshot = await _service.GetSnapshotAsync(cancellationToken);
            if (snapshot is null)
            {
                HasHousehold = false;
                HouseholdName = "Household";
                IsCurrentUserAdmin = false;
                IsChoreMutationsLocked = true;
                Members = [];
                PendingInvites = await _service.GetPendingInvitesAsync(cancellationToken);
                return;
            }

            HasHousehold = true;
            HouseholdName = snapshot.HouseholdName;
            IsCurrentUserAdmin = snapshot.IsCurrentUserAdmin;
            IsChoreMutationsLocked = snapshot.IsChoreMutationsLocked;
            Members = snapshot.Members;
            PendingInvites = [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load household context.");
            HasHousehold = false;
            IsChoreMutationsLocked = true;
            PendingInvites = [];
            Error = UserFacingError.FromException(ex, "Unable to load household details right now.");
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    public async Task CreateHouseholdAsync(CreateHouseholdRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.CreateHouseholdAsync(request, cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create household.");
            Error = UserFacingError.FromException(ex, "Unable to create the household.");
            NotifyChanged();
        }
    }

    public async Task InviteMemberAsync(InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.InviteMemberAsync(request, cancellationToken);
            Members = await _service.GetMembersAsync(cancellationToken);
            NotifyChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite household member.");
            Error = UserFacingError.FromException(ex, "Unable to send the invite.");
            NotifyChanged();
        }
    }

    public async Task AcceptInviteAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.AcceptInviteAsync(inviteId, cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept household invite.");
            Error = UserFacingError.FromException(ex, "Unable to accept the invite.");
            NotifyChanged();
        }
    }

    public async Task LeaveHouseholdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.LeaveHouseholdAsync(cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave household.");
            Error = UserFacingError.FromException(ex, "Unable to leave the household.");
            NotifyChanged();
        }
    }

    public async Task SetChoreLockAsync(bool isLocked, CancellationToken cancellationToken = default)
    {
        try
        {
            Error = null;
            await _service.SetChoreLockAsync(isLocked, cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chore lock setting.");
            Error = UserFacingError.FromException(ex, "Unable to update chore lock setting.");
            NotifyChanged();
        }
    }

    public void Clear()
    {
        HasHousehold = false;
        HouseholdName = "Household";
        IsCurrentUserAdmin = false;
        IsChoreMutationsLocked = true;
        Members = [];
        PendingInvites = [];
        IsLoading = false;
        Error = null;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
