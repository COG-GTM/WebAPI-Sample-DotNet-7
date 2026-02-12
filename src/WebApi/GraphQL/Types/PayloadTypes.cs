using Application.Dtos;

namespace WebApi.GraphQL.Types;

public record Error(string Message, string? Code = null);

public class EducationPayload
{
    public EducationDto? Education { get; }
    public IReadOnlyList<Error>? Errors { get; }

    public EducationPayload(EducationDto? education)
    {
        Education = education;
    }

    public EducationPayload(Error error)
    {
        Errors = new[] { error };
    }

    public EducationPayload(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }
}

public class DeleteEducationPayload
{
    public bool Success { get; }
    public IReadOnlyList<Error>? Errors { get; }

    public DeleteEducationPayload(bool success, Error? error = null)
    {
        Success = success;
        if (error != null)
        {
            Errors = new[] { error };
        }
    }
}
