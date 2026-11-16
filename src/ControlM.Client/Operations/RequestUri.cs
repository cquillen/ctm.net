namespace ControlM.Client.Operations;

internal static class RequestUri
{
    public static Uri Of(Uri endpoint, string path) => new($"{endpoint.AbsoluteUri.TrimEnd('/')}/{path.TrimStart('/')}");
}
