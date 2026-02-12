using Application.Dtos;
using Application.Service.Interfaces;

namespace WebApi.GraphQL.DataLoaders;

public class EducationBatchDataLoader : BatchDataLoader<Guid, EducationDto>
{
    private readonly IEducationService _educationService;

    public EducationBatchDataLoader(
        IEducationService educationService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _educationService = educationService;
    }

    protected override async Task<IReadOnlyDictionary<Guid, EducationDto>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var educations = await _educationService.GetAll();
        return educations
            .Where(e => keys.Contains(e.Id))
            .ToDictionary(e => e.Id);
    }
}
