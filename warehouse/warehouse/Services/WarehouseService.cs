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
            string createProcedureSql =
                @"IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'AddProductToWarehouse')
                    BEGIN
                        EXEC('
                        CREATE PROCEDURE AddProductToWarehouse 
                            @IdProduct INT, 
                            @IdWarehouse INT, 
                            @Amount INT,  
                            @CreatedAt DATETIME
                        AS  
                        BEGIN  
                           
                         DECLARE @IdProductFromDb INT, @IdOrder INT, @Price DECIMAL(5,2);  
                          
                         SELECT TOP 1 @IdOrder = o.IdOrder  
                         FROM [Order] o   
                         LEFT JOIN Product_Warehouse pw ON o.IdOrder=pw.IdOrder  
                         WHERE o.IdProduct=@IdProduct AND o.Amount=@Amount AND pw.IdProductWarehouse IS NULL AND  
                         o.CreatedAt<@CreatedAt;  
                          
                         SELECT @IdProductFromDb=Product.IdProduct, @Price=Product.Price FROM Product WHERE IdProduct=@IdProduct  
                           
                         IF @IdProductFromDb IS NULL  
                         BEGIN  
                          RAISERROR('Invalid parameter: Provided IdProduct does not exist', 18, 0);  
                          RETURN;  
                         END;  
                          
                         IF @IdOrder IS NULL  
                         BEGIN  
                          RAISERROR('Invalid parameter: There is no order to fullfill', 18, 0);  
                          RETURN;  
                         END;  
                           
                         IF NOT EXISTS(SELECT 1 FROM Warehouse WHERE IdWarehouse=@IdWarehouse)  
                         BEGIN  
                          RAISERROR('Invalid parameter: Provided IdWarehouse does not exist', 18, 0);  
                          RETURN;  
                         END;  
                          
                         SET XACT_ABORT ON;  
                         BEGIN TRAN;  
                           
                         UPDATE [Order] SET  
                         FulfilledAt=@CreatedAt  
                         WHERE IdOrder=@IdOrder;  
                          
                         INSERT INTO Product_Warehouse(IdWarehouse,   
                         IdProduct, IdOrder, Amount, Price, CreatedAt)  
                         VALUES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount*@Price, @CreatedAt);  
                           
                         SELECT @@IDENTITY AS NewId;
                           
                         COMMIT;  
                        END
                        ');
                    END";

            await connection.ExecuteAsync(createProcedureSql);
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