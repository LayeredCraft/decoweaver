using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Instance registration without decorators
var instance = new SqlRepository<Customer>();
services.AddSingleton<IRepository<Customer>>(instance);

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredService<IRepository<Customer>>();
repo.Save(new Customer { Id = 1, Name = "John" });
