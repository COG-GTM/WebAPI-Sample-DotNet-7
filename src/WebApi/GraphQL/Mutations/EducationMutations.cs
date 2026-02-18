using Application.Dtos;
using Application.Service.Interfaces;
using HotChocolate.Subscriptions;
using WebApi.GraphQL.Subscriptions;

namespace WebApi.GraphQL.Mutations;

public class EducationMutations
{
    public async Task<EducationPayload> CreateEducation(
        CreateEducationInput input,
        [Service] IEducationService educationService,
        [Service] ITopicEventSender eventSender)
    {
        var dto = new EducationDto
        {
            Degree = input.Degree,
            FieldOfStudy = input.FieldOfStudy,
            School = input.School,
            Description = input.Description,
            StartDate = input.StartDate,
            EndDate = input.EndDate
        };

        var result = await educationService.Add(dto);
        await eventSender.SendAsync(nameof(EducationSubscriptions.EducationAdded), result);
        return new EducationPayload(result);
    }

    public async Task<EducationPayload> UpdateEducation(
        Guid id,
        UpdateEducationInput input,
        [Service] IEducationService educationService)
    {
        var dto = new EducationDto
        {
            Id = id,
            Degree = input.Degree,
            FieldOfStudy = input.FieldOfStudy,
            School = input.School,
            Description = input.Description,
            StartDate = input.StartDate,
            EndDate = input.EndDate
        };

        var success = await educationService.Update(id, dto);
        if (!success)
            throw new GraphQLException("Education not found or update failed.");

        var updated = await educationService.GetById(id);
        return new EducationPayload(updated!);
    }

    public async Task<bool> DeleteEducation(
        Guid id,
        [Service] IEducationService educationService)
    {
        return await educationService.Delete(id);
    }
}

public class CreateEducationInput
{
    public required string Degree { get; set; }
    public required string FieldOfStudy { get; set; }
    public required string School { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateEducationInput
{
    public required string Degree { get; set; }
    public required string FieldOfStudy { get; set; }
    public required string School { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class EducationPayload
{
    public EducationDto Education { get; }

    public EducationPayload(EducationDto education)
    {
        Education = education;
    }
}
