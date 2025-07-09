using Microsoft.AspNetCore.Http;

namespace YooVisitStorageAPI.Dtos;

public class PhotoUploadRequest
{
    // Le fichier envoyé par Flutter
    public IFormFile File { get; set; }
    // La latitude envoyée comme champ de formulaire
    public double Latitude { get; set; }
    // La longitude envoyée comme champ de formulaire
    public double Longitude { get; set; }
}