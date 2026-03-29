using DecoWeaver.Sample;

[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 1)]
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 2)]