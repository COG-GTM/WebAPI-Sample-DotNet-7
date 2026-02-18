using Application.Dtos;

namespace WebApi.GraphQL.Subscriptions;

public class EducationSubscriptions
{
    [Subscribe]
    public EducationDto EducationAdded([EventMessage] EducationDto education)
        => education;
}
