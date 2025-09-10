using System.ComponentModel.DataAnnotations;

namespace PROG7312_POEPART1.Models
{
    public class Issue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string? Location { get; set; }
        [Required]
        public string? Category { get; set; }
        [Required]
        public string? Description { get; set; }
        public string? MediaFileName { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
