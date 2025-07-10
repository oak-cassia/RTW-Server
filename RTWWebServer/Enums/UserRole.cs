namespace RTWWebServer.Enums;

public enum UserRole
{
    Guest = 0,
    Normal = 1,
    Admin = 2
}

public static class UserRoleExtensions
{
    public static string ToRoleString(this UserRole role)
    {
        return role.ToString().ToLowerInvariant();
    }

    public static UserRole FromRoleString(string roleString)
    {
        return Enum.TryParse<UserRole>(roleString, true, out var result)
            ? result
            : UserRole.Guest;
    }
}