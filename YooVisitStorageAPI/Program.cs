using YooVisitStorageAPI.PhotoInterface;
using YooVisitStorageAPI.PhotoServices;

var builder = WebApplication.CreateBuilder(args);

// --- Section Services ---
builder.Services.AddControllers();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Configuration de Swagger pour la documentation
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Section Pipeline de Requ�tes ---
app.Use(async (context, next) =>
{
    Console.WriteLine($"--> Requ�te re�ue: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"<-- R�ponse envoy�e: {context.Response.StatusCode}");
});

// Configuration de l'UI de Swagger pour �tre � la racine
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "YooVisit API V1");
});

app.UseAuthorization();

app.MapControllers();

app.Run();