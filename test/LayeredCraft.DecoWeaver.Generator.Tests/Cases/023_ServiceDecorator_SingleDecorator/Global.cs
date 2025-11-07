using DecoWeaver.Sample;

[assembly: DecoWeaver.Attributes.DecorateService(typeof(DynamoDbRepository<>), typeof(CachingRepository<>))]