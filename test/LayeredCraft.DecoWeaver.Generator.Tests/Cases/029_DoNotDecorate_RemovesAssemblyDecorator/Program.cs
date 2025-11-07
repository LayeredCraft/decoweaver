using DecoWeaver.Sample;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// DynamoDbRepository SHOULD generate interceptor with CachingRepository
services.AddScoped<IRepository<User>, DynamoDbRepository<User>>();

// SqlRepository SHOULD NOT generate interceptor (DoNotDecorate applied)
services.AddScoped<IRepository<Order>, SqlRepository<Order>>();

var provider = services.BuildServiceProvider();