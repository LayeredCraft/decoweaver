using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// Test mixing generic and non-generic decorator syntax
// Expected: Logging -> Audit -> Validation -> UserService
var serviceProvider = new ServiceCollection()
    .AddScoped<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");