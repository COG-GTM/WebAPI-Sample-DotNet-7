namespace Application.Dtos
{
    public class EducationStatisticsDto
    {
        public int TotalCount { get; set; }
        public Dictionary<string, int> DegreeCounts { get; set; } = new();
        public List<SchoolCountDto> MostCommonSchools { get; set; } = new();
    }

    public class SchoolCountDto
    {
        public required string School { get; set; }
        public int Count { get; set; }
    }
}
