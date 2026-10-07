namespace Notifications.API.Extensions;

public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        if (context?.Request.Headers.TryGetValue("X-Correlation-Id", out var id) == true)
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", id.ToString());
        return base.SendAsync(request, cancellationToken);
    }
}
