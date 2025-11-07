using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Two registrations to verify deduplication works independently per implementation
services.AddScoped<IRepository<User>, UserRepository>();
services.AddScoped<IRepository<Product>, ProductRepository>();

var provider = services.BuildServiceProvider();