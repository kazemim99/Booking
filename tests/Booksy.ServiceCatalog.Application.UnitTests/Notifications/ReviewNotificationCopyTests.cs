using Booksy.ServiceCatalog.Application.Services.Notifications;
using Booksy.ServiceCatalog.Domain.Enums;

namespace Booksy.ServiceCatalog.Application.UnitTests.Notifications;

/// <summary>The wording of the four review notifications.</summary>
public class ReviewNotificationCopyTests
{
    private readonly PersianNotificationCopyWriter _writer = new();

    private static Dictionary<string, string> With(string? rating = null, string? reason = null)
    {
        var parameters = new Dictionary<string, string>
        {
            [NotificationParameter.BusinessName] = "سالن رز",
            [NotificationParameter.CustomerName] = "مریم",
        };
        if (rating is not null) parameters[NotificationParameter.Rating] = rating;
        if (reason is not null) parameters[NotificationParameter.Reason] = reason;
        return parameters;
    }

    [Theory]
    [InlineData("4.5", "۴٫۵ از ۵")]
    [InlineData("5", "۵ از ۵")]
    [InlineData("1.0", "۱ از ۵")]
    public void A_published_review_tells_the_salon_its_rating_in_persian_digits(string rating, string expected)
    {
        var copy = _writer.Write(NotificationEventCode.ReviewPublished, With(rating));

        Assert.Contains(expected, copy.Body);
        Assert.Contains("سالن رز", copy.Body);
    }

    [Fact]
    public void A_changed_review_is_worded_as_a_change_not_as_a_new_review()
    {
        var published = _writer.Write(NotificationEventCode.ReviewPublished, With("3"));
        var republished = _writer.Write(NotificationEventCode.ReviewRepublished, With("3"));

        Assert.NotEqual(published.Subject, republished.Subject);
        Assert.Contains("تغییر", republished.Body);
    }

    [Fact]
    public void A_missing_rating_reads_neutrally_rather_than_printing_a_wrong_number()
    {
        var copy = _writer.Write(NotificationEventCode.ReviewPublished, With());

        Assert.DoesNotContain("۰", copy.Body);
        Assert.False(string.IsNullOrWhiteSpace(copy.Body));
    }

    [Fact]
    public void A_published_reply_is_addressed_to_the_customer_by_name()
    {
        var copy = _writer.Write(NotificationEventCode.ReviewReplyPublished, With());

        Assert.Contains("مریم", copy.Body);
        Assert.Contains("سالن رز", copy.Body);
    }

    // Task 7.7, decided 2026-09-22: the author IS told their review was rejected, and told the reason.

    [Fact]
    public void A_rejected_review_tells_its_author_by_name_and_gives_the_reason()
    {
        var copy = _writer.Write(NotificationEventCode.ReviewRejected, With(reason: "حاوی شماره تماس است"));

        Assert.Contains("مریم", copy.Body);
        Assert.Contains("حاوی شماره تماس است", copy.Body);
        Assert.Contains("سالن رز", copy.Body);
    }

    [Fact]
    public void A_rejection_without_a_recorded_reason_still_reads_as_a_whole_sentence()
    {
        // The reason is required at the API, but wording must never depend on a parameter arriving.
        var copy = _writer.Write(NotificationEventCode.ReviewRejected, With());

        Assert.EndsWith(".", copy.Body);
        Assert.DoesNotContain(":", copy.Body);
    }

    [Fact]
    public void A_rejection_is_not_worded_as_a_publication()
    {
        var rejected = _writer.Write(NotificationEventCode.ReviewRejected, With(reason: "نامرتبط"));
        var published = _writer.Write(NotificationEventCode.ReviewPublished, With("4"));

        Assert.NotEqual(published.Subject, rejected.Subject);
        Assert.DoesNotContain("منتشر شد", rejected.Body);
    }
}
