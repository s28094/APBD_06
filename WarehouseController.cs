using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace APBD_06;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly string _connectionString;

    public WarehouseController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }


    [HttpPost("addproduct")]
    public IActionResult AddProductToWarehouse([FromBody] WarehouseRequestModel model)
    {
        /*try
        {*/
            // Perform validation checks on model data
            if (model == null)
            {
                return BadRequest("Request body is null");
            }
            if (model.IdProduct <= 0 || model.IdWarehouse <= 0 || model.Amount <= 0)
            {
                return BadRequest("Invalid request data");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if product with the given IdProduct exists
                SqlCommand productExistCommand = new SqlCommand("SELECT COUNT(*) FROM Product WHERE IdProduct = @IdProduct", connection);
                productExistCommand.Parameters.AddWithValue("@IdProduct", model.IdProduct);
                int productCount = Convert.ToInt32(productExistCommand.ExecuteScalar());
                if (productCount == 0)
                {
                    return NotFound("Product does not exist");
                }

                // Check if warehouse with the given IdWarehouse exists
                SqlCommand warehouseExistCommand = new SqlCommand("SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @IdWarehouse", connection);
                warehouseExistCommand.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
                int warehouseCount = Convert.ToInt32(warehouseExistCommand.ExecuteScalar());
                if (warehouseCount == 0)
                {
                    return NotFound("Warehouse does not exist");
                }

                // Check if there is an order to fulfill
                SqlCommand orderFulfillCommand = new SqlCommand(@"SELECT TOP 1 IdOrder
                                                                FROM [Order]
                                                                WHERE IdProduct = @IdProduct 
                                                                AND Amount = @Amount 
                                                                AND FulfilledAt IS NULL 
                                                                AND CreatedAt < @CreatedAt", connection);
                orderFulfillCommand.Parameters.AddWithValue("@IdProduct", model.IdProduct);
                orderFulfillCommand.Parameters.AddWithValue("@Amount", model.Amount);
                orderFulfillCommand.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
                int? orderId = orderFulfillCommand.ExecuteScalar() as int?;
                if (!orderId.HasValue)
                {
                    return BadRequest("No order to fulfill");
                }

                // Update the FullfilledAt column of the order with the current date and time
                SqlCommand updateOrderCommand = new SqlCommand("UPDATE [Order] SET FulfilledAt = @FulfilledAt WHERE IdOrder = @IdOrder", connection);
                updateOrderCommand.Parameters.AddWithValue("@FulfilledAt", DateTime.Now);
                updateOrderCommand.Parameters.AddWithValue("@IdOrder", orderId.Value);
                updateOrderCommand.ExecuteNonQuery();

                // Insert a record into the Product_Warehouse table
                SqlCommand insertCommand = new SqlCommand(@"INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                                            VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt);
                                                            SELECT SCOPE_IDENTITY();", connection);
                insertCommand.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
                insertCommand.Parameters.AddWithValue("@IdProduct", model.IdProduct);
                insertCommand.Parameters.AddWithValue("@IdOrder", orderId.Value);
                insertCommand.Parameters.AddWithValue("@Amount", model.Amount);

                // Retrieve Product_Warehouse primary key
                SqlCommand productPriceCommand = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", connection);
                productPriceCommand.Parameters.AddWithValue("@IdProduct", model.IdProduct);
                decimal price = Convert.ToDecimal(productPriceCommand.ExecuteScalar());
                insertCommand.Parameters.AddWithValue("@Price", model.Amount * price);

                insertCommand.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                // Execute the insert command and get the generated primary key
                int newId = Convert.ToInt32(insertCommand.ExecuteScalar());

                return Ok(new { Id = newId });
            }
        /*}*/
        /*catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }*/
    }

        [HttpPost("addproductwithprocedure")]
        public IActionResult AddProductToWarehouseWithProcedure([FromBody] WarehouseRequestModel model)
        {
            /*try
            {*/
                // Perform validation checks on model data
                if (model == null)
                {
                    return BadRequest("Request body is null");
                }
                if (model.IdProduct <= 0 || model.IdWarehouse <= 0 || model.Amount <= 0)
                {
                    return BadRequest("Invalid request data");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Execute stored procedure
                    SqlCommand command = new SqlCommand("AddProductToWarehouse", connection);
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@IdProduct", model.IdProduct);
                    command.Parameters.AddWithValue("@IdWarehouse", model.IdWarehouse);
                    command.Parameters.AddWithValue("@Amount", model.Amount);
                    command.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);

                    int newId = Convert.ToInt32(command.ExecuteScalar());

                    return Ok(new { Id = newId });
                }
            /*}*/
            /*catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }*/
        }
}

public class WarehouseRequestModel
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
