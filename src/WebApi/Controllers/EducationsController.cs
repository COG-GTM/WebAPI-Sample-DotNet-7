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
            ? ProblemResult(
                StatusCodes.Status404NotFound,
                "Education not found.",
                $"No education with id '{id}' exists.")
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
            return ValidationProblemResult();
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
            return ValidationProblemResult();
        }

        if (model.Id != id)
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                "Route id and body id do not match.",
                "The route id and body id must be the same.");
        }

        var result = await _educationService.Update(id, model);
        return result
            ? NoContent()
            : ProblemResult(
                StatusCodes.Status404NotFound,
                "Education not found.",
                $"No education with id '{id}' exists.");
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
            : ProblemResult(
                StatusCodes.Status404NotFound,
                "Education not found.",
                $"No education with id '{id}' exists.");
    }

    private ObjectResult ProblemResult(int status, string title, string detail)
    {
        if (ControllerContext.HttpContext is not null)
        {
            ControllerContext.HttpContext.Response.ContentType = "application/problem+json";
        }
        return new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private IActionResult ValidationProblemResult()
    {
        if (ControllerContext.HttpContext is not null)
        {
            ControllerContext.HttpContext.Response.ContentType = "application/problem+json";
        }
        var result = ValidationProblem(ModelState);
        if (result is ObjectResult objectResult)
        {
            objectResult.StatusCode = StatusCodes.Status400BadRequest;
            if (objectResult.Value is ProblemDetails problemDetails)
            {
                problemDetails.Status = StatusCodes.Status400BadRequest;
            }
        }

        return result;
    }
}
