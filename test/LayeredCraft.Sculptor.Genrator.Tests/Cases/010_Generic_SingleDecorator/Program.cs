using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

// Test single generic decorator with concrete service
var serviceProvider = new ServiceCollection()
    .AddScoped<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");