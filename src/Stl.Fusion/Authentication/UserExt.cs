namespace Stl.Fusion.Authentication;

public static class UserExt
{
    public static User OrGuest(this User? user, string? name = null)
        => user ?? User.NewGuest(name);
}
