using System.ComponentModel.DataAnnotations;

namespace Application.Dtos
{
    public class EducationDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Degree { get; set; }

        [Required]
        [StringLength(250)]
        public required string FieldOfStudy { get; set; }

        [Required]
        [StringLength(250)]
        public required string School { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
