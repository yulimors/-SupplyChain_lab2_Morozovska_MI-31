const string connectionString = "Host=localhost;Database=logist_system;Username=yulia;Password=pass";

using var uow = new UnitOfWork(connectionString);

try
{
    int orderId = uow.PurchaseOrders.Create(supplierId: 1, warehouseId: 2, userId: 2);
    uow.PurchaseOrders.AddItem(orderId, productId: 1, quantity: 100, userId: 2);
    uow.PurchaseOrders.AddItem(orderId, productId: 2, quantity: 50, userId: 2);
    uow.PurchaseOrders.Receive(orderId, userId: 2);

    uow.Suppliers.SoftDelete(supplierId: 2, userId: 1);

    uow.Commit();
    Console.WriteLine("Збережено.");
}
catch (Exception ex)
{
    uow.Rollback();
    Console.WriteLine("Скасовано: " + ex.Message);
    return;
}

using var reader = new UnitOfWork(connectionString);

Console.WriteLine("Активні постачальники:");
foreach (var s in reader.Suppliers.GetActive())
    Console.WriteLine($"  {s.Id} {s.Name} ({s.TaxId})");

Console.WriteLine("Замовлення:");
foreach (var o in reader.PurchaseOrders.GetAll())
    Console.WriteLine($"  №{o.Id} {o.Supplier} -> {o.Warehouse}, {o.Status}, {o.Total}, позицій: {o.Items}");
