using System.ComponentModel.DataAnnotations;

namespace YooVisitStorageAPI.Models
{
    public class PastilleRating
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PastilleId { get; set; } // <-- ON LIE À LA PASTILLE

        [Required]
        public Guid RaterUserId { get; set; }

        public int RatingValue { get; set; }
        public DateTime RatedAt { get; set; }
    }
}
