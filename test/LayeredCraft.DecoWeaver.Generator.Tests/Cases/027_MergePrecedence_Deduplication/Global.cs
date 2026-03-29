using DecoWeaver.Sample;

// Assembly-level: Caching@10 for IRepository<> and Logging@5 for IRepository<>
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 10)]
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 5)]