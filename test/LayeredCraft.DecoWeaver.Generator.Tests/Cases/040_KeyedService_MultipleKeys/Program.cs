using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Multiple keyed services with same interface but different keys
services.AddKeyedScoped<IRepository<User>, SqlRepository<User>>("sql");
services.AddKeyedScoped<IRepository<User>, CosmosRepository<User>>("cosmos");

var serviceProvider = services.BuildServiceProvider();

// Resolve each by key - both should be decorated independently
var sqlRepo = serviceProvider.GetRequiredKeyedService<IRepository<User>>("sql");
sqlRepo.Save(new User { Id = 1, Name = "John" });

var cosmosRepo = serviceProvider.GetRequiredKeyedService<IRepository<User>>("cosmos");
cosmosRepo.Save(new User { Id = 2, Name = "Jane" });
