using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register SpecialRepository - should get ValidationRepository but NOT CachingRepository
services.AddScoped<IRepository<User>, SpecialRepository<User>>();

public class User { }