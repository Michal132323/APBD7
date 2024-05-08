namespace warehouse.DTOs;

public class WarehouseRequestDTO
{
    public int IdProduct { get; set; }
    public int IdWarehouse { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WarehouseResponseDTO
{
    public int NewId { get; set; }
}

