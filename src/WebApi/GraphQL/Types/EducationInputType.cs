namespace WebApi.GraphQL.Types;

public record CreateEducationInput(
    string Degree,
    string FieldOfStudy,
    string School,
    string? Description);

public record UpdateEducationInput(
    Guid Id,
    string Degree,
    string FieldOfStudy,
    string School,
    string? Description);

public class CreateEducationInputType : InputObjectType<CreateEducationInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateEducationInput> descriptor)
    {
        descriptor.Name("CreateEducationInput");

        descriptor.Field(i => i.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree to be obtained (required, max 50 characters)");

        descriptor.Field(i => i.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (required, max 250 characters)");

        descriptor.Field(i => i.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (required, max 250 characters)");

        descriptor.Field(i => i.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}

public class UpdateEducationInputType : InputObjectType<UpdateEducationInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<UpdateEducationInput> descriptor)
    {
        descriptor.Name("UpdateEducationInput");

        descriptor.Field(i => i.Id)
            .Type<NonNullType<UuidType>>()
            .Description("The unique identifier of the education record to update");

        descriptor.Field(i => i.Degree)
            .Type<NonNullType<StringType>>()
            .Description("The degree (required, max 50 characters)");

        descriptor.Field(i => i.FieldOfStudy)
            .Type<NonNullType<StringType>>()
            .Description("The field of study (required, max 250 characters)");

        descriptor.Field(i => i.School)
            .Type<NonNullType<StringType>>()
            .Description("The school or institution name (required, max 250 characters)");

        descriptor.Field(i => i.Description)
            .Type<StringType>()
            .Description("Optional description of the education (max 1000 characters)");
    }
}
