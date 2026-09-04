namespace NewsWebApp.Services;

internal static class ShockOverloadMessages
{
    private static readonly string[] Messages =
    {
        "You've consumed too much internet. Go look at a tree.",
        "Maximum shock achieved. The headlines won.",
        "Congratulations. You have officially been clickbaited.",
        "Your daily outrage allowance has been exceeded.",
        "BREAKING: You desperately need to cool down.",
        "The news has won. Please return to reality."
    };

    public static string GetRandom()
    {
        return Messages[Random.Shared.Next(Messages.Length)];
    }
}
