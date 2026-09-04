namespace NewsWebApp.ViewModels;

public class ShockStatusViewModel
{
    public decimal CurrentShock { get; init; }
    public string LevelClass { get; init; } = "low";
    public bool IsInCooldown { get; init; }
    public DateTimeOffset? CooldownEndsAt { get; init; }
    public string? OverloadMessage { get; init; }
    public bool ShouldAnimateOverload { get; init; }

    public int CurrentPercentage =>
        Math.Clamp(
            (int)Math.Round(CurrentShock, MidpointRounding.AwayFromZero),
            0,
            100
        );

    public long? CooldownEndsAtUnixMilliseconds =>
        CooldownEndsAt?.ToUnixTimeMilliseconds();
}
