using Microsoft.AspNetCore.Http;
using System;

public static class HttpRequestExtensions
{
    public static bool IsAjax(this HttpRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
} 