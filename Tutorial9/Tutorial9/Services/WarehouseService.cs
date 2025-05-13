using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Exceptions;
using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString;

    public WarehouseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
    
    public async Task<bool> DoesProductExist(int idProduct)
    {
        var command = "SELECT * FROM Product WHERE IdProduct = @IdProduct";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();

        cmd.Connection = conn;
        cmd.CommandText = command;
        cmd.Parameters.AddWithValue("@IdProduct", idProduct);

        await conn.OpenAsync();

        var product = await cmd.ExecuteScalarAsync();

        return product is not null;
    }
    
    public async Task<bool> DoesWarehouseExist(int idWarehouse)
    {
        var command = "SELECT * FROM Warehouse WHERE IdWarehouse = @IdWarehouse";

        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand();

        cmd.Connection = conn;
        cmd.CommandText = command;
        cmd.Parameters.AddWithValue("@IdWarehouse", idWarehouse);

        await conn.OpenAsync();

        var warehouse = await cmd.ExecuteScalarAsync();

        return warehouse is not null;
    }
    
    public async Task<int> addProductToWarehouseAsync(ProductWarehouseDTO productWarehouseDTO)
    {
        string command = "SELECT * FROM \"Order\" WHERE IdProduct = @IdProduct AND Amount = @Amount";
        
        await using SqlConnection conn = new SqlConnection(_connectionString);
        await using SqlCommand cmd = new SqlCommand(command, conn);
        await conn.OpenAsync();
        
        cmd.Parameters.AddWithValue("@IdProduct", productWarehouseDTO.IdProduct);
        cmd.Parameters.AddWithValue("@Amount", productWarehouseDTO.Amount);
        
        var reader = await cmd.ExecuteReaderAsync();
        
        int? idOrder = null;
        DateTime? orderCreateDate = null;

        while (await reader.ReadAsync())
        {
            idOrder = reader.GetInt32(0);
            orderCreateDate = reader.GetDateTime(3);
        }
        await reader.DisposeAsync();

        if (orderCreateDate > productWarehouseDTO.CreatedAt)
        {
            throw new BadRequestException("Order create date for specified product and amount is later than date in request");
        }
        
        if (orderCreateDate is null)
        {
            throw new NotFoundException("Not found order for specified product and amount");
        }
        
        command = "SELECT * FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        cmd.CommandText = command;
        
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        
        var checker = await cmd.ExecuteScalarAsync();

        if (checker is not null)
        {
            throw new BadRequestException("Order already fulfilled");
        }
        
        DbTransaction transaction = await conn.BeginTransactionAsync();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {

            DateTime fulfillDate = DateTime.Now;

            command = "UPDATE \"Order\" SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder";
            cmd.CommandText = command;

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@FulfilledAt", fulfillDate);

            await cmd.ExecuteNonQueryAsync();


            command = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            cmd.CommandText = command;

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdProduct", productWarehouseDTO.IdProduct);

            var price = await cmd.ExecuteScalarAsync();

            command =
                @"INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES 
(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt); SELECT @@IDENTITY AS IdClient";
            cmd.CommandText = command;

            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@IdWarehouse", productWarehouseDTO.IdWarehouse);
            cmd.Parameters.AddWithValue("@IdProduct", productWarehouseDTO.IdProduct);
            cmd.Parameters.AddWithValue("@IdOrder", idOrder);
            cmd.Parameters.AddWithValue("@Amount", productWarehouseDTO.Amount);
            cmd.Parameters.AddWithValue("@Price", Convert.ToInt32(price) * productWarehouseDTO.Amount);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var id = await cmd.ExecuteScalarAsync();

            await transaction.CommitAsync();

            return Convert.ToInt32(id);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<int> addProductToWarehouseWithProcedureAsync(ProductWarehouseDTO productWarehouseDTO)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "AddProductToWarehouse";
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@IdProduct", productWarehouseDTO.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", productWarehouseDTO.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", productWarehouseDTO.Amount);
        command.Parameters.AddWithValue("@CreatedAt", productWarehouseDTO.CreatedAt);
        
        var id = await command.ExecuteScalarAsync();

        return Convert.ToInt32(id);
    }
}