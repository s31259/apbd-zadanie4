using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<bool> DoesProductExist(int idProduct);
    Task<bool> DoesWarehouseExist(int idWarehouse);
    Task<int> addProductToWarehouseAsync(ProductWarehouseDTO product_WarehouseDTO);
    Task<int> addProductToWarehouseWithProcedureAsync(ProductWarehouseDTO productWarehouseDTO);
}