using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service registration with single decorator
services.AddKeyedScoped<IRepository<Customer>, SqlRepository<Customer>>("sql");

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Customer>>("sql");
repo.Save(new Customer { Id = 1, Name = "John" });
