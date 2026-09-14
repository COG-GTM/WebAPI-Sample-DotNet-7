using Application.Dtos;
using Application.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EducationsController : ControllerBase
{
    private readonly IEducationService _educationService;

    public EducationsController(IEducationService educationService)
    {
        _educationService = educationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EducationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get()
    {
        var result = await _educationService.GetAll();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EducationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var result = await _educationService.GetById(id);
        return result is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Education not found.",
                detail: $"No education with id '{id}' exists.")
            : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EducationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] EducationDto model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _educationService.Add(model);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] EducationDto model)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (model.Id != id)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Route id and body id do not match.",
                detail: "The route id and body id must be the same.");
        }

        var result = await _educationService.Update(id, model);
        return result
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Education not found.",
                detail: $"No education with id '{id}' exists.");
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _educationService.Delete(id);
        return result
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Education not found.",
                detail: $"No education with id '{id}' exists.");
    }
}
