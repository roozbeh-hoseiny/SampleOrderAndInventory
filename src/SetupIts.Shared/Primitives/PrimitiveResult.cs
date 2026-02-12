namespace SetupIts.Shared.Primitives;
public sealed class PrimitiveResult : IPrimitiveResult
{
    #region " Fields "
    private bool _isSuccess = false;
    private PrimitiveError[] _errors = [];
    #endregion

    #region " Properties "
    public bool IsSuccess => this._isSuccess;
    public bool IsFailure => !this.IsSuccess;
    public PrimitiveError[] Errors => this._errors;
    public PrimitiveError Error => this.Errors.Any()
        ? this.Errors[0]
        : throw new InvalidOperationException("empty error.");
    #endregion

    private PrimitiveResult(bool isSuccess, PrimitiveError[] errors)
    {
        this._isSuccess = isSuccess;
        this._errors = errors;
    }

    public static PrimitiveResult Success() => new(true, []);
    public static PrimitiveResult Failure(PrimitiveError[] errors) => new(false, errors);
    public static PrimitiveResult Failure(PrimitiveError error) => new(false, [error]);
    public static PrimitiveResult Failure(string errorCode, string errorMessage) => new(false, [PrimitiveError.Create(errorCode, errorMessage)]);
    public static PrimitiveResult InternalFailure(string errorCode, string errorMessage) => new(false, [PrimitiveError.CreateInternal(errorCode, errorMessage)]);

    public static PrimitiveResult<TValue> Success<TValue>(TValue value) => new(value, true, Array.Empty<PrimitiveError>());
    public static PrimitiveResult<TValue> Failure<TValue>(PrimitiveError[] errors) => new(default, false, errors);
    public static PrimitiveResult<TValue> Failure<TValue>(PrimitiveError error) => new(default, false, [error]);
    public static PrimitiveResult<TValue> Failure<TValue>(string errorCode, string errorMessage) => new(default, false, [PrimitiveError.Create(errorCode, errorMessage)]);
    public static PrimitiveResult<TValue> InternalFailure<TValue>(string errorCode, string errorMessage) => new(default, false, [PrimitiveError.CreateInternal(errorCode, errorMessage)]);
    public static PrimitiveResult<TValue> InternalFailure<TValue>(PrimitiveError error) => new(default, false, [PrimitiveError.CreateInternal(error.Code, error.Message)]);
    public static PrimitiveResult<TValue> Failure<TValue>(PrimitiveResult result) => new(default, false, result.Errors);
    public static PrimitiveResult<TValue> Failure<TValue>(PrimitiveResult<TValue> result) => new(default, false, result.Errors);
}
