using AutoMapper;
using BDayServer.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace BDayServer.UnitTests.TestUtilities;

internal static class TestMapper
{
    public static IMapper Create() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();
}
