# OAuth Implementation Plan
## Google & GitHub Authentication for Codewrinkles

> **Purpose**: Complete implementation guide for OAuth 2.0 authentication. Point to this file in a new session to start implementation.

**Status**: Ready for Implementation
**Estimated Time**: 18-26 hours

---

## Table of Contents
1. [Overview & Flow](#overview--flow)
2. [Database Schema (EF Core)](#database-schema-ef-core)
3. [Backend Implementation](#backend-implementation)
4. [Frontend Implementation](#frontend-implementation)
5. [Third-Party Setup](#third-party-setup)
6. [Implementation Steps](#implementation-steps)

---

## Overview & Flow

### Integration Approach
- OAuth users share same `Identity` + `Profile` entities as password users
- `PasswordHash` is nullable (OAuth-only accounts have no password)
- Email uniqueness enforced across all auth methods
- Supports account linking (existing email + new OAuth provider)

### Secure OAuth Flow

```
┌─────────┐                                           ┌──────────────┐
│ Browser │                                           │ OAuth Provider│
└────┬────┘                                           └───────┬──────┘
     │                                                        │
     │ 1. User clicks "Sign in with Google"                 │
     │                                                        │
     │ 2. POST https://localhost:7280/api/identity/oauth/google/initiate │
     ├──────────────────────────────────────────────────────►│
     │    Backend generates state, stores in cache           │
     │    Returns authorization URL                          │
     │                                                        │
     │ 3. Frontend redirects to Google consent               │
     ├──────────────────────────────────────────────────────►│
     │    User grants permission                             │
     │                                                        │
┌────┴────┐                                           ┌───────┴──────┐
│ Backend │◄─── 4. Google redirects with code ────────┤ OAuth Provider│
│   API   │    GET https://localhost:7280/api/identity/oauth/google/callback │
└────┬────┘    ?code=...&state=...                    └──────────────┘
     │
     │ 5. Backend validates state (CSRF check)
     │ 6. Backend exchanges code for tokens
     │ 7. Backend creates Identity + Profile
     │ 8. Backend generates JWT tokens
     │
     │ 9. Backend redirects to frontend:
     │    /auth/success?access_token=JWT&refresh_token=JWT
     │
┌────┴────┐
│ Browser │
└────┬────┘
     │ 10. Frontend extracts tokens from URL
     │ 11. Frontend stores tokens, clears URL
     │ 12. Redirect to /onboarding or /pulse
```

**Key Security:** Authorization code NEVER exposed to browser. Backend handles all OAuth logic.

---

## Database Schema (EF Core)

### New Entity: ExternalLogin

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Identity/ExternalLogin.cs

namespace Codewrinkles.Domain.Identity;

public sealed class ExternalLogin
{
    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public OAuthProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; }
    public string ProviderEmail { get; private set; }
    public string? ProviderDisplayName { get; private set; }
    public string? ProviderAvatarUrl { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Identity Identity { get; private set; }

#pragma warning disable CS8618
    private ExternalLogin() { }
#pragma warning restore CS8618

    public static ExternalLogin Create(
        Guid identityId,
        OAuthProvider provider,
        string providerUserId,
        string providerEmail,
        string? providerDisplayName = null,
        string? providerAvatarUrl = null,
        string? accessToken = null,
        string? refreshToken = null,
        DateTime? tokenExpiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEmail);

        return new ExternalLogin
        {
            IdentityId = identityId,
            Provider = provider,
            ProviderUserId = providerUserId,
            ProviderEmail = providerEmail.Trim().ToLowerInvariant(),
            ProviderDisplayName = providerDisplayName?.Trim(),
            ProviderAvatarUrl = providerAvatarUrl?.Trim(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = tokenExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTokens(string accessToken, string? refreshToken, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        AccessToken = accessToken;
        if (!string.IsNullOrWhiteSpace(refreshToken)) RefreshToken = refreshToken;
        TokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### New Enum: OAuthProvider

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Identity/OAuthProvider.cs

namespace Codewrinkles.Domain.Identity;

public enum OAuthProvider
{
    Google = 1,
    GitHub = 2
}
```

### Update Identity Entity

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Identity/Identity.cs

// Make PasswordHash nullable
public string? PasswordHash { get; private set; }  // Changed from string to string?

// Add navigation property
public ICollection<ExternalLogin> ExternalLogins { get; private set; } = [];

// Add factory method for OAuth users
public static Identity CreateFromOAuth(string email, bool isEmailVerified)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(email);

    return new Identity
    {
        Email = email.Trim(),
        EmailNormalized = email.Trim().ToLowerInvariant(),
        PasswordHash = null,  // No password for OAuth-only accounts
        IsEmailVerified = isEmailVerified,
        IsActive = true,
        FailedLoginAttempts = 0,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Role = UserRole.User
    };
}

// Add helper methods
public bool HasOAuthProvider(OAuthProvider provider)
    => ExternalLogins.Any(el => el.Provider == provider);

public bool CanLoginWithPassword()
    => !string.IsNullOrWhiteSpace(PasswordHash);

public bool IsOAuthOnly()
    => string.IsNullOrWhiteSpace(PasswordHash) && ExternalLogins.Any();
```

### EF Core Configuration

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Identity/ExternalLoginConfiguration.cs

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins", "identity");
        builder.HasKey(el => el.Id);
        builder.Property(el => el.Id).ValueGeneratedOnAdd();

        builder.Property(el => el.Provider).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(el => el.ProviderUserId).IsRequired().HasMaxLength(255);
        builder.Property(el => el.ProviderEmail).IsRequired().HasMaxLength(255);
        builder.Property(el => el.ProviderDisplayName).HasMaxLength(255);
        builder.Property(el => el.ProviderAvatarUrl).HasMaxLength(500);
        builder.Property(el => el.CreatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");
        builder.Property(el => el.UpdatedAt).IsRequired().HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(el => new { el.Provider, el.ProviderUserId })
            .IsUnique()
            .HasDatabaseName("UQ_ExternalLogins_Provider_UserId");

        builder.HasIndex(el => el.IdentityId).HasDatabaseName("IX_ExternalLogins_IdentityId");

        builder.HasOne(el => el.Identity)
            .WithMany(i => i.ExternalLogins)
            .HasForeignKey(el => el.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Update ApplicationDbContext

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs

public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations
    modelBuilder.ApplyConfiguration(new ExternalLoginConfiguration());
}
```

### Generate Migration

```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add AddExternalLoginsTable --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

---

## Backend Implementation

### 1. IOAuthService Interface

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IOAuthService.cs

namespace Codewrinkles.Application.Common.Interfaces;

public interface IOAuthService
{
    Task<OAuthTokenResponse> ExchangeCodeForTokenAsync(
        OAuthProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthUserInfo> GetUserInfoAsync(
        OAuthProvider provider,
        string accessToken,
        CancellationToken cancellationToken = default);

    OAuthAuthorizationUrl GenerateAuthorizationUrl(
        OAuthProvider provider,
        string redirectUri,
        string state);
}

public sealed record OAuthTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? Scope);

public sealed record OAuthUserInfo(
    string ProviderUserId,
    string Email,
    bool EmailVerified,
    string? Name,
    string? GivenName,
    string? FamilyName,
    string? Picture,
    string? Locale);

public sealed record OAuthAuthorizationUrl(
    string Url,
    string State);
```

### 2. OAuthService Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/OAuthService.cs
// Full implementation with Google + GitHub token exchange and user info retrieval
// See oauth-plan.md section "OAuthService Implementation" for complete code
```

**Note**: Full OAuthService code is ~300 lines. Implements:
- Google/GitHub token exchange
- Google/GitHub user info fetching
- Authorization URL generation
- PKCE helper methods

### 3. IExternalLoginRepository

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IExternalLoginRepository.cs

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> FindByProviderAndUserIdAsync(
        OAuthProvider provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task<List<ExternalLogin>> FindByIdentityIdAsync(
        Guid identityId,
        CancellationToken cancellationToken = default);

    void Add(ExternalLogin externalLogin);
    void Remove(ExternalLogin externalLogin);
}
```

### 4. ExternalLoginRepository Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/ExternalLoginRepository.cs

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly ApplicationDbContext _context;

    public ExternalLoginRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExternalLogin?> FindByProviderAndUserIdAsync(
        OAuthProvider provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(
                el => el.Provider == provider && el.ProviderUserId == providerUserId,
                cancellationToken);
    }

    public async Task<List<ExternalLogin>> FindByIdentityIdAsync(
        Guid identityId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ExternalLogins
            .AsNoTracking()
            .Where(el => el.IdentityId == identityId)
            .ToListAsync(cancellationToken);
    }

    public void Add(ExternalLogin externalLogin)
    {
        _context.ExternalLogins.Add(externalLogin);
    }

    public void Remove(ExternalLogin externalLogin)
    {
        _context.ExternalLogins.Remove(externalLogin);
    }
}
```

### 5. Update IUnitOfWork

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IUnitOfWork.cs

public interface IUnitOfWork : IAsyncDisposable
{
    IIdentityRepository Identities { get; }
    IProfileRepository Profiles { get; }
    IExternalLoginRepository ExternalLogins { get; }  // NEW
    // ... other repositories

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken);
}
```

### 6. Update UnitOfWork Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/UnitOfWork.cs

private readonly Lazy<IExternalLoginRepository> _externalLogins;

public IExternalLoginRepository ExternalLogins => _externalLogins.Value;

// In constructor:
_externalLogins = new Lazy<IExternalLoginRepository>(() => new ExternalLoginRepository(_context));
```

### 7. CompleteOAuthCallback Command & Handler

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Users/CompleteOAuthCallback.cs

public sealed record CompleteOAuthCallbackCommand(
    OAuthProvider Provider,
    string Code,
    string State,
    string RedirectUri
) : ICommand<CompleteOAuthCallbackResult>;

public sealed record CompleteOAuthCallbackResult(
    Guid IdentityId,
    Guid ProfileId,
    string Email,
    string Name,
    string? Handle,
    string? Bio,
    string? AvatarUrl,
    UserRole Role,
    string AccessToken,
    string RefreshToken,
    bool IsNewUser
);

public sealed class CompleteOAuthCallbackCommandHandler
    : ICommandHandler<CompleteOAuthCallbackCommand, CompleteOAuthCallbackResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOAuthService _oAuthService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public CompleteOAuthCallbackCommandHandler(
        IUnitOfWork unitOfWork,
        IOAuthService oAuthService,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _unitOfWork = unitOfWork;
        _oAuthService = oAuthService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<CompleteOAuthCallbackResult> HandleAsync(
        CompleteOAuthCallbackCommand command,
        CancellationToken cancellationToken)
    {
        // Exchange code for tokens
        var tokenResponse = await _oAuthService.ExchangeCodeForTokenAsync(
            command.Provider,
            command.Code,
            command.RedirectUri,
            cancellationToken);

        // Fetch user info
        var userInfo = await _oAuthService.GetUserInfoAsync(
            command.Provider,
            tokenResponse.AccessToken,
            cancellationToken);

        // Check existing external login
        var existingExternalLogin = await _unitOfWork.ExternalLogins
            .FindByProviderAndUserIdAsync(
                command.Provider,
                userInfo.ProviderUserId,
                cancellationToken);

        if (existingExternalLogin is not null)
        {
            return await HandleReturningUserAsync(existingExternalLogin, tokenResponse, cancellationToken);
        }

        // Check existing identity by email
        var identityByEmail = await _unitOfWork.Identities.FindByEmailAsync(
            userInfo.Email,
            cancellationToken);

        if (identityByEmail is not null)
        {
            return await HandleAccountLinkingAsync(
                identityByEmail,
                command.Provider,
                userInfo,
                tokenResponse,
                cancellationToken);
        }

        // New user registration
        return await HandleNewUserRegistrationAsync(
            command.Provider,
            userInfo,
            tokenResponse,
            cancellationToken);
    }

    private async Task<CompleteOAuthCallbackResult> HandleReturningUserAsync(
        ExternalLogin externalLogin,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        var identity = (await _unitOfWork.Identities.FindByIdAsync(
            externalLogin.IdentityId,
            cancellationToken))!;

        var profile = (await _unitOfWork.Profiles.FindByIdentityIdAsync(
            externalLogin.IdentityId,
            cancellationToken))!;

        externalLogin.UpdateTokens(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

        identity.RecordSuccessfulLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken(identity);

        return new CompleteOAuthCallbackResult(
            identity.Id,
            profile.Id,
            identity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            identity.Role,
            accessToken,
            refreshToken,
            IsNewUser: false);
    }

    private async Task<CompleteOAuthCallbackResult> HandleAccountLinkingAsync(
        Identity existingIdentity,
        OAuthProvider provider,
        OAuthUserInfo userInfo,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        var externalLogin = ExternalLogin.Create(
            existingIdentity.Id,
            provider,
            userInfo.ProviderUserId,
            userInfo.Email,
            userInfo.Name,
            userInfo.Picture,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

        _unitOfWork.ExternalLogins.Add(externalLogin);

        if (userInfo.EmailVerified)
        {
            existingIdentity.MarkEmailAsVerified();
        }

        existingIdentity.RecordSuccessfulLogin();

        var profile = (await _unitOfWork.Profiles.FindByIdentityIdAsync(
            existingIdentity.Id,
            cancellationToken))!;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(existingIdentity, profile);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken(existingIdentity);

        return new CompleteOAuthCallbackResult(
            existingIdentity.Id,
            profile.Id,
            existingIdentity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            existingIdentity.Role,
            accessToken,
            refreshToken,
            IsNewUser: false);
    }

    private async Task<CompleteOAuthCallbackResult> HandleNewUserRegistrationAsync(
        OAuthProvider provider,
        OAuthUserInfo userInfo,
        OAuthTokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        Identity identity;
        Profile profile;

        await using var transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            identity = Identity.CreateFromOAuth(userInfo.Email, userInfo.EmailVerified);
            _unitOfWork.Identities.Register(identity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var handle = await GenerateUniqueHandleAsync(
                userInfo.Name ?? userInfo.Email.Split('@')[0],
                cancellationToken);

            profile = Profile.Create(
                identity.Id,
                userInfo.Name ?? userInfo.Email.Split('@')[0],
                handle,
                bio: null,
                avatarUrl: userInfo.Picture);

            _unitOfWork.Profiles.Create(profile);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var externalLogin = ExternalLogin.Create(
                identity.Id,
                provider,
                userInfo.ProviderUserId,
                userInfo.Email,
                userInfo.Name,
                userInfo.Picture,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken,
                DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn));

            _unitOfWork.ExternalLogins.Add(externalLogin);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(identity, profile);
        var refreshToken = JwtTokenGenerator.GenerateRefreshToken(identity);

        return new CompleteOAuthCallbackResult(
            identity.Id,
            profile.Id,
            identity.Email,
            profile.Name,
            profile.Handle,
            profile.Bio,
            profile.AvatarUrl,
            identity.Role,
            accessToken,
            refreshToken,
            IsNewUser: true);
    }

    private async Task<string> GenerateUniqueHandleAsync(
        string baseName,
        CancellationToken cancellationToken)
    {
        var baseHandle = new string(baseName
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .Replace(" ", "_")
            .ToLowerInvariant();

        if (baseHandle.Length < 3)
        {
            baseHandle = $"user_{baseHandle}";
        }

        var handleExists = await _unitOfWork.Profiles.ExistsByHandleAsync(
            baseHandle,
            cancellationToken);

        if (!handleExists)
        {
            return baseHandle;
        }

        for (var i = 1; i <= 999; i++)
        {
            var candidate = $"{baseHandle}{i}";
            var exists = await _unitOfWork.Profiles.ExistsByHandleAsync(
                candidate,
                cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"{baseHandle}_{DateTime.UtcNow.Ticks}";
    }
}
```

### 8. Validator

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Users/CompleteOAuthCallbackValidator.cs

public sealed class CompleteOAuthCallbackValidator : IValidator<CompleteOAuthCallbackCommand>
{
    private List<ValidationError> _errors = null!;

    public Task<ValidationResult> ValidateAsync(
        CompleteOAuthCallbackCommand request,
        CancellationToken cancellationToken)
    {
        _errors = [];

        ValidateProvider(request.Provider);
        ValidateCode(request.Code);
        ValidateState(request.State);
        ValidateRedirectUri(request.RedirectUri);

        if (_errors.Count > 0)
        {
            return Task.FromResult(ValidationResult.Failure(_errors));
        }

        return Task.FromResult(ValidationResult.Success());
    }

    private void ValidateProvider(OAuthProvider provider)
    {
        if (!Enum.IsDefined(typeof(OAuthProvider), provider))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.Provider),
                "Invalid OAuth provider"));
        }
    }

    private void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.Code),
                "Authorization code is required"));
        }
    }

    private void ValidateState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.State),
                "State parameter is required"));
        }
    }

    private void ValidateRedirectUri(string redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.RedirectUri),
                "Redirect URI is required"));
            return;
        }

        if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
        {
            _errors.Add(new ValidationError(
                nameof(CompleteOAuthCallbackCommand.RedirectUri),
                "Redirect URI must be a valid absolute URL"));
        }
    }
}
```

### 9. API Endpoints

```csharp
// Location: apps/backend/src/Codewrinkles.API/Modules/Identity/IdentityEndpoints.cs

// Initiate OAuth flow
app.MapPost("/api/identity/oauth/{provider}/initiate",
    async (
        string provider,
        [FromBody] InitiateOAuthRequest request,
        IOAuthService oauthService,
        IDistributedCache cache,
        CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<OAuthProvider>(provider, true, out var oauthProvider))
    {
        return Results.BadRequest(new { error = "Invalid OAuth provider" });
    }

    var state = OAuthService.GenerateState();
    var cacheKey = $"oauth_state_{state}";
    var cacheValue = new OAuthStateData
    {
        Provider = oauthProvider,
        CreatedAt = DateTime.UtcNow,
        RedirectUri = request.RedirectUri
    };

    await cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(cacheValue),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        },
        cancellationToken);

    var backendCallbackUri = $"{request.BaseUrl}/api/identity/oauth/{provider.ToLowerInvariant()}/callback";
    var authUrl = oauthService.GenerateAuthorizationUrl(oauthProvider, backendCallbackUri, state);

    return Results.Ok(new { authorizationUrl = authUrl.Url });
})
.WithName("InitiateOAuthFlow")
.WithTags("Identity");

// Handle OAuth callback (Google/GitHub redirects here)
app.MapGet("/api/identity/oauth/{provider}/callback",
    async (
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        IKommand kommand,
        IDistributedCache cache,
        CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<OAuthProvider>(provider, true, out var oauthProvider))
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Invalid+provider");
    }

    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message={Uri.EscapeDataString(error)}");
    }

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Missing+code+or+state");
    }

    var cacheKey = $"oauth_state_{state}";
    var storedStateJson = await cache.GetStringAsync(cacheKey, cancellationToken);

    if (storedStateJson is null)
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Invalid+state");
    }

    var storedState = JsonSerializer.Deserialize<OAuthStateData>(storedStateJson);

    if (storedState is null || storedState.Provider != oauthProvider)
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message=Provider+mismatch");
    }

    await cache.RemoveAsync(cacheKey, cancellationToken);

    var redirectUri = $"{GetBackendUrl()}/api/identity/oauth/{provider.ToLowerInvariant()}/callback";

    var command = new CompleteOAuthCallbackCommand(oauthProvider, code, state, redirectUri);

    try
    {
        var result = await kommand.ExecuteAsync(command, cancellationToken);

        var frontendSuccessUrl = $"{storedState.RedirectUri}" +
            $"?access_token={Uri.EscapeDataString(result.AccessToken)}" +
            $"&refresh_token={Uri.EscapeDataString(result.RefreshToken)}" +
            $"&is_new_user={result.IsNewUser}";

        return Results.Redirect(frontendSuccessUrl);
    }
    catch (Exception ex)
    {
        return Results.Redirect($"{GetFrontendUrl()}/auth/error?message={Uri.EscapeDataString(ex.Message)}");
    }
})
.WithName("HandleOAuthCallback")
.WithTags("Identity");

public sealed record InitiateOAuthRequest(string BaseUrl, string RedirectUri);

internal sealed record OAuthStateData
{
    public required OAuthProvider Provider { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string RedirectUri { get; init; }
}

static string GetBackendUrl() =>
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
        ? "http://localhost:5000"
        : "https://api.codewrinkles.com";

static string GetFrontendUrl() =>
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
        ? "http://localhost:5173"
        : "https://codewrinkles.com";
```

### 10. Dependency Injection

```csharp
// Location: apps/backend/src/Codewrinkles.API/Program.cs

// Register distributed cache for state storage
builder.Services.AddDistributedMemoryCache();

// Register HttpClient for OAuthService
builder.Services.AddHttpClient<IOAuthService, OAuthService>();
```

### 11. User Secrets Configuration

```bash
cd apps/backend/src/Codewrinkles.API

dotnet user-secrets set "OAuth:Google:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "OAuth:Google:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
dotnet user-secrets set "OAuth:GitHub:ClientId" "YOUR_GITHUB_CLIENT_ID"
dotnet user-secrets set "OAuth:GitHub:ClientSecret" "YOUR_GITHUB_CLIENT_SECRET"
```

---

## Frontend Implementation

### 1. Types

```typescript
// Location: apps/frontend/src/types.ts

export type OAuthProvider = "Google" | "GitHub";

export interface InitiateOAuthRequest {
  baseUrl: string;
  redirectUri: string;
}

export interface InitiateOAuthResponse {
  authorizationUrl: string;
}
```

### 2. OAuth API Service

```typescript
// Location: apps/frontend/src/services/oauthApi.ts

import type { OAuthProvider, InitiateOAuthRequest, InitiateOAuthResponse } from "../types";
import { apiRequest } from "../utils/api";

export const oauthApi = {
  async initiateOAuth(
    provider: OAuthProvider,
    baseUrl: string,
    redirectUri: string
  ): Promise<InitiateOAuthResponse> {
    const response = await apiRequest<InitiateOAuthResponse>(
      `identity/oauth/${provider.toLowerCase()}/initiate`,
      {
        method: "POST",
        body: JSON.stringify({ baseUrl, redirectUri } as InitiateOAuthRequest),
      }
    );
    return response;
  },
};
```

### 3. Update Social Buttons

```typescript
// Location: apps/frontend/src/features/auth/SocialSignInButtons.tsx

import { useState } from "react";
import type { OAuthProvider } from "../../types";
import { oauthApi } from "../../services/oauthApi";

export function SocialSignInButtons(): JSX.Element {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleOAuthClick = async (provider: OAuthProvider): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);

      const baseUrl = window.location.origin.replace('5173', '5000');
      const frontendRedirectUri = `${window.location.origin}/auth/success`;

      const { authorizationUrl } = await oauthApi.initiateOAuth(
        provider,
        baseUrl,
        frontendRedirectUri
      );

      window.location.href = authorizationUrl;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to initiate OAuth");
      setIsLoading(false);
    }
  };

  return (
    <>
      <div className="mt-6 flex items-center gap-2 text-[11px] text-text-tertiary">
        <span className="h-px flex-1 bg-border-deep" />
        <span>or continue with</span>
        <span className="h-px flex-1 bg-border-deep" />
      </div>

      {error && (
        <div className="mt-4 rounded-lg bg-red-500/10 border border-red-500/20 px-3 py-2 text-xs text-red-400">
          {error}
        </div>
      )}

      <div className="mt-4 grid grid-cols-2 gap-3">
        <button
          type="button"
          onClick={() => handleOAuthClick("Google")}
          disabled={isLoading}
          className="flex items-center justify-center gap-2 rounded-xl border border-border bg-surface-card2 px-4 py-2.5 text-sm text-text-secondary hover:border-brand-soft/50 hover:bg-surface-page transition-colors disabled:cursor-not-allowed disabled:opacity-50"
        >
          <span>Google</span>
        </button>
        <button
          type="button"
          onClick={() => handleOAuthClick("GitHub")}
          disabled={isLoading}
          className="flex items-center justify-center gap-2 rounded-xl border border-border bg-surface-card2 px-4 py-2.5 text-sm text-text-secondary hover:border-brand-soft/50 hover:bg-surface-page transition-colors disabled:cursor-not-allowed disabled:opacity-50"
        >
          <span>GitHub</span>
        </button>
      </div>
    </>
  );
}
```

### 4. OAuth Success Page

```typescript
// Location: apps/frontend/src/features/auth/OAuthSuccessPage.tsx

import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

export function OAuthSuccessPage(): JSX.Element {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { completeOAuthLogin } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleSuccess = async (): Promise<void> => {
      try {
        const accessToken = searchParams.get("access_token");
        const refreshToken = searchParams.get("refresh_token");
        const isNewUser = searchParams.get("is_new_user") === "true";

        if (!accessToken || !refreshToken) {
          setError("Missing authentication tokens");
          setTimeout(() => navigate("/login"), 3000);
          return;
        }

        await completeOAuthLogin(accessToken, refreshToken);

        window.history.replaceState({}, document.title, "/auth/success");

        if (isNewUser) {
          navigate("/onboarding");
        } else {
          navigate("/pulse");
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to complete authentication");
        setTimeout(() => navigate("/login"), 3000);
      }
    };

    handleSuccess();
  }, [searchParams, navigate, completeOAuthLogin]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-center">
          <div className="mb-4 text-red-400">{error}</div>
          <div className="text-sm text-text-secondary">Redirecting to login...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page">
      <div className="text-center">
        <div className="mb-4 text-text-primary">Completing sign-in...</div>
        <div className="text-sm text-text-secondary">Please wait...</div>
      </div>
    </div>
  );
}
```

### 5. OAuth Error Page

```typescript
// Location: apps/frontend/src/features/auth/OAuthErrorPage.tsx

import { useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";

export function OAuthErrorPage(): JSX.Element {
  const [searchParams] = useSearchParams();
  const message = searchParams.get("message") || "An error occurred during authentication";

  useEffect(() => {
    window.history.replaceState({}, document.title, "/auth/error");
  }, []);

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page">
      <div className="max-w-md text-center">
        <div className="mb-6 text-4xl">⚠️</div>
        <h1 className="mb-4 text-xl font-semibold text-text-primary">
          Authentication Failed
        </h1>
        <p className="mb-6 text-sm text-text-secondary">{message}</p>
        <Link
          to="/login"
          className="inline-flex items-center justify-center rounded-full bg-brand text-black px-6 py-2 text-sm font-medium hover:bg-brand-soft transition-colors"
        >
          Back to Login
        </Link>
      </div>
    </div>
  );
}
```

### 6. Update useAuth Hook

```typescript
// Location: apps/frontend/src/hooks/useAuth.tsx

// Add to AuthContext
const completeOAuthLogin = async (
  accessToken: string,
  refreshToken: string
): Promise<void> => {
  try {
    setIsLoading(true);
    setAuthTokens(accessToken, refreshToken);

    const decoded = jwtDecode<JwtPayload>(accessToken);
    const user: User = {
      identityId: decoded.sub!,
      profileId: decoded.profileId,
      email: decoded.email!,
      name: decoded.name!,
      handle: decoded.handle || null,
      bio: null,
      avatarUrl: null,
      location: null,
      websiteUrl: null,
      role: decoded.role,
    };

    setUser(user);
    localStorage.setItem(config.auth.userKey, JSON.stringify(user));
  } finally {
    setIsLoading(false);
  }
};

// Add to context value
return (
  <AuthContext.Provider
    value={{
      user,
      isAuthenticated,
      isLoading,
      register,
      login,
      logout,
      updateProfile,
      updateAvatar,
      changePassword,
      completeOAuthLogin,
    }}
  >
    {children}
  </AuthContext.Provider>
);
```

### 7. Update Routing

```typescript
// Location: apps/frontend/src/App.tsx

import { OAuthSuccessPage } from "./features/auth/OAuthSuccessPage";
import { OAuthErrorPage } from "./features/auth/OAuthErrorPage";

// Add routes
<Route path="/auth/success" element={<OAuthSuccessPage />} />
<Route path="/auth/error" element={<OAuthErrorPage />} />
```

---

## Third-Party Setup

### Google OAuth

1. Go to https://console.cloud.google.com/
2. Create new project (or select existing)
3. Enable Google+ API
4. Create OAuth 2.0 credentials (Web application)
5. Add authorized redirect URIs:
   - `http://localhost:5000/api/identity/oauth/google/callback` (dev)
   - `https://api.codewrinkles.com/api/identity/oauth/google/callback` (prod)
6. Copy Client ID and Client Secret to user secrets

### GitHub OAuth

1. Go to https://github.com/settings/developers
2. Click "New OAuth App"
3. Fill in:
   - Application name: Codewrinkles
   - Homepage URL: `http://localhost:5173` (dev) or `https://codewrinkles.com` (prod)
   - Authorization callback URL:
     - `http://localhost:5000/api/identity/oauth/github/callback` (dev)
     - `https://api.codewrinkles.com/api/identity/oauth/github/callback` (prod)
4. Generate client secret
5. Copy Client ID and Client Secret to user secrets

---

## Implementation Steps

### Phase 1: Database & Domain (2-3 hours)
1. Create `OAuthProvider` enum
2. Create `ExternalLogin` entity
3. Update `Identity` entity (nullable PasswordHash, OAuth methods)
4. Create EF Core configuration
5. Update ApplicationDbContext
6. Generate migration: `dotnet ef migrations add AddExternalLoginsTable`
7. Apply migration: `dotnet ef database update`

### Phase 2: Infrastructure (4-5 hours)
1. Create `IOAuthService` interface in Application/Common/Interfaces
2. Implement `OAuthService` in Infrastructure/Services
3. Create `IExternalLoginRepository` interface
4. Implement `ExternalLoginRepository`
5. Update `IUnitOfWork` interface
6. Update `UnitOfWork` implementation
7. Register services in DI

### Phase 3: Application Layer (3-4 hours)
1. Create `CompleteOAuthCallbackCommand`
2. Implement `CompleteOAuthCallbackValidator`
3. Implement `CompleteOAuthCallbackCommandHandler`
4. Test handler scenarios

### Phase 4: API Endpoints (2-3 hours)
1. Add POST `/api/identity/oauth/{provider}/initiate`
2. Add GET `/api/identity/oauth/{provider}/callback`
3. Configure distributed cache
4. Test endpoints with Postman

### Phase 5: Third-Party (1 hour)
1. Create Google OAuth app
2. Create GitHub OAuth app
3. Store credentials in user secrets

### Phase 6: Frontend (3-4 hours)
1. Update types and API service
2. Update `SocialSignInButtons`
3. Create `OAuthSuccessPage`
4. Create `OAuthErrorPage`
5. Update `useAuth` hook
6. Add routing

### Phase 7: Testing (3-4 hours)
1. End-to-end OAuth flow testing
2. Error scenario testing
3. Security testing

**Total: 18-26 hours**

---

**Ready to implement. Point to this file in a new session to start.**
