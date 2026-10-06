namespace Orders.API.Extensions;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null && context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
