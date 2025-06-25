using System.Text.Json.Serialization;

namespace ChatClient.Tool.Config;

[JsonSerializable(typeof(ThemeStyle))]
public partial class ThemeStyleContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(LoginData))]
public partial class LoginDataContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsContext : JsonSerializerContext
{
}