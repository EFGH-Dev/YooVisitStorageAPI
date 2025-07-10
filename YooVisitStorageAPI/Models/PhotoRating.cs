using System.ComponentModel.DataAnnotations;

namespace YooVisitStorageAPI.Models
{
    public class PhotoRating
    {
        [Key]
        public Guid Id { get; set; }

        // La photo qui est notée
        [Required]
        public Guid PhotoId { get; set; }

        // Le joueur qui donne la note
        [Required]
        public Guid RaterUserId { get; set; }

        // La note donnée (par exemple, de 1 à 5)
        public int RatingValue { get; set; }

        public DateTime RatedAt { get; set; }
    }
}
