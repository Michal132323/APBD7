using System.Data;
using System.Data.SqlClient;
using warehouse.DTOs;
using Dapper;

namespace warehouse.Services;

public interface IWarehouseService
{
    Task<WarehouseResponseDTO> AddProductToWarehouse(WarehouseRequestDTO request);
}

public class WarehouseService(IConfiguration configuration) : IWarehouseService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

    public async Task<WarehouseResponseDTO> AddProductToWarehouse(WarehouseRequestDTO request)
    {
        await using var connection = await GetConnection();
        await using var transaction = await connection.BeginTransactionAsync();


        try
        {
            var returnedId = await connection.ExecuteScalarAsync<int>(
                "AddProductToWarehouse",
                new
                {
                    IdProduct = request.IdProduct,
                    IdWarehouse = request.IdWarehouse,
                    Amount = request.Amount,
                    CreatedAt = request.CreatedAt
                },
                commandType: CommandType.StoredProcedure,
                transaction: transaction
            );
            


            await transaction.CommitAsync();
        var response = new WarehouseResponseDTO
        {
            NewId = returnedId
        };
        return response;
        
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}