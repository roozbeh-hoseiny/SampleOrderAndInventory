namespace SetupIts.Shared.Primitives;
public sealed class PrimitiveResult<TValue> : IPrimitiveResult<TValue>
{
    #region " Fields "
    private TValue? _value { get; set; }
    private bool _isSuccess = false;
    private PrimitiveError[] _errors = Array.Empty<PrimitiveError>();
    #endregion

    #region " Properties "
    public TValue Value => this.IsSuccess
       ? this._value!
       : throw new InvalidOperationException("the value of failure result can not be accessed.");
    public bool IsSuccess => this._isSuccess;
    public bool IsFailure => !this.IsSuccess;
    public PrimitiveError[] Errors => this._errors;
    public PrimitiveError Error => this.Errors.Any()
        ? this.Errors[0]
        : throw new InvalidOperationException("empty error.");
    #endregion
    internal PrimitiveResult(TValue? value, bool isSuccess, PrimitiveError[] errors)
    {
        this._value = value;
        this._isSuccess = isSuccess;
        this._errors = errors;
    }

    public static PrimitiveResult<TValue> Failure(PrimitiveError[] errors) => PrimitiveResult.Failure<TValue>(errors);
    public static PrimitiveResult<TValue> CreateSuccess(TValue value) => PrimitiveResult.Success(value);

    public static PrimitiveResult From(PrimitiveResult<TValue> src) =>
        src.IsSuccess
        ? PrimitiveResult.Success()
        : PrimitiveResult.Failure(src.Errors);

    public static TValue? GetValue(PrimitiveResult<TValue> src) =>
        src.IsSuccess
        ? src.Value
        : default;

    public static implicit operator PrimitiveResult<TValue>(TValue value) => PrimitiveResult.Success(value);
}
