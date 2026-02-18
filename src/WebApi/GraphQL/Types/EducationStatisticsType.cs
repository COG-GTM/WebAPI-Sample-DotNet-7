using Application.Dtos;

namespace WebApi.GraphQL.Types;

public class EducationStatisticsType : ObjectType<EducationStatisticsDto>
{
    protected override void Configure(IObjectTypeDescriptor<EducationStatisticsDto> descriptor)
    {
        descriptor.Field(s => s.TotalCount).Type<NonNullType<IntType>>();
        descriptor.Field(s => s.DegreeCounts).Type<NonNullType<ListType<NonNullType<ObjectType<DegreeCountEntry>>>>>();
        descriptor.Field(s => s.MostCommonSchools).Type<NonNullType<ListType<NonNullType<ObjectType<SchoolCountDto>>>>>();
    }
}

public class DegreeCountEntry
{
    public required string Degree { get; set; }
    public int Count { get; set; }
}
