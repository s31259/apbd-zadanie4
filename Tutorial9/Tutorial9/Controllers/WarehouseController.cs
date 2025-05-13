using Microsoft.AspNetCore.Mvc;
using Tutorial9.Exceptions;
using Tutorial9.Model.DTOs;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    
    public WarehouseController(IWarehouseService WarehouseService)
    {
        _warehouseService = WarehouseService;
    }
    
    [HttpPost("warehouses/products")]
    public async Task<IActionResult> addProductToWarehouseAsync(ProductWarehouseDTO productWarehouseDTO)
    {
        if (!await _warehouseService.DoesProductExist(productWarehouseDTO.IdProduct))
        {
            return NotFound($"Product with given ID - {productWarehouseDTO.IdProduct} doesn't exist");
        }
        
        if (!await _warehouseService.DoesWarehouseExist(productWarehouseDTO.IdWarehouse))
        {
            return NotFound($"Warehouse with given ID - {productWarehouseDTO.IdWarehouse} doesn't exist");
        }

        try
        {
            var id = await _warehouseService.addProductToWarehouseAsync(productWarehouseDTO);
            return Created(Request.Path.Value ?? "api/warehouses/products", id);
        }
        catch (BadRequestException bre)
        {
            return BadRequest(bre.Message);
        }
        catch (NotFoundException nfe)
        {
            return NotFound(nfe.Message);
        }
    }
    
    [HttpPost("warehouses/products/procedure")]
    public async Task<IActionResult> addProductToWarehouseWithProcedureAsync(ProductWarehouseDTO productWarehouseDTO)
    {
        var id = await _warehouseService.addProductToWarehouseWithProcedureAsync(productWarehouseDTO);
        return Created(Request.Path.Value ?? "api/warehouses/products", id);
    }
}