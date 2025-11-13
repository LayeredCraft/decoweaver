using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service without decorators - should pass through
services.AddKeyedScoped<IRepository<Data>, PlainRepository<Data>>("cache");

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Data>>("cache");
repo.Save(new Data { Id = 42, Value = "Test" });
