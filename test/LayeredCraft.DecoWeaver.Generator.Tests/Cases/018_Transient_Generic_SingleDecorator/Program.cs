using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test transient lifetime with generic decorator syntax
var serviceProvider = new ServiceCollection()
    .AddTransient<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");