using Application.Dtos;
using Application.Service.Interfaces;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WebApi.Controllers
{
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
        public async Task<IActionResult> Get()
        {
            try
            {
                var result = await _educationService.GetAll();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            try
            {
                var result = await _educationService.GetById(id);
                if (result is null) return NoContent();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] EducationDto model)
        {
            try
            {
                if (!ModelState.IsValid || model is null) return BadRequest(ModelState);
                var result = await _educationService.Add(model);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] EducationDto model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                var result = await _educationService.Update(id, model);
                if (!result) return BadRequest();
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            try
            {
                var result = await _educationService.Delete(id);
                if (!result) return BadRequest();
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var result = await _educationService.GetStatistics();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query parameter 'q' is required.");
                var result = await _educationService.Search(q);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("timeline/{userId:guid}")]
        public async Task<IActionResult> GetTimeline([FromRoute] Guid userId)
        {
            try
            {
                var result = await _educationService.GetTimelineByUserId(userId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                if (file is null || file.Length == 0)
                    return BadRequest("File is required.");

                var imported = new List<EducationDto>();
                var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();

                using var stream = file.OpenReadStream();

                if (extension == ".csv")
                {
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    var records = csv.GetRecords<EducationDto>().ToList();
                    foreach (var record in records)
                    {
                        var added = await _educationService.Add(record);
                        imported.Add(added);
                    }
                }
                else if (extension == ".json")
                {
                    var records = await JsonSerializer.DeserializeAsync<List<EducationDto>>(stream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (records is not null)
                    {
                        foreach (var record in records)
                        {
                            var added = await _educationService.Add(record);
                            imported.Add(added);
                        }
                    }
                }
                else
                {
                    return BadRequest("Unsupported file format. Use .csv or .json.");
                }

                return Ok(new { ImportedCount = imported.Count, Records = imported });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] string format)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(format))
                    return BadRequest("Query parameter 'format' is required (csv or json).");

                var educations = await _educationService.GetAll();
                var educationList = educations.ToList();

                if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    using var memoryStream = new MemoryStream();
                    using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                    using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    csv.WriteRecords(educationList);
                    await writer.FlushAsync();
                    var bytes = memoryStream.ToArray();
                    return File(bytes, "text/csv", "educations.csv");
                }
                else if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    var json = JsonSerializer.Serialize(educationList, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    var bytes = Encoding.UTF8.GetBytes(json);
                    return File(bytes, "application/json", "educations.json");
                }
                else
                {
                    return BadRequest("Unsupported format. Use 'csv' or 'json'.");
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
