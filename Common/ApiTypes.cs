namespace Common
{
    public readonly record struct UserInfo(string Email, string Id, UserDataInfo Info);
    public readonly record struct UserDataInfo(string Username);
}