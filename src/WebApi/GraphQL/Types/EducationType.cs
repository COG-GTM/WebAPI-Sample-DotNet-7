using Application.Dtos;

namespace WebApi.GraphQL.Types;

public class EducationType : ObjectType<EducationDto>
{
    protected override void Configure(IObjectTypeDescriptor<EducationDto> descriptor)
    {
        descriptor.Name("Education");

        descriptor.Field(e => e.Id)
            .Type<NonNullType<UuidType>>()
            .Description("The unique identifier of the education record");

        descriptor.Field(e => e.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree obtained (max 50 characters)");

        descriptor.Field(e => e.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (max 250 characters)");

        descriptor.Field(e => e.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (max 250 characters)");

        descriptor.Field(e => e.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}
