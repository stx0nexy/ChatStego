using System.Reflection;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PixChat.API.HubConfig;
using PixChat.Application;
using PixChat.Application.Config;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Services;
using PixChat.Core.Interfaces;
using PixChat.Core.Interfaces.Repositories;
using PixChat.Infrastructure.Database;
using PixChat.Infrastructure.ExternalServices;
using PixChat.Infrastructure.Filters;
using PixChat.Infrastructure.Repositories;

var configuration = GetConfiguration();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});

builder.Services.AddApplicationServices();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);


builder.Services.AddControllers(options =>
    {
        options.Filters.Add(typeof(HttpGlobalExceptionFilter));
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.Configure<ChatConfig>(configuration);
builder.Services.Configure<ImageConfig>(builder.Configuration.GetSection("ImageConfig"));

builder.Services.AddSingleton<IPasswordHasher<UserDto>, PasswordHasher<UserDto>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddTransient<IParticipantService, ParticipantService>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Services.AddTransient<IContactService, ContactService>();
builder.Services.AddTransient<IKeyService, KeyService>();
builder.Services.AddTransient<ISteganographyService, SteganographyService>(sp =>
{
    var imageConfigOptions = sp.GetRequiredService<IOptions<ImageConfig>>();
    var logger = sp.GetRequiredService<ILogger<SteganographyService>>();
    return new SteganographyService( logger, imageConfigOptions);
});
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IOfflineMessageService, OfflineMessageService>();
builder.Services.AddTransient<IOneTimeMessageService, OneTimeMessageService>();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddTransient<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>(provider =>
{
    var imageConfigOptions = provider.GetRequiredService<IOptions<ImageConfig>>();
    var imageDirectory = imageConfigOptions.Value.ImageFolderPath;

    var dbContextWrapper = provider.GetRequiredService<IDbContextWrapper<ApplicationDbContext>>();
    var logger = provider.GetRequiredService<ILogger<UserRepository>>();

    return new UserRepository(dbContextWrapper, logger, imageDirectory);
});

builder.Services.AddTransient<IChatRepository, ChatRepository>();
builder.Services.AddTransient<IChatParticipantRepository, ChatParticipantRepository>();
builder.Services.AddTransient<IOneTimeMessageRepository, OneTimeMessageRepository>();
builder.Services.AddTransient<IOfflineMessageRepository, OfflineMessageRepository>();
builder.Services.AddTransient<IUserKeyRepository, UserKeyRepository>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITwoFactorService, TwoFactorService>();

builder.Services.AddDbContextFactory<ApplicationDbContext>(opts => opts.UseNpgsql(configuration["ConnectionString"]));
builder.Services.AddScoped<IDbContextWrapper<ApplicationDbContext>, DbContextWrapper<ApplicationDbContext>>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PixChat API",
        Version = "v1",
        Description = "The PixChat HTTP API"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Description = "Jwt Auth header using the bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Proxy", "assets", "images")),
    RequestPath = "/assets/images"
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

app.UseRouting();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapDefaultControllerRoute();
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

app.Run();

IConfiguration GetConfiguration()
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    return builder.Build();
}
