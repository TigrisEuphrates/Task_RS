using Microsoft.AspNetCore.Mvc;
using Task_RS.Interfaces;

namespace Task_RS.Controllers;

[ApiController]
[Route("api")]
public class MainController : ControllerBase
{
    private readonly IExcelMappingService _excelMappingService;
    private readonly IDataService _dataService;

    public MainController(IExcelMappingService excelMappingService, IDataService dataService)
    {
        _excelMappingService = excelMappingService;
        _dataService = dataService;
    }


    [HttpPost("xlsxupload")]
    public async Task<IActionResult> UploadXlsx(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File not received");

        if (!file.FileName.EndsWith(".xlsx"))
            return BadRequest("Only .xlsx");

        if (file.ContentType !=
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            return BadRequest("Invalid MIME type");
        }

      
        using var stream = file.OpenReadStream();
        var products = _excelMappingService.MapProducts(stream);

        await _dataService.AddProductsAsync(products);

        return Ok();
    }
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups()
    {
        var groups = await _dataService.GetGroupsAsync();
        return Ok(groups);
    }


    [HttpGet("productsbygroup")]
    public async Task<IActionResult> GetProductsByGroup([FromQuery] int id)
    {
        var productsByGroup = await _dataService.GetProductsByGroupAsync(id);
        return Ok(productsByGroup);
    }
}
