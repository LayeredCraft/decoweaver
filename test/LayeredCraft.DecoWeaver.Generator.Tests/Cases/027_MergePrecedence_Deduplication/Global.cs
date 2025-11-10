using DecoWeaver.Sample;

// Assembly-level: Caching@10 for IRepository<> and Logging@5 for IRepository<>
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), 10)]
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 5)]