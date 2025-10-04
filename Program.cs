using BussinessCupApi;
using BussinessCupApi.CloudflareManager;
using BussinessCupApi.Data;
using BussinessCupApi.Jobs;
using BussinessCupApi.Managers;
using BussinessCupApi.Models;
using BussinessCupApi.Models.UserPlayerTypes;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


// Scoped ve transient servisler
builder.Services.AddScoped<LeagueManager>();

builder.Services.AddScoped<NotificationManager>();
builder.Services.AddTransient<DbBackupJob>();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;              // Rakam gereksinimi kapalı
    options.Password.RequireLowercase = false;          // Küçük harf gereksinimi kapalı
    options.Password.RequireUppercase = false;          // Büyük harf gereksinimi kapalı
    options.Password.RequireNonAlphanumeric = false;    // Özel karakter gereksinimi kapalı
    options.Password.RequiredLength = 6;                // Minimum 6 karakter
    options.Password.RequiredUniqueChars = 1;           // Farklı karakter sayısı
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Bearer Authentication ekle
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Scoped servisler
builder.Services.AddScoped<SignInManager<User>>();
builder.Services.AddScoped<CustomUserManager>();

// MVC ve Session
builder.Services.AddControllersWithViews();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bussiness Cup Futbol API", Version = "v1" });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. Example: \"X-Api-Key: {your key}\"",
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new List<string>()
        }
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache(); // veya Redis için: AddStackExchangeRedisCache

builder.Services.Configure<CloudflareR2Options>(builder.Configuration.GetSection("CloudflareR2"));
builder.Services.AddSingleton<CloudflareR2Manager>();
builder.Services.AddSingleton<OpenAiManager>();
 
builder.Services.Configure<DatabaseBackupOptions>(builder.Configuration.GetSection("DatabaseBackup"));

// appsettings.json'dan config binding
builder.Services.Configure<CloudflareD1LoggerOptions>(builder.Configuration.GetSection("CloudflareD1Logger"));

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHangfire(x =>
    x.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// HttpClient factory
builder.Services.AddHttpClient();

// EmailServiceManager'ı ekle
builder.Services.AddScoped<EmailServiceManager>();

var sp = builder.Services.BuildServiceProvider();
var options = sp.GetRequiredService<IOptions<CloudflareD1LoggerOptions>>().Value;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new CloudflareD1LoggerProvider(
    sp.GetRequiredService<IHttpClientFactory>(),
    options.ApiUrl, options.BearerToken));

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 100 * 1024 * 1024;
    await next.Invoke();
});

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = new[] { "Admin", "Player", "CityAdmin", "Public", "Captain", "Announcer" };

    foreach (var role in roles)
    {
        var roleExists = await roleManager.RoleExistsAsync(role);
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

//using (var scope = app.Services.CreateScope())
//{
//    var services = scope.ServiceProvider;
//    var context = services.GetRequiredService<ApplicationDbContext>();
//    var userManager = services.GetRequiredService<UserManager<User>>();

//    // Seed methodunu çağır
//    DummyDataSeeder.Seed(context, userManager);
//}


// Developer exception page (sadece development için)
app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();   // Authentication middleware
app.UseAuthorization();    // Authorization middleware

app.UseCors("AllowAll");

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Cache invalidation middleware (isteğe bağlı)
// Burada kalabilir ya da düzenlenebilir
app.UseWhen(context =>
    (context.Request.Path.StartsWithSegments("/admin") ||
     context.Request.Path.StartsWithSegments("/user")) &&
    (HttpMethods.IsPost(context.Request.Method) ||
     HttpMethods.IsPut(context.Request.Method) ||
     HttpMethods.IsDelete(context.Request.Method)),
    appBuilder =>
    {
        appBuilder.Use(async (context, next) =>
        {
            await next();

            var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

            try
            {
                (memoryCache as MemoryCache)?.Clear();

                logger.LogInformation("All cache entries cleared after {Method} request to {Path}",
                context.Request.Method, context.Request.Path);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error clearing cache");
            }
        });
    });

// MVC rotaları ve API
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

// Hangfire joblar
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

    recurringJobManager.AddOrUpdate<DbBackupJob>(
        "nightly-db-backup",
        job => job.RunAsync(),
        Cron.Daily);

    backgroundJobClient.Enqueue<DbBackupJob>(job => job.RunAsync());
}

async Task CreateRoles(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roleNames = { "Admin", "Captain", "Public", "CityAdmin", "Announcer", "Player" };

    foreach (var roleName in roleNames)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            // Rol yoksa oluştur
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

// Uygulama başlarken roller oluşturulsun
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await CreateRoles(services);
}


// Uygulama başlarken roller oluşturulsun
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await CreateRoles(services);

    // Admin user oluşturma ve SuperAdmin rolü atama
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    string adminEmail = "admin@agx-labs.com";
    string adminPassword = "Admin123!"; // Güçlü bir şifre belirleyin
    string superAdminRole = "Admin";

    // SuperAdmin rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync(superAdminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(superAdminRole));
    }

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Firstname = "",
            Lastname = "",
            UserKey = "",
            UserType = UserType.Admin, // Admin olarak ayarla
            UserRole = superAdminRole

        };
        var result = await userManager.CreateAsync(user, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, superAdminRole);
        }
        // Hata yönetimi ekleyebilirsiniz
    }
    else
    {
        // Kullanıcı varsa ama rolü yoksa ekle
        if (!await userManager.IsInRoleAsync(adminUser, superAdminRole))
        {
            await userManager.AddToRoleAsync(adminUser, superAdminRole);
        }
    }
}


app.Run();


// Helper methods for cache version management
static async Task<long> GetCacheVersionAsync(IDistributedCache cache, string key)
{
    var data = await cache.GetAsync(key);
    if (data == null)
        return 0;
    return BitConverter.ToInt64(data, 0);
}

static async Task SetCacheVersionAsync(IDistributedCache cache, string key, long value)
{
    var data = BitConverter.GetBytes(value);
    await cache.SetAsync(key, data, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
    });
}

// MemoryCache için extension method
public static class MemoryCacheExtensions
{
    public static void Clear(this IMemoryCache cache)
    {
        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }
}
