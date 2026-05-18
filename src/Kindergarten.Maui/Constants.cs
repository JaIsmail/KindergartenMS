namespace Kindergarten.Maui;

public static class Constants
{
    public const string ApiBaseUrl = "https://kms-api-staging-kg01.azurewebsites.net";
    public const string SignalRHubUrl = ApiBaseUrl + "/hubs/trip";

    // Storage keys
    public const string TokenKey    = "auth_token";
    public const string UserIdKey   = "user_id";
    public const string UserRoleKey = "user_role";
    public const string UserNameKey = "user_name";
    public const string LangKey     = "app_lang";
}
