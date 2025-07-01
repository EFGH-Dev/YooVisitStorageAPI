using YooVisitStorageAPI.PhotoInterface;
using YooVisitStorageAPI.PhotoServices;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

// --- Section Services ---
builder.Services.AddControllers();

// On r�active notre service de stockage de fichiers
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Configuration CORS
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

// Configuration de Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Section Pipeline de Requ�tes ---

// On peut garder une sonde de d�bogage simple pour le mode D�veloppement
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"--> Requ�te re�ue: {context.Request.Method} {context.Request.Path}");
        await next.Invoke();
    });
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = string.Empty;
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "YooVisit API V1");
});

app.UseCors(MyAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();
app.Run();