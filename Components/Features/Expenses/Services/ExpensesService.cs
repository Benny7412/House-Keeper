using HouseKeeper.Components.Features.Expenses.Contracts;
using HouseKeeper.Components.Services;
using HouseKeeper.Components.Models;
using HouseKeeper.Data;
using MongoDB.Driver;

namespace HouseKeeper.Components.Features.Expenses.Services;

public sealed class ExpensesService
{
    private readonly MongoDbContext _dbContext;
    private readonly HouseholdContextAccessor _householdContextAccessor;

    public ExpensesService(MongoDbContext dbContext, HouseholdContextAccessor householdContextAccessor)
    {
        _dbContext = dbContext;
        _householdContextAccessor = householdContextAccessor;
    }

    public async Task<IReadOnlyList<ExpenseVm>> GetAsync(CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);

        var expenses = await _dbContext.Expenses
            .Find(x => x.HouseholdId == context.HouseholdId)
            .ToListAsync(cancellationToken);

        return expenses
            .OrderBy(x => x.IsSettled)
            .ThenByDescending(x => x.IsUrgent)
            .ThenByDescending(x => x.CreatedAt)
            .Select(MapToVm)
            .ToList();
    }

    public async Task<ExpenseVm> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);

        var item = new ExpenseItem
        {
            HouseholdId = context.HouseholdId,
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            IsUrgent = request.IsUrgent,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = context.UserId
        };

        await _dbContext.Expenses.InsertOneAsync(item, cancellationToken: cancellationToken);
        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = $"{context.DisplayName} added an expense: {item.Title}.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);

        return MapToVm(item);
    }

    public async Task<ExpenseVm?> ToggleSettledAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var context = await _householdContextAccessor.GetRequiredAsync(cancellationToken);

        var item = await _dbContext.Expenses
            .Find(x => x.Id == id.ToString("D") && x.HouseholdId == context.HouseholdId)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.IsSettled = !item.IsSettled;
        await _dbContext.Expenses.ReplaceOneAsync(
            x => x.Id == item.Id && x.HouseholdId == context.HouseholdId,
            item,
            cancellationToken: cancellationToken);

        await _dbContext.HouseholdActivities.InsertOneAsync(new HouseholdActivity
        {
            HouseholdId = context.HouseholdId,
            Message = item.IsSettled
                ? $"{context.DisplayName} settled an expense: {item.Title}."
                : $"{context.DisplayName} marked an expense as unsettled: {item.Title}.",
            OccurredAt = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);

        return MapToVm(item);
    }

    private static ExpenseVm MapToVm(ExpenseItem item)
    {
        if (!Guid.TryParse(item.Id, out var parsedId))
        {
            throw new InvalidOperationException("Expense id is malformed in database.");
        }

        return new ExpenseVm
        {
            Id = parsedId,
            Title = item.Title,
            Amount = item.Amount,
            Currency = item.Currency,
            IsSettled = item.IsSettled,
            IsUrgent = item.IsUrgent,
            CreatedAt = item.CreatedAt
        };
    }
}
