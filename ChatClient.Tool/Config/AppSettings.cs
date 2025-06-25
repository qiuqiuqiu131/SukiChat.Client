using Microsoft.Extensions.Configuration;

namespace ChatClient.Tool.Config;

public class AppSettings
{
    public AddressSettings Address { get; set; } = new();
    public int MaxFrameLength { get; set; }
    public int MaxFieldLength { get; set; }
    public ReconnectSettings Reconnect { get; set; } = new();
    public int ChatMessageCount { get; set; }
    public int OutOfTime { get; set; }
    public LoggingSettings Logging { get; set; } = new();
    public string BaseFolder { get; set; } = string.Empty;
    public bool SIPSorceryDebug { get; set; }

    public string StunServerUrl { get; set; } = string.Empty;
    public string[] IceServers { get; set; } = [];
    public TurnServerSettings TurnServer { get; set; } = new();
    public bool TransportRelay { get; set; }
    public int GatherTime { get; set; }
    public int OutConnectTime { get; set; }
}

public class AddressSettings
{
    [ConfigurationKeyName("IP")] public string Ip { get; set; } = string.Empty;

    [ConfigurationKeyName("Main_Port")] public int MainPort { get; set; }

    [ConfigurationKeyName("Resources_Port")]
    public int ResourcesPort { get; set; }
}

public class ReconnectSettings
{
    [ConfigurationKeyName("ConnectTime")] public int ConnectTime { get; set; }

    [ConfigurationKeyName("ReconnectTime")]
    public int ReconnectTime { get; set; }

    [ConfigurationKeyName("AllIdleTime")] public int AllIdleTime { get; set; }
}

public class LoggingSettings
{
    [ConfigurationKeyName("LogLevel")] public LogLevelSettings LogLevel { get; set; } = new();
}

public class LogLevelSettings
{
    public string Default { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Microsoft { get; set; } = string.Empty;
}

public class TurnServerSettings
{
    public string Url { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Credential { get; set; } = string.Empty;
}