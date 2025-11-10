using DecoWeaver.Sample;

// Assembly-level: Logging@10 for IRepository<>
[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 10)]