using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// Test singleton lifetime with generic decorator syntax
var serviceProvider = new ServiceCollection()
    .AddSingleton<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");