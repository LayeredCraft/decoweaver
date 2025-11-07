using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Should NOT generate interceptor (open generic DoNotDecorate matches closed generic)
services.AddScoped<IRepository<Customer>, SqlRepository<Customer>>();

var provider = services.BuildServiceProvider();