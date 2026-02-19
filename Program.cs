using HouseKeeper.Components;
using HouseKeeper.Components.Features.Accounts;
using HouseKeeper.Components.Features.Chores.Services;
using HouseKeeper.Components.Features.Chores.State;
using HouseKeeper.Components.Features.Expenses.Services;
using HouseKeeper.Components.Features.Expenses.State;
using HouseKeeper.Components.Features.Household.Services;
using HouseKeeper.Components.Features.Household.State;
using HouseKeeper.Components.Features.TownHall.Services;
using HouseKeeper.Components.Features.TownHall.State;
using HouseKeeper.Components.Routing;
using HouseKeeper.Components.Services;
using HouseKeeper.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = options.ConnectionString;
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "MongoDB connection string is missing. Set MongoDb:ConnectionString in appsettings or MONGODB_CONNECTION_STRING.");
    }

    return new MongoClient(connectionString);
});
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<MongoIndexInitializer>();
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = AppRoutes.Login;
        options.AccessDeniedPath = AppRoutes.Login;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ChoresService>();
builder.Services.AddScoped<ChoresState>();
builder.Services.AddScoped<ExpensesService>();
builder.Services.AddScoped<ExpensesState>();
builder.Services.AddScoped<HouseholdService>();
builder.Services.AddScoped<HouseholdState>();
builder.Services.AddScoped<TownHallService>();
builder.Services.AddScoped<TownHallState>();
builder.Services.AddScoped<ItemsService>();
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<HouseholdContextAccessor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var indexInitializer = scope.ServiceProvider.GetRequiredService<MongoIndexInitializer>();
    await indexInitializer.EnsureIndexesAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
