using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HouseKeeper.Components.Features.Accounts.Contracts;
using HouseKeeper.Components.Routing;
using HouseKeeper.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace HouseKeeper.Components.Features.Accounts;

public static class AccountEndpoints
{
    private const int MaxFailedAccessAttempts = 5;
    private static readonly TimeSpan LockoutTimeSpan = TimeSpan.FromMinutes(10);

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(AppRoutes.Register, RegisterAsync);
        endpoints.MapPost(AppRoutes.Login, LoginAsync);
        endpoints.MapPost(AppRoutes.Logout, LogoutAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromForm] RegisterAccountRequest request,
        [FromForm] string? returnUrl,
        MongoDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResults = ValidateModel(request);
        if (validationResults.Count > 0)
        {
            return RedirectWithError(AppRoutes.Register, string.Join(" ", validationResults), returnUrl);
        }

        var username = request.Username.Trim();
        var normalizedUsername = Normalize(username);
        var email = request.Email.Trim();
        var normalizedEmail = Normalize(email);

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return RedirectWithError(AppRoutes.Register, "Passwords do not match.", returnUrl);
        }

        var usernameExists = await dbContext.Users
            .Find(x => x.NormalizedUsername == normalizedUsername)
            .AnyAsync(cancellationToken);
        if (usernameExists)
        {
            return RedirectWithError(AppRoutes.Register, "That username is already taken.", returnUrl);
        }

        var emailExists = await dbContext.Users
            .Find(x => x.NormalizedEmail == normalizedEmail)
            .AnyAsync(cancellationToken);
        if (emailExists)
        {
            return RedirectWithError(AppRoutes.Register, "That email is already in use.", returnUrl);
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("D"),
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = username,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        try
        {
            await dbContext.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return RedirectWithError(AppRoutes.Register, "That username or email is already in use.", returnUrl);
        }

        await SignInUserAsync(httpContext, user);
        return Results.LocalRedirect(AppRoutes.Home);
    }

    private static async Task<IResult> LoginAsync(
        [FromForm] LoginAccountRequest request,
        [FromForm] string? returnUrl,
        MongoDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResults = ValidateModel(request);
        if (validationResults.Count > 0)
        {
            return RedirectWithError(AppRoutes.Login, string.Join(" ", validationResults), returnUrl);
        }

        var normalizedUsername = Normalize(request.Username);
        var user = await dbContext.Users
            .Find(x => x.NormalizedUsername == normalizedUsername)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return RedirectWithError(AppRoutes.Login, "Invalid username or password.", returnUrl);
        }

        if (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            return RedirectWithError(AppRoutes.Login, "Account is temporarily locked after too many failed attempts.", returnUrl);
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult is PasswordVerificationResult.Failed)
        {
            // Track failed attempts and enforce temporary lockout to slow credential stuffing.
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAccessAttempts)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutTimeSpan);
                user.AccessFailedCount = 0;
            }

            await dbContext.Users.ReplaceOneAsync(x => x.Id == user.Id, user, cancellationToken: cancellationToken);
            return RedirectWithError(AppRoutes.Login, "Invalid username or password.", returnUrl);
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await dbContext.Users.ReplaceOneAsync(x => x.Id == user.Id, user, cancellationToken: cancellationToken);

        await SignInUserAsync(httpContext, user);
        return Results.LocalRedirect(NormalizeReturnUrl(returnUrl));
    }

    private static async Task<IResult> LogoutAsync(
        [FromForm] string? returnUrl,
        HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.LocalRedirect(NormalizeReturnUrl(returnUrl, AppRoutes.Login));
    }

    private static async Task SignInUserAsync(HttpContext httpContext, ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static IResult RedirectWithError(string path, string error, string? returnUrl)
    {
        var encodedError = Uri.EscapeDataString(error);
        var encodedReturnUrl = Uri.EscapeDataString(NormalizeReturnUrl(returnUrl));
        return Results.LocalRedirect($"{path}?error={encodedError}&returnUrl={encodedReturnUrl}");
    }

    private static string NormalizeReturnUrl(string? returnUrl, string fallback = AppRoutes.Home)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        return returnUrl.StartsWith('/') ? returnUrl : fallback;
    }

    private static List<string> ValidateModel<T>(T model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model!, serviceProvider: null, items: null);
        Validator.TryValidateObject(model!, validationContext, validationResults, validateAllProperties: true);
        return validationResults.Select(x => x.ErrorMessage ?? "Invalid request.").ToList();
    }
}
