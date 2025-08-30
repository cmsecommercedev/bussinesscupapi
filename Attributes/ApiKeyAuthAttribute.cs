using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace BussinessCupApi.Attributes
{
    // Güncellenmiş attribute constructor ile
    public class ApiKeyAuthAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var validApiKey = config["ApiSettings:ApiKey"];

            if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key is missing"
                };
                return;
            }

            if (!string.Equals(extractedApiKey, validApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 403,
                    Content = "Invalid API Key"
                };
            }
        }
    }

}
