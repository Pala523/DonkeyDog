using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Text;
using DonkeyDog.Models;
using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDB.Bson;

var builder = WebApplication.CreateBuilder(args);

// Configurazione BsonDefaults
BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V2;

// Configura la stringa di connessione MongoDB da una variabile d'ambiente
var mongoDbSettings = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
if (string.IsNullOrEmpty(mongoDbSettings))
{
    throw new InvalidOperationException("La variabile d'ambiente MONGODB_CONNECTION_STRING non è impostata.");
}
var mongoClient = new MongoClient(mongoDbSettings);
var mongoDatabase = mongoClient.GetDatabase("Cluster0"); // Assicurati che questo sia il nome corretto del tuo database

// Configura Identity con MongoDB
var mongoDbIdentityConfiguration = new MongoDbIdentityConfiguration
{
    MongoDbSettings = new MongoDbSettings
    {
        ConnectionString = mongoDbSettings,
        DatabaseName = "Cluster0"
    },
    IdentityOptionsAction = options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    }
};

builder.Services.ConfigureMongoDbIdentity<ApplicationUser, MongoIdentityRole<Guid>, Guid>(mongoDbIdentityConfiguration)
    .AddUserManager<UserManager<ApplicationUser>>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddRoleManager<RoleManager<MongoIdentityRole<Guid>>>()
    .AddDefaultTokenProviders();

// Leggi i valori JWT dalla stringa di connessione
var jwtSettings = builder.Configuration.GetSection("ConnectionStrings:JwtSettings");

var jwtSecret = jwtSettings["Secret"];
var jwtValidIssuer = jwtSettings["ValidIssuer"];
var jwtValidAudience = jwtSettings["ValidAudience"];

if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtValidIssuer) || string.IsNullOrEmpty(jwtValidAudience))
{
    throw new InvalidOperationException("One or more JWT configuration settings are missing.");
}

// Configura l'autenticazione JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = jwtValidAudience,
        ValidIssuer = jwtValidIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

// Configura i servizi MVC
builder.Services.AddControllers();

const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var allowedOrigins = "*";
builder.Services.AddCors(o =>
{
    o.AddPolicy(MyAllowSpecificOrigins, b =>
    {
        if (allowedOrigins.Contains("*"))
        {
            b.AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader();
        }
        else
        {
            b.WithOrigins(allowedOrigins)
             .AllowAnyMethod()
             .AllowAnyHeader();
        }
    });
});

// Configura Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Dnk Management API", Version = "v1" });
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = JwtBearerDefaults.AuthenticationScheme
        });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            new string[]{}
        }
    });
});

// Registrazione del servizio MongoDB
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<ServiziService>();
builder.Services.AddSingleton<FeedbackService>();

builder.Services.AddSingleton(serviceProvider =>
{
    var database = serviceProvider.GetRequiredService<IMongoDatabase>();
    return new GridFSBucket(database);
});

var app = builder.Build();

// Configura il middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
