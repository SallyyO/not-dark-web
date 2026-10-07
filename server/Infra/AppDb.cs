namespace Infra;

using LinqToDB; using LinqToDB.Data;

public class AppDb(DataOptions<AppDb> options) : DataConnection(options.Options)
{ 
    public ITable<Entities.Customer> Customers => this.GetTable<Entities.Customer>();
    public ITable<Entities.Category> Categories => this.GetTable<Entities.Category>();
}
