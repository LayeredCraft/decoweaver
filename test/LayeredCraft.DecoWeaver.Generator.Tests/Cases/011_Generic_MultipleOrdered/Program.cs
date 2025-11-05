using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test multiple ordered generic decorators
// Expected: Logging -> Audit -> UserService
var serviceProvider = new ServiceCollection()
    .AddScoped<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");