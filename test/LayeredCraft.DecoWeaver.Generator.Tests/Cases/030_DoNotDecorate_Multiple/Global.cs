using DecoWeaver.Sample;

// Assembly declares 3 decorators for all IRepository<> implementations
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 10)]
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 20)]
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(ValidationRepository<>), 30)]