using Microsoft.Extensions.DependencyInjection;
using DecoWeaver.Sample;

// Test IsInterceptable = false with generic syntax
// Should resolve directly to UserService without any wrapping
var serviceProvider = new ServiceCollection()
    .AddScoped<IUserService, UserService>()
    .BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IUserService>();
Console.WriteLine($"Resolved: {userService.GetType().Name}");
userService.CreateUser("Test User");