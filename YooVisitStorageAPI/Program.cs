using YooVisitStorageAPI.PhotoInterface;
using YooVisitStorageAPI.PhotoServices;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

// --- Section Services ---
builder.Services.AddControllers();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Configuration CORS pour autoriser les requêtes de l'appli mobile
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// Configuration de Swagger pour la documentation
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Section Pipeline de Requêtes ---

// Configuration de l'UI de Swagger pour être à la racine
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "YooVisit API V1");
});

// Important : UseCors doit être appelé avant UseAuthorization.
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthorization();

app.MapControllers();

app.Run();