using DecoWeaver.Sample;

[assembly: LayeredCraft.DecoWeaver.Attributes.DecorateService(typeof(IRepository<>), typeof(CachingRepository<>))]
