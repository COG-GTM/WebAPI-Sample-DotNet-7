using Application.Dtos;
using Application.Service.Interfaces;
using WebApi.GraphQL.Types;

namespace WebApi.GraphQL.Mutations;

public class EducationMutations
{
    public async Task<EducationPayload> CreateEducation(
        CreateEducationInput input,
        [Service] IEducationService educationService)
    {
        try
        {
            var dto = new EducationDto
            {
                Degree = input.Degree,
                FieldOfStudy = input.FieldOfStudy,
                School = input.School,
                Description = input.Description
            };

            var result = await educationService.Add(dto);
            return new EducationPayload(result);
        }
        catch (Exception ex)
        {
            return new EducationPayload(new GraphQLError(ex.Message, "CREATE_FAILED"));
        }
    }

    public async Task<EducationPayload> UpdateEducation(
        UpdateEducationInput input,
        [Service] IEducationService educationService)
    {
        try
        {
            var dto = new EducationDto
            {
                Id = input.Id,
                Degree = input.Degree,
                FieldOfStudy = input.FieldOfStudy,
                School = input.School,
                Description = input.Description
            };

            var success = await educationService.Update(input.Id, dto);
            if (!success)
            {
                return new EducationPayload(new GraphQLError("Education record not found or update failed", "UPDATE_FAILED"));
            }

            var updated = await educationService.GetById(input.Id);
            return new EducationPayload(updated);
        }
        catch (Exception ex)
        {
            return new EducationPayload(new GraphQLError(ex.Message, "UPDATE_FAILED"));
        }
    }

    public async Task<DeleteEducationPayload> DeleteEducation(
        Guid id,
        [Service] IEducationService educationService)
    {
        try
        {
            var success = await educationService.Delete(id);
            if (!success)
            {
                return new DeleteEducationPayload(false, new GraphQLError("Education record not found", "NOT_FOUND"));
            }
            return new DeleteEducationPayload(true);
        }
        catch (Exception ex)
        {
            return new DeleteEducationPayload(false, new GraphQLError(ex.Message, "DELETE_FAILED"));
        }
    }
}
