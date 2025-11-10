using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// SqlRepository: Should NOT generate interceptor (DoNotDecorate applied)
services.AddScoped<IRepository<Customer>, SqlRepository<Customer>>();

// DynamoDbRepository: SHOULD generate interceptor with CachingRepository
services.AddScoped<IRepository<Order>, DynamoDbRepository<Order>>();

var provider = services.BuildServiceProvider();