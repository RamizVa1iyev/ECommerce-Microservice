using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Sample;

public sealed class ModifiedSwaggerUiMiddleware
{
    private readonly SwaggerUIMiddleware _baseMiddleware;
    private readonly SwaggerUIOptions _options;

    private readonly List<UrlDescriptor> _urls;

    public ModifiedSwaggerUiMiddleware(
        RequestDelegate next,
        IWebHostEnvironment hostingEnv,
        ILoggerFactory loggerFactory,
        SwaggerUIOptions options)
    {
        _urls = new List<UrlDescriptor>();
        options.ConfigObject.Urls = _urls;

        _options = options;
        _baseMiddleware = new SwaggerUIMiddleware(next, hostingEnv, loggerFactory, options);
    }

    private async Task ReloadUrls()
    {
        _urls.Clear();
        _urls.Add(new UrlDescriptor
        {
            Url = "http://localhost:5005/swagger/v1/swagger.json",
            Name = "Service Name"
        });
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var httpMethod = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value!;

        if (httpMethod == "GET" && Regex.IsMatch(path, $"^/{Regex.Escape(_options.RoutePrefix)}/?index.html$", RegexOptions.IgnoreCase))
        {
            await ReloadUrls();
        }

        await _baseMiddleware.Invoke(httpContext);
    }
}

public static class SwaggerUiModifiedMiddlewareExtensions
{
    public static IApplicationBuilder UseModifiedSwaggerUi(this IApplicationBuilder app, SwaggerUIOptions options)
    {
        return app.UseMiddleware<ModifiedSwaggerUiMiddleware>(options);
    }

    public static IApplicationBuilder UseModifiedSwaggerUi(this IApplicationBuilder app, Action<SwaggerUIOptions> configure)
    {
        var options = new SwaggerUIOptions();
        configure(options);
        return UseModifiedSwaggerUi(app, options);
    }
}