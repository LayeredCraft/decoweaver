using DecoWeaver.Sample;

[assembly: DecoWeaver.Attributes.DecorateService(typeof(DynamoDbRepository<>), typeof(CachingRepository<>), 1)]
[assembly: DecoWeaver.Attributes.DecorateService(typeof(DynamoDbRepository<>), typeof(LoggingRepository<>), 2)]