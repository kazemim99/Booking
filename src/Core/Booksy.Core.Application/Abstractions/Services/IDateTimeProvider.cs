using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booksy.Core.Application.Abstractions.Services
{
    /// <summary>
    /// Provides date and time services for testability
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current UTC date and time
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Gets the current local date and time
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets today's date in UTC
        /// </summary>
        DateTime UtcToday { get; }

        /// <summary>
        /// Gets today's date in local time
        /// </summary>
        DateOnly Today { get; }

        /// <summary>
        /// Gets the current Unix timestamp in seconds
        /// </summary>
        long UnixTimestamp { get; }

        /// <summary>
        /// Gets the current Unix timestamp in milliseconds
        /// </summary>
        long UnixTimestampMilliseconds { get; }

        /// <summary>
        /// Converts a DateTime to Unix timestamp
        /// </summary>
        long ToUnixTimestamp(DateTime dateTime);

        /// <summary>
        /// Converts a Unix timestamp to DateTime
        /// </summary>
        DateTime FromUnixTimestamp(long timestamp);
    }
}