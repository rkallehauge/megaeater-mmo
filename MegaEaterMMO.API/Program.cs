using MegaEaterMMO.API.Data.DB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using RabbitMQ.Client;
using Neo4j.Driver;
using MegaEaterMMO.API.Service.Security;
using MegaEaterMMO.API.Service.Auth;
using MegaEaterMMO.API.Service.Game;
using MegaEaterMMO.API.Service.Users;



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
builder.Services.AddScoped<ISocialService, SocialService>();

string postgresConnectionString = configuration.GetConnectionString("Postgres");
string redisConnectionString = configuration.GetConnectionString("Redis");
string rabbitMQConnectionString = configuration.GetConnectionString("RabbitMQ");
IConfigurationSection neo4jSection = configuration.GetSection("Neo4j");

// too lazy to do the lazy way of doing this
if(string.IsNullOrEmpty(postgresConnectionString))
{
    throw new Exception("PostgreSQL connection string is not set in configuration.");
}

if(string.IsNullOrEmpty(redisConnectionString))
{
    throw new Exception("Redis connection string is not set in configuration.");
}

if(string.IsNullOrEmpty(rabbitMQConnectionString))
{
    throw new Exception("RabbitMQ connection string is not set in configuration.");
}

if(string.IsNullOrEmpty(neo4jSection.GetValue<string>("Uri")) || string.IsNullOrEmpty(neo4jSection.GetValue<string>("Username")) || string.IsNullOrEmpty(neo4jSection.GetValue<string>("Password")))
{
    throw new Exception("RabbitMQ connection string(s) is not set in configuration.");
}

Console.WriteLine("::CONFIG VARS::");
Console.WriteLine(postgresConnectionString);
Console.WriteLine(redisConnectionString);
Console.WriteLine(rabbitMQConnectionString);


builder.Services.AddDbContext<MegaEaterContext>(options =>
    options.UseNpgsql(postgresConnectionString), ServiceLifetime.Scoped
);


builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString)
);

builder.Services.AddSingleton(sp =>
{
    var factory = new ConnectionFactory()
    {
        Uri = new Uri(configuration.GetConnectionString("RabbitMQ"))
    };

    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IDriver>(sp =>
{
    return GraphDatabase.Driver(
        neo4jSection.GetValue<string>("Uri"),
        AuthTokens.Basic(
           neo4jSection.GetValue<string>("Username"),
           neo4jSection.GetValue<string>("Password")
        )
    );
});


Console.WriteLine("::CONNECTION VARS::");
Console.WriteLine(postgresConnectionString);
Console.WriteLine(redisConnectionString);
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



string frontendURL = configuration["FrontendURL"];

if (string.IsNullOrEmpty(frontendURL))
{
    throw new Exception("Frontend url is not currently set.");
}
Console.WriteLine("frontendurl");
Console.WriteLine(frontendURL);

builder.Services.AddCors(
    options =>
    {
        options.AddPolicy("React", policy =>
        {
            policy.WithOrigins(frontendURL)
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
    }
);


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

if (!isDevelopment)
{
    app.UseHttpsRedirection();
}

app.UseCors("React");

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

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Running Migrations...");
    var db = scope.ServiceProvider.GetService<MegaEaterContext>();
    if (db == null)
        throw new Exception("Database couldn't be instantiated during migration.");

    db.Database.Migrate();
    Console.WriteLine("Migrations done.");
}

app.Run();