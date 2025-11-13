using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service with Transient lifetime
services.AddKeyedTransient<IRepository<Event>, MemoryRepository<Event>>("events");

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Event>>("events");
repo.Save(new Event { Id = 1, Type = "UserCreated" });
