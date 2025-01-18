using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tevling.Data;

namespace Tevling;

public class InMemoryDataContextFactory : IDbContextFactory<DataContext>
{
    // InMemoryDatabaseRoot is used to create a new in-memory database for each instance of this factory
    private readonly InMemoryDatabaseRoot _inMemoryDatabaseRoot = new();

    public DataContext CreateDbContext()
    {
        return new DataContext(
            new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase("InMemoryDataContext", _inMemoryDatabaseRoot)
                .Options);
    }
}
