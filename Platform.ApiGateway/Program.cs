using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (context, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next();
    sw.Stop();

    // Безопасное получение фичи YARP без выброса исключения
    var proxyFeature = context.Features.Get<Yarp.ReverseProxy.Model.IReverseProxyFeature>();
    var route = proxyFeature?.Route.Config.RouteId;

    app.Logger.LogInformation("{Method} {Path} -> route {Route} : {StatusCode} in {Elapsed} ms",
        context.Request.Method, context.Request.Path, route ?? "unknown",
        context.Response.StatusCode, sw.ElapsedMilliseconds);
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();