
using AnAspect.Mediator.Abstractions;
using AnAspect.Mediator.Registration;
using AnAspect.Mediator.Tests.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AnAspect.Mediator.Tests;

public class ValueTaskMediatorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly TestTracker _tracker = new();

    public ValueTaskMediatorTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_tracker);
        services.AddMediator(typeof(CreateUserHandler).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }
    
    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task SendAsync_BasicRequest_ReturnsResponse()
    {
        // Arrange
        var command = new CreateUserCommand("Test User", "test@example.com");

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task SendAsync_CustomQueryInterface_Works()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserQuery(userId);

        // Act
        var result = await _mediator.SendAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task SendAsync_CustomCommandInterface_Works()
    {
        // Arrange
        var command = new DeleteUserCommand(Guid.NewGuid());

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SendAsync_RequestWithoutResponse_ReturnsUnit()
    {
        // Arrange
        var command = new LogMessageCommand("Hello World");

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Equal("Hello World", LogMessageHandler.LastMessage);
    }

    [Fact]
    public async Task SendAsync_AbstractBaseHandler_ExecutesProperly()
    {
        // Arrange
        UpdateUserHandler.TransactionCommitted = false;
        var command = new UpdateUserCommand(Guid.NewGuid(), "Updated Name");

        // Act
        var result = await _mediator.SendAsync(command);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        Assert.True(UpdateUserHandler.TransactionCommitted, "Transaction should be committed");
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _mediator.SendAsync<UserDto>(null!).AsTask());
    }
}
