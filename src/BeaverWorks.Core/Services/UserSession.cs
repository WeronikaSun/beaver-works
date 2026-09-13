namespace BeaverWorks.Core.Services;

/// <summary>
/// Holds the currently logged-in username for the lifetime of the running
/// process only. There is no persistence — relaunching the app always
/// requires logging in again.
/// </summary>
public sealed class UserSession
{
    public string? CurrentUsername { get; private set; }

    public bool IsLoggedIn => CurrentUsername is not null;

    public void SignIn(string username)
    {
        CurrentUsername = username;
    }

    public void SignOut()
    {
        CurrentUsername = null;
    }
}
