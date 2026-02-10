namespace SetupIts.Shared.Helpers;
public static class IdHelper
{
    public static string CreateNewUlid() => Ulid.NewUlid().ToString();
    public static string CreateNewUlid(DateTimeOffset dateTimeOffset) => Ulid.NewUlid(dateTimeOffset).ToString();
    public static string CreateNewGuidV7String() => Guid.CreateVersion7().ToString();
    public static Guid CreateNewGuidV7() => Guid.CreateVersion7();
    public static byte[] CreateNewGuidV7Bytes() => CreateNewGuidV7().ToByteArray();
}
