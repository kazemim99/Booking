using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Core.Domain.Infrastructure.Services
{
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current user's ID
        /// </summary>
        string? GetUserId();

        /// <summary>
        /// Gets the current user's email
        /// </summary>
        string? GetUserEmail();

        /// <summary>
        /// Gets the current user's full name
        /// </summary>
        string? GetUserName();

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        bool IsAuthenticated();

        /// <summary>
        /// Gets all claims for the current user
        /// </summary>
        IEnumerable<Claim> GetUserClaims();

        /// <summary>
        /// Checks if the current user has a specific role
        /// </summary>
        bool IsInRole(string role);

        /// <summary>
        /// Gets the current user's IP address
        /// </summary>
        string? GetUserIpAddress();


    }
}
