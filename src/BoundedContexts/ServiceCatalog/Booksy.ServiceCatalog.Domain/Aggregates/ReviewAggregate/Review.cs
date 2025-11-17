// ========================================
// Booksy.ServiceCatalog.Domain/Aggregates/ReviewAggregate/Review.cs
// ========================================
using Booksy.Core.Domain.Abstractions.Entities;
using Booksy.Core.Domain.Base;
using Booksy.Core.Domain.Exceptions;
using Booksy.ServiceCatalog.Domain.ValueObjects;

namespace Booksy.ServiceCatalog.Domain.Aggregates
{
    /// <summary>
    /// Represents a customer review for a provider
    /// Reviews can only be created for completed bookings
    /// </summary>
    public sealed class Review : AggregateRoot<Guid>, IAuditableEntity
    {
        // Core Identity
        public ProviderId ProviderId { get; private set; }
        public UserId CustomerId { get; private set; }
        public Guid BookingId { get; private set; }
        
        // Rating (1.0 - 5.0)
        public decimal RatingValue { get; private set; }
        
        // Comment (Persian and/or English)
        public string? Comment { get; private set; }
        
        // Verification
        public bool IsVerified { get; private set; }
        
        // Provider Response
        public string? ProviderResponse { get; private set; }
        public DateTime? ProviderResponseAt { get; private set; }
        
        // Helpfulness (votes from other users)
        public int HelpfulCount { get; private set; }
        public int NotHelpfulCount { get; private set; }
        
        // Audit Properties
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
        
        // Private constructor for EF Core
        private Review() : base() { }
        
        /// <summary>
        /// Creates a new review for a completed booking
        /// </summary>
        public static Review Create(
            ProviderId providerId,
            UserId customerId,
            Guid bookingId,
            decimal ratingValue,
            string? comment = null,
            bool isVerified = true,
            string? createdBy = null)
        {
            ValidateRating(ratingValue);
            
            if (!string.IsNullOrWhiteSpace(comment))
            {
                ValidateComment(comment);
            }
            
            return new Review
            {
                Id = Guid.NewGuid(),
                ProviderId = providerId,
                CustomerId = customerId,
                BookingId = bookingId,
                RatingValue = ratingValue,
                Comment = comment?.Trim(),
                IsVerified = isVerified, // True if from actual booking
                HelpfulCount = 0,
                NotHelpfulCount = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };
        }
        
        /// <summary>
        /// Updates the review comment
        /// </summary>
        public void UpdateComment(string comment, string? modifiedBy = null)
        {
            ValidateComment(comment);
            
            Comment = comment.Trim();
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Updates the rating
        /// </summary>
        public void UpdateRating(decimal ratingValue, string? modifiedBy = null)
        {
            ValidateRating(ratingValue);
            
            RatingValue = ratingValue;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Provider responds to the review
        /// </summary>
        public void AddProviderResponse(string response, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(response))
                throw new DomainValidationException("Provider response cannot be empty");
            
            if (response.Length > 1000)
                throw new DomainValidationException("Provider response cannot exceed 1000 characters");
            
            ProviderResponse = response.Trim();
            ProviderResponseAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Updates provider response
        /// </summary>
        public void UpdateProviderResponse(string response, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(ProviderResponse))
                throw new DomainValidationException("Cannot update response that doesn't exist. Use AddProviderResponse first.");
            
            if (string.IsNullOrWhiteSpace(response))
                throw new DomainValidationException("Provider response cannot be empty");
            
            if (response.Length > 1000)
                throw new DomainValidationException("Provider response cannot exceed 1000 characters");
            
            ProviderResponse = response.Trim();
            ProviderResponseAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Removes provider response
        /// </summary>
        public void RemoveProviderResponse(string? modifiedBy = null)
        {
            ProviderResponse = null;
            ProviderResponseAt = null;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Marks review as helpful
        /// </summary>
        public void MarkAsHelpful()
        {
            HelpfulCount++;
            LastModifiedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Marks review as not helpful
        /// </summary>
        public void MarkAsNotHelpful()
        {
            NotHelpfulCount++;
            LastModifiedAt = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Verifies the review (admin action or automatic verification)
        /// </summary>
        public void Verify(string? modifiedBy = null)
        {
            IsVerified = true;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Unverifies the review (if flagged as suspicious)
        /// </summary>
        public void Unverify(string? modifiedBy = null)
        {
            IsVerified = false;
            LastModifiedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
        }
        
        /// <summary>
        /// Gets the helpfulness ratio (helpful / total votes)
        /// </summary>
        public decimal GetHelpfulnessRatio()
        {
            var totalVotes = HelpfulCount + NotHelpfulCount;
            if (totalVotes == 0) return 0;
            
            return (decimal)HelpfulCount / totalVotes;
        }
        
        /// <summary>
        /// Checks if review is considered helpful (>60% helpful ratio with minimum 5 votes)
        /// </summary>
        public bool IsConsideredHelpful()
        {
            var totalVotes = HelpfulCount + NotHelpfulCount;
            return totalVotes >= 5 && GetHelpfulnessRatio() >= 0.6m;
        }
        
        /// <summary>
        /// Gets age of review in days
        /// </summary>
        public int GetAgeInDays()
        {
            return (DateTime.UtcNow - CreatedAt).Days;
        }
        
        /// <summary>
        /// Checks if review is recent (within 30 days)
        /// </summary>
        public bool IsRecent()
        {
            return GetAgeInDays() <= 30;
        }
        
        /// <summary>
        /// Validates rating value
        /// </summary>
        private static void ValidateRating(decimal ratingValue)
        {
            if (ratingValue < 1.0m || ratingValue > 5.0m)
                throw new DomainValidationException("Rating must be between 1.0 and 5.0");
            
            // Allow only 0.5 increments (1.0, 1.5, 2.0, 2.5, etc.)
            if (ratingValue % 0.5m != 0)
                throw new DomainValidationException("Rating must be in 0.5 increments (e.g., 3.5, 4.0, 4.5)");
        }
        
        /// <summary>
        /// Validates comment
        /// </summary>
        private static void ValidateComment(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new DomainValidationException("Review comment cannot be empty");
            
            if (comment.Length < 10)
                throw new DomainValidationException("Review comment must be at least 10 characters");
            
            if (comment.Length > 2000)
                throw new DomainValidationException("Review comment cannot exceed 2000 characters");
        }
        
        public override string ToString()
        {
            return $"Review {Id}: {RatingValue}★ by Customer {CustomerId.Value} for Provider {ProviderId.Value}";
        }
    }
}
