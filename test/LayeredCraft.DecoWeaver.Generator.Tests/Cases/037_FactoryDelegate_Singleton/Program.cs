using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test AddSingleton with factory delegate
// Decorator should be applied to singleton registrations
var serviceProvider = new ServiceCollection()
    .AddSingleton<ICache<string>, InMemoryCache<string>>(sp =>
        new InMemoryCache<string>())
    .BuildServiceProvider();

// Should resolve with decorator
var cache = serviceProvider.GetRequiredService<ICache<string>>();
Console.WriteLine($"Resolved: {cache.GetType().Name}");

// Expected: LoggingCache<string> wrapping InMemoryCache<string>