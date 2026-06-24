namespace LegalOfficeApp
{
    /// <summary>
    /// Holds the currently logged-in user for the session.
    /// Access from any form via SessionManager.Current
    /// </summary>
    public static class SessionManager
    {
        public static AppUser? Current { get; private set; }

        public static bool IsAdmin => Current?.Role == "Admin";

        public static void Login(AppUser user)
        {
            Current = user;
            // Log the login event
            DatabaseService.Instance.InsertLog(user.FullName, "Login", "Logged into the system");
        }

        public static void Logout()
        {
            if (Current != null)
                DatabaseService.Instance.InsertLog(Current.FullName, "Logout", "Logged out of the system");
            Current = null;
        }
    }
}
