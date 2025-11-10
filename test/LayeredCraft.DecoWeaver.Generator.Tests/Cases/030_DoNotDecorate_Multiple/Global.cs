using DecoWeaver.Sample;

// Assembly declares 3 decorators for all IRepository<> implementations
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 10)]
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 20)]
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(ValidationRepository<>), 30)]