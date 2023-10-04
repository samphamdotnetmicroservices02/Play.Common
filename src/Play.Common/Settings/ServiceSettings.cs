namespace Play.Common.Settings;

public class ServiceSettings
{
    public string ServiceName { get; init; }

    public string Authority { get; init; }
    public string InternalHostAuthority { get; init; } = string.Empty;

    public string InternalAuthority => $"http://{InternalHostAuthority}";

    public string MessageBroker { get; init; }
    public string KeyVaultName { get; init; }
    public string IsKubernetesLocal { get; init; } = string.Empty;
}