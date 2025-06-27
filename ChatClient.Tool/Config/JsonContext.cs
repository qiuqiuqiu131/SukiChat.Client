using System.Text.Json.Serialization;

namespace ChatClient.Tool.Config;

[JsonSerializable(typeof(LoginData))]
public partial class LoginDataContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsContext : JsonSerializerContext
{
}