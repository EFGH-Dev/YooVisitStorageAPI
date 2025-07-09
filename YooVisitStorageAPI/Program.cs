using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using YooVisitStorageAPI.Data;
using YooVisitStorageAPI.Interfaces;
using YooVisitStorageAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Section Services ---

// On récupère la chaîne de connexion (fournie par docker-compose.yml)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// On enregistre le DbContext UNE SEULE FOIS, et correctement.
builder.Services.AddDbContext<StorageDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// On enregistre notre service de stockage de fichiers
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Configuration pour accepter les gros fichiers
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50 MB
});

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// Configuration CORS (si nécessaire, sinon peut être enlevé)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "_myAllowSpecificOrigins",
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

var app = builder.Build();

// --- Section Pipeline de Requêtes ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Middleware de débogage simple
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"--> StorageAPI Request: {context.Request.Method} {context.Request.Path}");
        await next.Invoke();
    });
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "storage")),
    RequestPath = "/storage"
});

app.UseCors("_myAllowSpecificOrigins");

app.UseRouting(); // S'assure que le routage est en place
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
