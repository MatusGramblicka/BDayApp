using Blazored.Toast.Services;

namespace BDayClient.Http;

public static class ToastServiceExtensions
{
    /// <summary>Shows the message of a failed API call, unless <see cref="ApiErrorHandler"/> has already told the user.</summary>
    public static void ShowApiError(this IToastService toastService, ApiException exception)
    {
        if (!exception.Handled)
            toastService.ShowError(exception.Message);
    }
}
