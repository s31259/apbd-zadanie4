using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model.DTOs;

public class ProductWarehouseDTO
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    [Range(1, Int32.MaxValue)]
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}