using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// Test transient lifetime with multiple generic decorators
// Expected: Logging -> Audit -> UserService
var serviceProvider = new ServiceCollection()
    .AddTransient<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");