using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service with multiple decorators (should apply in ascending order)
services.AddKeyedScoped<IRepository<Product>, DynamoDbRepository<Product>>("primary");

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Product>>("primary");
repo.Save(new Product { Id = 1, Name = "Widget" });
