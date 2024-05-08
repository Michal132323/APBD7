using Microsoft.AspNetCore.Mvc;
using warehouse.DTOs;
using warehouse.Services;

namespace warehouse.Controllers;

[ApiController]
[Route("[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    public ActionResult<WarehouseResponseDTO> AddProductToWarehouse(WarehouseRequestDTO request)
    {
        var response = _warehouseService.AddProductToWarehouse(request);
        return Ok(response);
    }
}
