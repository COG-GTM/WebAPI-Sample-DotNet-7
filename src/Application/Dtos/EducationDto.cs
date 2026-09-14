using System.ComponentModel.DataAnnotations;

namespace Application.Dtos
{
    public class EducationDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Degree { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string FieldOfStudy { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string School { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
