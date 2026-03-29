using LayeredCraft.DecoWeaver.Attributes;

[assembly: DecorateService(typeof(IRepository<>), typeof(CachingRepository<>), order: 50)]