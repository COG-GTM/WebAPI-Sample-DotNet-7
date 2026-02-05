using Application.Dtos;

namespace WebApi.GraphQL.Types;

public record GraphQLError(string Message, string? Code = null);

public class EducationPayload
{
    public EducationDto? Education { get; }
    public IReadOnlyList<GraphQLError>? Errors { get; }

    public EducationPayload(EducationDto? education)
    {
        Education = education;
    }

    public EducationPayload(GraphQLError error)
    {
        Errors = new[] { error };
    }

    public EducationPayload(IReadOnlyList<GraphQLError> errors)
    {
        Errors = errors;
    }
}

public class DeleteEducationPayload
{
    public bool Success { get; }
    public IReadOnlyList<GraphQLError>? Errors { get; }

    public DeleteEducationPayload(bool success, GraphQLError? error = null)
    {
        Success = success;
        if (error != null)
        {
            Errors = new[] { error };
        }
    }
}
