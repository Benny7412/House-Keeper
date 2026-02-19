namespace HouseKeeper.Components.Services;

public static class UserFacingError
{
    // preserve domain messages when available while guaranteeing a safe fallback for UI 
    public static string FromException(Exception exception, string fallbackMessage)
    {
        if (exception is OperationCanceledException)
        {
            return "The request was canceled. Please try again.";
        }

        return string.IsNullOrWhiteSpace(exception.Message)
            ? fallbackMessage
            : exception.Message.Trim();
    }
}
