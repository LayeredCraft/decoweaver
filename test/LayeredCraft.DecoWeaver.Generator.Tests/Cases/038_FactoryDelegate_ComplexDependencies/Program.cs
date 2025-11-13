using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test factory delegate with complex dependencies
// Factory resolves ILogger from IServiceProvider and passes it to the constructor
var serviceProvider = new ServiceCollection()
    .AddSingleton<ILogger, ConsoleLogger>()
    .AddScoped<IRepository<Account>, Repository<Account>>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger>();
        return new Repository<Account>(logger);
    })
    .BuildServiceProvider();

// Decorator should be applied, and factory dependencies should be resolved
var repo = serviceProvider.GetRequiredService<IRepository<Account>>();
Console.WriteLine($"Resolved: {repo.GetType().Name}");

// Expected: CachingRepository<Account> wrapping Repository<Account> (with logger injected)

public class Account
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
}