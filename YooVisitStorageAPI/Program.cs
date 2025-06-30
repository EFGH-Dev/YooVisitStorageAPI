using YooVisitStorageAPI.PhotoInterface;
using YooVisitStorageAPI.PhotoServices;

var builder = WebApplication.CreateBuilder(args);

// --- Section Services ---
builder.Services.AddControllers();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Configuration de Swagger pour la documentation
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Section Pipeline de Requêtes ---
app.Use(async (context, next) =>
{
    Console.WriteLine($"--> Requête reçue: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"<-- Réponse envoyée: {context.Response.StatusCode}");
});

// Configuration de l'UI de Swagger pour être à la racine
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "YooVisit API V1");
});

app.UseAuthorization();

app.MapControllers();

app.Run();