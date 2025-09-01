using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.UserManagement.Application.CQRS.Commands.UpldateUserProfile
{
  public  class UpdateUserProfileCommand : ICommand<UpdateUserProfileResult>
    {
        public UpdateUserProfileCommand(Guid id, string? firstName, string? lastName, string? phoneNumber, string? bio, string? address, string? profilePictureUrl)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            Bio = bio;
            Address = address;
            ProfilePictureUrl = profilePictureUrl;
        }

        public Guid? IdempotencyKey => throw new NotImplementedException();

        public Guid Id { get; }
        public string? FirstName { get; }
        public string? LastName { get; }
        public string? PhoneNumber { get; }
        public string? Bio { get; }
        public string? Address { get; }
        public string? ProfilePictureUrl { get; }
    }
}
