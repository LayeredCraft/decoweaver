using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Keyed service with factory delegate - decorator should still be applied
services.AddKeyedScoped<IRepository<Customer>, ConfigurableRepository<Customer>>(
    "primary",
    (sp, key) => new ConfigurableRepository<Customer>("Server=primary;Database=Main")
);

var serviceProvider = services.BuildServiceProvider();
var repo = serviceProvider.GetRequiredKeyedService<IRepository<Customer>>("primary");
repo.Save(new Customer { Id = 1, Name = "Alice" });
