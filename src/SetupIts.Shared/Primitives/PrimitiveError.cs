namespace SetupIts.Shared.Primitives;

public sealed class PrimitiveError
{
    #region " Properties "
    public string Code { get; }
    public string Message { get; }
    public bool Internal { get; }
    #endregion

    private PrimitiveError(string code, string message, bool isInternal)
    {
        this.Code = code;
        this.Message = message;
        this.Internal = isInternal;
    }
    public static PrimitiveError Create(string code, string message) => CreateCore(code, message, false);
    public static PrimitiveError CreateInternal(string code, string message) => CreateCore(code, message, true);

    private static PrimitiveError CreateCore(string code, string message, bool isInternal) => new PrimitiveError(code, message, isInternal);
}
