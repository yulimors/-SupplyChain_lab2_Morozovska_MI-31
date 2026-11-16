using Npgsql;

public record Supplier(int Id, string Name, string TaxId);

public record PurchaseOrder(int Id, string Supplier, string Warehouse, string Status, decimal Total, long Items);

public record LowStockProduct(int Id, string Sku, string Name, int MinStock, int TotalStock);

public abstract class Repository
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    protected Repository(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    protected NpgsqlCommand Command(string sql) => new NpgsqlCommand(sql, _connection, _transaction);
}

public class SupplierRepository : Repository
{
    public SupplierRepository(NpgsqlConnection connection, NpgsqlTransaction transaction)
        : base(connection, transaction) { }

    public List<Supplier> GetActive()
    {
        using var cmd = Command("SELECT supplier_id, name, tax_id FROM vw_active_suppliers ORDER BY supplier_id");
        using var reader = cmd.ExecuteReader();

        var result = new List<Supplier>();
        while (reader.Read())
            result.Add(new Supplier(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        return result;
    }

    public void SoftDelete(int supplierId, int userId)
    {
        using var cmd = Command("CALL sp_soft_delete_supplier(@supplier, @user)");
        cmd.Parameters.AddWithValue("supplier", supplierId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.ExecuteNonQuery();
    }
}

public class PurchaseOrderRepository : Repository
{
    public PurchaseOrderRepository(NpgsqlConnection connection, NpgsqlTransaction transaction)
        : base(connection, transaction) { }

    public int Create(int supplierId, int warehouseId, int userId)
    {
        using var cmd = Command("CALL sp_create_purchase_order(@supplier, @warehouse, @user, NULL::int)");
        cmd.Parameters.AddWithValue("supplier", supplierId);
        cmd.Parameters.AddWithValue("warehouse", warehouseId);
        cmd.Parameters.AddWithValue("user", userId);
        return (int)cmd.ExecuteScalar()!;
    }

    public void AddItem(int orderId, int productId, int quantity, int userId)
    {
        using var cmd = Command("CALL sp_add_purchase_order_item(@order, @product, @quantity, @user)");
        cmd.Parameters.AddWithValue("order", orderId);
        cmd.Parameters.AddWithValue("product", productId);
        cmd.Parameters.AddWithValue("quantity", quantity);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.ExecuteNonQuery();
    }

    public void Send(int orderId, int userId)
    {
        using var cmd = Command("CALL sp_send_purchase_order(@order, @user)");
        cmd.Parameters.AddWithValue("order", orderId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.ExecuteNonQuery();
    }

    public void Receive(int orderId, int userId)
    {
        using var cmd = Command("CALL sp_receive_purchase_order(@order, @user)");
        cmd.Parameters.AddWithValue("order", orderId);
        cmd.Parameters.AddWithValue("user", userId);
        cmd.ExecuteNonQuery();
    }

    public List<PurchaseOrder> GetAll()
    {
        using var cmd = Command(
            "SELECT po_id, supplier_name, warehouse_name, status::text, total_amount, items_count " +
            "FROM vw_purchase_order_summary ORDER BY po_id");
        using var reader = cmd.ExecuteReader();

        var result = new List<PurchaseOrder>();
        while (reader.Read())
            result.Add(new PurchaseOrder(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                                         reader.GetString(3), reader.GetDecimal(4), reader.GetInt64(5)));
        return result;
    }
}

public class StockRepository : Repository
{
    public StockRepository(NpgsqlConnection connection, NpgsqlTransaction transaction)
        : base(connection, transaction) { }

    public List<LowStockProduct> GetLowStock()
    {
        using var cmd = Command(
            "SELECT product_id, sku, name, min_stock_level, total_stock FROM vw_low_stock ORDER BY product_id");
        using var reader = cmd.ExecuteReader();

        var result = new List<LowStockProduct>();
        while (reader.Read())
            result.Add(new LowStockProduct(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                                           reader.GetInt32(3), reader.GetInt32(4)));
        return result;
    }
}
