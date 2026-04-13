using MegaEaterMMO.API.Data.DB;
using MegaEaterMMO.API.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables() 
    .Build();

builder.Services.AddControllers();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<IUserService, UserService>(); // todo : remove unnecessary interface
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();

string connectionString = configuration.GetConnectionString("Default");
string redisConfig = configuration.GetValue<string>("Redis:Configuration");

if(string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Database connection string is not configured.");
}
if (string.IsNullOrEmpty(redisConfig)){
    throw new Exception("Redis configuration is not configured.");
}


builder.Services.AddDbContext<MegaEaterContext>(options =>
    options.UseNpgsql(connectionString), ServiceLifetime.Scoped
);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfig)
);
Console.WriteLine("::CONNECTION VARS::");
Console.WriteLine(connectionString);
Console.WriteLine(redisConfig);
Console.WriteLine("::CONNECTION VARS::");

builder.Services.AddEndpointsApiExplorer();

// Swagger auth documentation and migration documentation for .NET 10.0 
builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    // from https://learn.microsoft.com/en-us/answers/questions/5646974/swagger-addsecurityrequirement-fails-after-migrati?page=1#answer-12420539
    c.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
});
bool isDevelopment = builder.Environment.IsDevelopment();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
    options.RequireHttpsMetadata = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = (isDevelopment) ? false : true,
        ValidateAudience = (isDevelopment) ? false : true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["JWT:Issuer"],
        ValidAudience = configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SigningKey"]))
    };
    options.Authority = configuration["JWT:Authority"]; // IdentityServer URL
    options.Audience = configuration["JWT:Audience"]; // API resource name
        options.Events = new JwtBearerEvents 
        {
            OnTokenValidated = async context => // JWT Blacklisting, Versioning and possibly future sec implementations
            {
                string? username = context.Principal?.Identity?.Name; // or use a specific claim if you have one for username

                string? jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                string? tokenVersion = context.Principal?.FindFirst("tokenVersion")?.Value;

                if(string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(tokenVersion))
                {
                    context.Fail("Token is invalid, you need to re-authenticate.");
                }

                var redis = context.HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                var db = context.HttpContext.RequestServices.GetRequiredService<MegaEaterContext>();

                var cache = redis.GetDatabase();

                var isBlacklisted = cache.StringGet($"blacklist:{jti}");

                if (!string.IsNullOrEmpty(isBlacklisted))
                {
                    context.Fail("Token is blacklisted, you need to re-authenticate.");
                }


                // Check token version for server-side invalidation
                string? currentTokenVersion;
                // check cache before hitting db
                if (cache.KeyExists($"tokenVersion:{username}"))
                {
                    currentTokenVersion = cache.StringGet($"tokenVersion:{username}");
                }
                else
                {
                    currentTokenVersion = db.Users.Where(u => u.Username == username).Select(u => u.TokenVersion).FirstOrDefault().ToString();
                }
                if(string.IsNullOrEmpty(currentTokenVersion) || currentTokenVersion != tokenVersion)
                {
                    context.Fail("Token is outdated, you need to re-authenticate.");
                }
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.MapControllers();
app.Run();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Running Migrations...");
    var db = scope.ServiceProvider.GetService<MegaEaterContext>();
    if (db == null)
        throw new Exception("Database couldn't be instantiated during migration.");

    db.Database.Migrate();
    Console.WriteLine("Migrations done.");
}