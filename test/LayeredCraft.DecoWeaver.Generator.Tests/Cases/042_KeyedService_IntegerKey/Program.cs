using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service with integer key (demonstrates non-string keys)
services.AddKeyedScoped<IRepository<Order>, DatabaseRepository<Order>>(1);

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Order>>(1);
repo.Save(new Order { Id = 100, Total = 299.99m });
