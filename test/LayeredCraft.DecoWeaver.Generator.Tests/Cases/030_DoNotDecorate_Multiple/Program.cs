using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Should only have LoggingRepository (Caching and Validation excluded)
services.AddScoped<IRepository<Order>, SqlRepository<Order>>();

var provider = services.BuildServiceProvider();