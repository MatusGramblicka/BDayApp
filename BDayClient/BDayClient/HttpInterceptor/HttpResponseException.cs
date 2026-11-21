namespace BDayClient.HttpInterceptor;

// No [Serializable] / serialization constructor: formatter-based serialization is obsolete (SYSLIB0051).
internal class HttpResponseException : Exception
{
    public HttpResponseException()
    {
    }

    public HttpResponseException(string message) : base(message)
    {
    }

    public HttpResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
