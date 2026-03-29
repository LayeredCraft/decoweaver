using DecoWeaver.Sample;

// Assembly-level: Logging@10 for IRepository<>
[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(LoggingRepository<>), 10)]