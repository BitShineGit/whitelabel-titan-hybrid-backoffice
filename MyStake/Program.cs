
using MyStake.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyStake.Services.Implementations;
using MyStake.Services.Interfaces;
using Serilog;
using MyStake.Models;
using MyStake.Services;
using Titan.Repository.Database;
using Titan.Repository;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<WhitelabelContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpClient();


builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IWhitelabelDBStorage, WhitelabelDBStorage>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddSingleton<IGoldenGateXAPITokenService, GoldenGateXAPITokenService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IBettingLogService, BettingLogService>();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "TITANBETUserAuth";

    options.LoginPath = "/Account/Login";

    options.AccessDeniedPath = "/Account/Login";
});
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

builder.Services.AddScoped<Global.Logging.ILogger, Global.Logging.Logger>(
    serviceProvider => new Global.Logging.Logger(
        logger: logger
    )
);
builder.Services.AddScoped<ISettingService, SettingService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleSeeder.SeedRoles(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseWebSockets();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
// Go to 404 error page//
app.UseStatusCodePagesWithReExecute("/Home/NotFound");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();