using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Whitelabel_backoffice.Data;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Implementations;
using Whitelabel_backoffice.Services.Interfaces;
using Whitelabel_backoffice.BackgroundJobs;
using Whitelabel_backoffice.Services;

var builder = WebApplication.CreateBuilder(args);


var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");



builder.Services.AddHttpClient();


builder.Services.AddDbContext<WhitelabelContext>(
    options =>
        options.UseSqlServer(connectionString)
);


builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(connectionString)
);



builder.Services.AddScoped<IWhitelabelDBStorage, WhitelabelDBStorage>();


builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings")
);



builder.Services.AddSingleton<IGoldenGateXAPITokenService,
    GoldenGateXAPITokenService>();


builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddScoped<IClientService, ClientService>();



builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddScoped<ISettingService, SettingService>();


builder.Services.AddSingleton<WebSocketConnectionManager>();

builder.Services.AddSingleton<WebSocketHandler>();

// =================================================
// IJob
// =================================================
builder.Services.AddScoped<CancelExpiredSelfDepositsJob>();
builder.Services.AddHostedService<JobRunner>();

// =====================================================
// IDENTITY
// =====================================================

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireNonAlphanumeric = false;

})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();




// =====================================================
// IMPORTANT
// Identity cookie configuration MUST BE AFTER Identity
// =====================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/Login";

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Account/Login");

        return Task.CompletedTask;
    };
});





builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});



builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();






// =====================================================
// SERILOG
// =====================================================

var logger =
    new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();


builder.Logging.ClearProviders();

builder.Logging.AddSerilog(logger);





builder.Services.AddScoped<Global.Logging.ILogger,
    Global.Logging.Logger>(
    serviceProvider =>
        new Global.Logging.Logger(
            logger: logger
        )
);





var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}



app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseWebSockets();


app.UseRouting();


app.UseAuthentication();

app.UseAuthorization();

app.Map("/ws", async context =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();

    if (context.WebSockets.IsWebSocketRequest)
    {
        var socket = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleAsync(socket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}"
);



app.Run();