using Microsoft.Extensions.Options;
using SetupIts.Hosting;

namespace SetuIts.Tests.Integration;
internal sealed class TestSetupOptions : IOptionsMonitor<SetupItsGlobalOptions>
{
    private SetupItsGlobalOptions _currentValue;

    public TestSetupOptions(string connectionString)
    {
        this._currentValue = new SetupItsGlobalOptions { ConnectionString = connectionString };
    }

    public SetupItsGlobalOptions CurrentValue => this._currentValue;

    public SetupItsGlobalOptions Get(string name) => this._currentValue;

    public IDisposable OnChange(Action<SetupItsGlobalOptions, string> listener)
        => null;
}
