using System.Diagnostics;
using WebApplication1.Services;

var webHost = WebApplication.CreateBuilder(args);

webHost.Services.AddControllersWithViews();
webHost.Services.AddSingleton<ITaskService, InMemoryTaskService>();

var pipeline = webHost.Build();

if (!pipeline.Environment.IsDevelopment())
{
    pipeline.UseExceptionHandler("/Home/Error");
    pipeline.UseHsts();
}

pipeline.UseHttpsRedirection();

pipeline.Use(async (ctx, proceed) =>
{
    const string protectedPrefix = "/tasks/api";
    const string headerName = "X-Api-Key";
    const string validKey = "secret123";

    if (ctx.Request.Path.StartsWithSegments(protectedPrefix))
    {
        var providedKey = ctx.Request.Headers[headerName].FirstOrDefault();

        if (!string.Equals(providedKey, validKey, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                "{\"error\":\"Unauthorized: invalid or missing X-Api-Key\"}");
            return;
        }
    }

    await proceed(ctx);
});

pipeline.UseRouting();
pipeline.UseAuthorization();

pipeline.Use(async (ctx, proceed) =>
{
    var responseTimer = Stopwatch.StartNew();

    ctx.Response.OnStarting(() =>
    {
        responseTimer.Stop();
        ctx.Response.Headers["X-Response-Time-ms"] =
            responseTimer.ElapsedMilliseconds.ToString();
        return Task.CompletedTask;
    });

    await proceed(ctx);
});

pipeline.Use(async (ctx, proceed) =>
{
    ctx.Response.Headers.Append("X-App-Name", "TaskBoard");

    var requestLogger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    requestLogger.LogInformation(
        ">>> {Verb} {Url}",
        ctx.Request.Method,
        ctx.Request.Path);

    await proceed(ctx);

    requestLogger.LogInformation(
        "<<< {HttpStatus} for {Url}",
        ctx.Response.StatusCode,
        ctx.Request.Path);
});

pipeline.Use(async (ctx, proceed) =>
{
    if (ctx.Request.Path == "/health")
    {
        ctx.Response.StatusCode = StatusCodes.Status200OK;
        await ctx.Response.WriteAsync("healthy");
        return;
    }

    await proceed(ctx);
});

pipeline.MapStaticAssets();

pipeline.MapControllerRoute(
        name: "default-route",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

pipeline.Run();