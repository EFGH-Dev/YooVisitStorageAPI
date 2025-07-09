using System;
using System.ComponentModel.DataAnnotations;

namespace YooVisitStorageAPI.Models;

public class Photo
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    // Les nouvelles propriétés pour la géolocalisation
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [Required]
    public Guid UserId { get; set; }
}