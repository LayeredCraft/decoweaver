using DecoWeaver.Sample;

[assembly: DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
