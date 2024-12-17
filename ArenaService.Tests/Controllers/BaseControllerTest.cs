using Moq;

namespace ArenaService.Tests.Controllers;

public abstract class BaseControllerTest<TRepository, TService>
    where TRepository : class
    where TService : class
{
    protected Mock<TRepository> RepositoryMock { get; private set; }
    protected TService Service { get; private set; }

    protected BaseControllerTest()
    {
        RepositoryMock = new Mock<TRepository>();
        Service = CreateService(RepositoryMock.Object);
    }

    protected abstract TService CreateService(TRepository repository);
}
