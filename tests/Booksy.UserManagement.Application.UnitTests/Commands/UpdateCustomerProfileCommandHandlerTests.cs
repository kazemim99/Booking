using Booksy.Core.Domain.ValueObjects;
using Booksy.UserManagement.Application.CQRS.Commands.Customer.UpdateCustomerProfile;
using Booksy.UserManagement.Domain.Aggregates;
using Booksy.UserManagement.Domain.Aggregates.CustomerAggregate;
using Booksy.UserManagement.Domain.Entities;
using Booksy.UserManagement.Domain.Enums;
using Booksy.UserManagement.Domain.Repositories;
using Booksy.UserManagement.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Booksy.UserManagement.Application.UnitTests.Commands;

public class UpdateCustomerProfileCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateCustomerProfileCommandHandler> _logger;
    private readonly UpdateCustomerProfileCommandHandler _handler;

    public UpdateCustomerProfileCommandHandlerTests()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _logger = Substitute.For<ILogger<UpdateCustomerProfileCommandHandler>>();
        _handler = new UpdateCustomerProfileCommandHandler(_customerRepository, _userRepository, _logger);
    }

    private static User CreateUser(string firstName, string lastName)
        => User.Register(
            Email.Create("john.doe@example.com"),
            HashedPassword.FromHash("hash"),
            UserProfile.Create(firstName, lastName),
            UserType.Customer);

    [Fact]
    public async Task Handle_Should_Update_Customer_Profile_Successfully()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var user = CreateUser("John", "Doe");
        var customer = Customer.Create(user.Id, user.Profile);

        var command = new UpdateCustomerProfileCommand
        {
            CustomerId = customerId,
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+989123456789"
        };

        _customerRepository.GetByIdAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>())
            .Returns(customer);
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Contain("Jane");
        result.FullName.Should().Contain("Smith");

        await _userRepository.Received(1).UpdateAsync(
            Arg.Any<User>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Customer_Not_Found()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new UpdateCustomerProfileCommand
        {
            CustomerId = customerId,
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "+989123456789"
        };

        _customerRepository.GetByIdAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>())
            .Returns((Customer?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Customer not found with ID: {customerId}");

        await _customerRepository.DidNotReceive().UpdateAsync(
            Arg.Any<Customer>(),
            Arg.Any<CancellationToken>());
    }
}
