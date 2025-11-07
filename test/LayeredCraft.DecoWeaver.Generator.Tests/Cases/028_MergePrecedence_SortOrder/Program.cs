using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddScoped<IRepository<User>, UserRepository>();

var provider = services.BuildServiceProvider();