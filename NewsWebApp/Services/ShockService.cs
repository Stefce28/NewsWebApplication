using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NewsWebApp.Shared.Models;
using NewsWebApp.Services.IServices;
using NewsWebApp.ViewModels;

namespace NewsWebApp.Services;

public class ShockService : IShockService
{
    private const decimal MaxShock = 100m;
    private const decimal ShockMultiplier = 2.5m;
    private static readonly TimeSpan OverloadCooldown = TimeSpan.FromMinutes(1);

    private const string CurrentShockKey = "Shock.Current";
    private const string ReadArticlesKey = "Shock.ReadArticles";
    private const string OverloadMessageKey = "Shock.OverloadMessage";
    private const string CooldownEndsAtKey = "Shock.CooldownEndsAt";
    private const string OverloadTriggerKey = "Shock.OverloadTrigger";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShockService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public decimal GetCurrentShock()
    {
        ResetExpiredCooldownIfNeeded();

        var rawShock = Session.GetString(CurrentShockKey);

        if (decimal.TryParse(
                rawShock,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var shock))
        {
            return ClampShock(shock);
        }

        return 0m;
    }

    public ShockStatusViewModel GetShockStatus()
    {
        ResetExpiredCooldownIfNeeded();

        var currentShock = GetCurrentShock();
        var cooldownEndsAt = GetCooldownEndsAt();
        var now = DateTimeOffset.UtcNow;
        var isInCooldown = cooldownEndsAt.HasValue && cooldownEndsAt.Value > now;
        var shouldAnimate = Session.GetInt32(OverloadTriggerKey) == 1;

        if (!isInCooldown)
        {
            Session.Remove(CooldownEndsAtKey);
            Session.Remove(OverloadMessageKey);
        }

        if (shouldAnimate)
        {
            Session.Remove(OverloadTriggerKey);
        }

        return new ShockStatusViewModel
        {
            CurrentShock = currentShock,
            LevelClass = GetLevelClass(currentShock),
            IsInCooldown = isInCooldown,
            CooldownEndsAt = isInCooldown ? cooldownEndsAt : null,
            OverloadMessage = isInCooldown
                ? Session.GetString(OverloadMessageKey)
                : null,
            ShouldAnimateOverload = shouldAnimate && isInCooldown
        };
    }

    public decimal AddShockFromArticle(Article article)
    {
        ResetExpiredCooldownIfNeeded();

        if (HasReadArticle(article.Pid))
        {
            return GetCurrentShock();
        }

        var currentShock = GetCurrentShock();
        var addedShock = Math.Max(0m, article.ShockValue * ShockMultiplier);
        var nextShock = ClampShock(currentShock + addedShock);

        SetCurrentShock(nextShock);
        MarkArticleAsRead(article.Pid);

        if (currentShock < MaxShock && nextShock >= MaxShock)
        {
            StartOverloadCooldown();
        }

        return nextShock;
    }

    public bool HasReadArticle(Guid articleId)
    {
        return GetReadArticleIds().Contains(articleId);
    }

    public void MarkArticleAsRead(Guid articleId)
    {
        var readArticleIds = GetReadArticleIds();
        readArticleIds.Add(articleId);

        Session.SetString(
            ReadArticlesKey,
            JsonSerializer.Serialize(readArticleIds)
        );
    }

    public void ResetShock()
    {
        Session.Remove(CurrentShockKey);
        Session.Remove(ReadArticlesKey);
        Session.Remove(OverloadMessageKey);
        Session.Remove(CooldownEndsAtKey);
        Session.Remove(OverloadTriggerKey);
    }

    private ISession Session =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("Session is not available.");

    private void SetCurrentShock(decimal shock)
    {
        Session.SetString(
            CurrentShockKey,
            ClampShock(shock).ToString(CultureInfo.InvariantCulture)
        );
    }

    private HashSet<Guid> GetReadArticleIds()
    {
        var rawArticleIds = Session.GetString(ReadArticlesKey);

        if (string.IsNullOrWhiteSpace(rawArticleIds))
        {
            return new HashSet<Guid>();
        }

        try
        {
            return JsonSerializer.Deserialize<HashSet<Guid>>(rawArticleIds)
                ?? new HashSet<Guid>();
        }
        catch (JsonException)
        {
            return new HashSet<Guid>();
        }
    }

    private DateTimeOffset? GetCooldownEndsAt()
    {
        var rawCooldownEndsAt = Session.GetString(CooldownEndsAtKey);

        if (long.TryParse(
                rawCooldownEndsAt,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var cooldownEndsAt))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(cooldownEndsAt);
        }

        return null;
    }

    private void ResetExpiredCooldownIfNeeded()
    {
        var cooldownEndsAt = GetCooldownEndsAt();

        if (cooldownEndsAt.HasValue && cooldownEndsAt.Value <= DateTimeOffset.UtcNow)
        {
            ResetShock();
        }
    }

    private void StartOverloadCooldown()
    {
        var cooldownEndsAt = DateTimeOffset.UtcNow.Add(OverloadCooldown);

        Session.SetString(
            CooldownEndsAtKey,
            cooldownEndsAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
        );
        Session.SetString(OverloadMessageKey, ShockOverloadMessages.GetRandom());
        Session.SetInt32(OverloadTriggerKey, 1);
    }

    private static decimal ClampShock(decimal shock)
    {
        return Math.Clamp(shock, 0m, MaxShock);
    }

    private static string GetLevelClass(decimal shock)
    {
        var percentage = Math.Clamp(
            (int)Math.Round(shock, MidpointRounding.AwayFromZero),
            0,
            100
        );

        if (percentage >= 100)
        {
            return "overload";
        }

        if (percentage >= 67)
        {
            return "high";
        }

        if (percentage >= 34)
        {
            return "medium";
        }

        return "low";
    }
}
