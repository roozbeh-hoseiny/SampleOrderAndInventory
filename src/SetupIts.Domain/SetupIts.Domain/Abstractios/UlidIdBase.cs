using SetupIts.Shared.Helpers;

namespace SetupIts.Domain.Abstractios;

public abstract record UlidIdBase
{
    public string Value { get; init; }

    protected UlidIdBase(string val)
    {
        this.Value = val;
    }
    protected UlidIdBase()
    {
        this.Value = IdHelper.CreateNewUlid();
    }
}