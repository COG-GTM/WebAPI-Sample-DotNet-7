using Application.Dtos;

namespace WebApi.GraphQL.Types;

public class EducationType : ObjectType<EducationDto>
{
    protected override void Configure(IObjectTypeDescriptor<EducationDto> descriptor)
    {
        descriptor.Field(e => e.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(e => e.Degree).Type<NonNullType<StringType>>();
        descriptor.Field(e => e.FieldOfStudy).Type<NonNullType<StringType>>();
        descriptor.Field(e => e.School).Type<NonNullType<StringType>>();
        descriptor.Field(e => e.Description).Type<StringType>();
        descriptor.Field(e => e.StartDate).Type<DateTimeType>();
        descriptor.Field(e => e.EndDate).Type<DateTimeType>();
    }
}
