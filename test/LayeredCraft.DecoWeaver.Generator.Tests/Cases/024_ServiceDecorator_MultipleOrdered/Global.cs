using DecoWeaver.Sample;

[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 1)]
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 2)]