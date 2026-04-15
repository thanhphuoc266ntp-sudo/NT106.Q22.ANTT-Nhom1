namespace RemoteMate
{
    public static class UserSession
    {
        public static string? FullName { get; set; }
        public static string? Email { get; set; }
        public static string? Username { get; set; }

        public static void Clear()
        {
            FullName = null;
            Email = null;
            Username = null;
        }
    }
}