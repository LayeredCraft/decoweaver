using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed instance registration with single decorator
var instance = new SqlRepository<Customer>();
services.AddKeyedSingleton<IRepository<Customer>>("my-key", instance);

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Customer>>("my-key");
repo.Save(new Customer { Id = 1, Name = "John" });