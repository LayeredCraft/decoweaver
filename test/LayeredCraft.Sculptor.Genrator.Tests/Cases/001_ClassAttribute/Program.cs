using Microsoft.Extensions.DependencyInjection;
using Sculptor.Sample;

var serviceProvider = new ServiceCollection()
    .AddScoped(typeof(IRepository<>), typeof(DynamoDbRepository<>))
    .BuildServiceProvider();