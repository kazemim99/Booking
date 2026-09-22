# Spec coverage — provider-reviews-and-ratings (task 9.3)

Every scenario in the five spec files, mapped to the test that proves it. `IT` = `tests/Booksy.Host.IntegrationTests/ServiceCatalog`
(class.method), `DU` = `Booksy.ServiceCatalog.Domain.UnitTests/ReviewAggregate` (or `ProviderAggregate`), `AU` = Application/Api unit
tests, `Vue` = `booksy-frontend`, `CA` = `booksy-customer-app`, `PA` = `booksy-provider-app`.

Mapping this found seven uncovered or partly covered scenarios (plus three scenarios added later the same day with decision 7.7) and one defect (comment length checked before trimming). All were closed test-first on 2026-09-22 and are marked **new**; one web-app scenario stays uncovered and says why.

## provider-reviews

| Scenario | Test |
|---|---|
| The booking's customer reviews a completed booking | IT `SubmitReviewTests.The_bookings_customer_can_review_it_and_it_waits_for_a_moderator` |
| A different user attempts to review the booking | IT `SubmitReviewTests.Someone_who_did_not_make_the_booking_is_refused` |
| The booking has not been completed | IT `SubmitReviewTests.A_booking_that_has_not_been_completed_cannot_be_reviewed` |
| The booking already has a review | IT `SubmitReviewTests.A_second_review_of_the_same_booking_is_refused` |
| An unauthenticated caller attempts to review | IT `SubmitReviewTests.An_anonymous_caller_cannot_review` |
| Overall rating only | DU `ReviewRatingTests.Overall_only_records_every_dimension_as_absent` |
| Overall rating with a subset of dimensions | DU `ReviewRatingTests.A_subset_of_dimensions_records_those_and_leaves_the_rest_absent`; IT `SubmitReviewTests.Dimensions_are_stored_and_the_ones_left_out_stay_absent` |
| Overall rating is missing | IT `SubmitReviewTests.A_review_without_the_overall_rating_is_refused_naming_it` |
| A rating is out of range or off-increment | DU `ReviewRatingTests.An_out_of_range_or_off_increment_dimension_is_rejected_by_name`, `An_invalid_overall_is_rejected_by_name`; IT `SubmitReviewTests.An_off_increment_dimension_is_refused_naming_it` |
| Dimensions never override the customer's overall verdict | DU `ReviewRatingTests.Dimensions_never_override_the_customers_overall_verdict`; Vue `ReviewForm` "never recomputes the overall from the dimensions" |
| Review with a Persian comment | **new** DU `ReviewRatingTests.A_persian_comment_is_kept_exactly_as_written` |
| Comment below the minimum length | **new** DU `ReviewRatingTests.A_comment_shorter_than_ten_characters_after_trimming_is_refused` — **defect found**: the length was checked before trimming, so ten-plus characters of padding let a 3-character comment through and stored it; fixed in `Review.NormalizeComment` |
| Review without a comment | **new** DU `ReviewRatingTests.A_review_without_words_has_no_comment` (null, empty, whitespace → no comment; whitespace used to be stored as `""`) |
| Author edits inside the window | IT `EditReviewTests.The_author_can_edit_inside_the_window`; DU `ReviewEditTests.The_last_moment_of_the_seventh_day_is_still_inside_the_window` |
| Author edits after the window | IT `EditReviewTests.Editing_after_seven_days_is_refused_and_changes_nothing` |
| A different user attempts to edit | IT `EditReviewTests.Someone_else_cannot_edit_the_review` |
| Author attempts to edit a rejected review | IT `EditReviewTests.A_rejected_review_cannot_be_edited_back_into_the_queue` |
| Author attempts to edit a hidden review | IT `EditReviewTests.A_hidden_review_cannot_be_edited_to_undo_the_hide` |
| The owning provider replies | IT `ProviderReplyTests.The_owning_provider_replies_and_it_waits_for_a_moderator` |
| A second reply to the same review | IT `ProviderReplyTests.A_second_reply_is_a_conflict_pointing_at_edit` |
| A different provider attempts to reply | IT `ProviderReplyTests.A_different_provider_cannot_reply` |
| The review's author attempts to reply | IT `ProviderReplyTests.The_reviews_author_cannot_reply` |
| The provider removes their reply | IT `ProviderReplyTests.Removing_a_reply_clears_it` |
| First review is published | IT `ReviewModerationTests.Approving_publishes_the_review_and_the_providers_rating_moves` |
| A review is hidden by moderation | IT `ReviewModerationTests.Hiding_takes_the_review_down_and_the_rating_is_recomputed_without_it` |
| Dimension averaged only over those who rated it | DU `ProviderRatingCalculatorTests.A_dimension_is_averaged_only_over_the_reviews_that_rated_it`; IT `ProviderRatingRecomputerTests.Dimension_averages_are_written_to_the_summary_over_only_those_who_rated_them` |
| Provider with no published reviews | DU `ProviderRatingCalculatorTests.A_provider_with_no_reviews_is_unrated`; IT `ProviderRatingRecomputerTests.A_provider_whose_last_published_review_goes_is_unrated_again` |
| Pending reviews do not move the average | IT `SubmitReviewTests.A_pending_review_does_not_move_the_providers_rating` |
| Review submitted while a reminder is scheduled | IT `ReviewNotificationTests.After_reviewing_the_customers_inbox_holds_no_review_request_for_that_booking` (asserts neither `ReviewRequest` nor `ReviewReminder` remains) |
| Withdrawal happens before moderation | same test — the review is still pending when the inbox is read |

## review-engagement

| Scenario | Test |
|---|---|
| Anonymous caller attempts to vote | IT `ReviewVoteTests.An_anonymous_caller_cannot_vote_any_more`; AU `ReviewsControllerIdentityTests.A_vote_without_a_user_id_is_refused_and_nothing_is_sent` |
| Anonymous caller reads a review | AU `ReviewsControllerIdentityTests.The_public_listing_still_works_with_nobody_signed_in`; IT `ReviewListingTests.The_public_listing_returns_published_reviews_only` |
| First vote | IT `ReviewVoteTests.A_first_vote_counts_once_and_the_voter_is_told_their_vote` |
| Repeating the same vote withdraws it | IT `ReviewVoteTests.Repeating_the_same_vote_withdraws_it` |
| Changing a vote moves it | IT `ReviewVoteTests.Changing_a_vote_moves_it_and_never_double_counts` |
| Repeated requests cannot inflate a count | IT `ReviewVoteTests.Fifty_simultaneous_votes_from_one_user_leave_at_most_one_vote_and_a_count_that_matches` |
| Two users voting | IT `ReviewVoteTests.Two_people_voting_count_twice` |
| Author votes on their own review | IT `ReviewVoteTests.The_author_cannot_vote_on_their_own_review` |
| Voting on a pending review | IT `ReviewVoteTests.A_review_awaiting_moderation_cannot_be_voted_on` |
| A published review is later hidden | DU `ReviewVisibilityEventTests.Restoring_keeps_the_votes_it_had` |
| Reader who has voted | IT `ReviewListingTests.A_signed_in_reader_is_told_their_own_vote_and_an_anonymous_one_is_not` |
| Reader who has not voted | **new** assertion in the same test: a signed-in reader who has not voted gets no `myVote` (it used to check only an anonymous reader, which the scenario is not about; passed first run — coverage, not a defect) |
| Migration preserves existing counts | IT `ReviewModerationMigrationTests.Existing_reviews_and_replies_are_published_and_their_counters_survive_as_the_baseline` |
| New vote adds to the baseline | IT `ReviewVoteTests.A_new_vote_adds_to_the_legacy_baseline_and_the_ratio_is_over_both` |
| Withdrawing a vote cannot erode the baseline | IT `ReviewVoteTests.Withdrawing_a_vote_never_takes_the_count_below_the_baseline` |
| A review created after the freeze can become helpful | IT `ReviewVoteTests.A_review_written_after_the_freeze_can_become_helpful_on_live_votes_alone` |
| The helpfulness ratio reflects live votes | IT `ReviewVoteTests.A_new_vote_adds_to_the_legacy_baseline_and_the_ratio_is_over_both`; DU `ReviewHelpfulnessPolicyTests.*` |
| Sorting by helpfulness responds to new votes | **new** IT `ReviewVoteTests.Sorting_by_helpfulness_puts_a_review_with_more_live_votes_above_a_baseline` (passed first run: the sort already summed baseline + live; this was a coverage gap, not a defect) |

## review-moderation

| Scenario | Test |
|---|---|
| Newly submitted review is not public | IT `SubmitReviewTests.The_bookings_customer_can_review_it_and_it_waits_for_a_moderator` |
| Author sees their own pending review | IT `ReviewListingTests.The_author_sees_a_pending_review_marked_as_awaiting_approval` |
| Owning provider sees a pending review about them | IT `ReviewListingTests.The_owning_provider_sees_every_state_in_their_inbox` |
| Approved review becomes public | IT `ReviewModerationTests.Approving_publishes_the_review_and_the_providers_rating_moves` |
| Rejected review stays invisible | IT `ReviewModerationTests.Rejecting_keeps_the_review_out_of_public_view_and_out_of_the_rating` |
| Public listing returns published reviews only | IT `ReviewListingTests.The_public_listing_returns_published_reviews_only` |
| Hiding a review does not unverify it | DU `ReviewModerationTests.Hiding_a_verified_review_leaves_it_verified` |
| Verified review count counts published verified reviews | IT `ReviewListingTests.Public_statistics_are_computed_over_published_reviews_only` (`verifiedReviews` = 1) |
| Queue lists pending items oldest first | IT `ReviewModerationTests.The_queue_lists_pending_reviews_oldest_first_with_what_a_moderator_needs`; admin `useReviewModeration` "loads the pending queue … oldest first" |
| Non-administrator attempts to view the queue | IT `ReviewModerationTests.A_customer_cannot_see_the_queue` |
| Provider attempts to moderate a review about their own business | IT `ReviewModerationTests.The_owning_provider_cannot_moderate_a_review_of_their_own_business` |
| Hiding a published review | IT `ReviewModerationTests.Hiding_takes_the_review_down_and_the_rating_is_recomputed_without_it` |
| Published review is edited | IT `EditReviewTests.Editing_a_published_review_takes_it_out_of_the_rating_until_it_is_approved_again` |
| Edited review carries a published reply | IT `ProviderReplyTests.Editing_a_review_that_carries_a_published_reply_takes_the_reply_back_to_pending` |
| Re-publication is distinguishable from first publication | DU `ReviewVisibilityEventTests.Approving_an_edited_review_is_marked_as_a_republication`; IT `ReviewNotificationTests.Approving_an_edited_review_is_announced_as_a_change_not_as_a_new_review` |
| Administrator restores a hidden review | IT `ReviewModerationTests.Restoring_a_hidden_review_brings_it_back_into_the_rating` |
| Hidden reviews are findable | IT `ReviewModerationTests.Hidden_reviews_are_found_on_their_own_filter_with_the_reason` |
| A rejected review cannot be restored | IT `ReviewModerationTests.A_rejected_review_cannot_be_restored` |
| Author lists their reviews | IT `ReviewListingTests.The_author_sees_their_reviews_in_every_state_with_the_reason` |
| Author sees why a review was rejected | same test |
| The list is scoped to the caller | same test (another customer's review is absent); AU `ReviewsControllerIdentityTests.The_own_review_list_is_the_nameidentifiers` |
| Owning provider lists reviews of their business | IT `ReviewListingTests.The_owning_provider_sees_every_state_in_their_inbox` |
| A different provider attempts the same listing | IT `ReviewListingTests.Another_provider_cannot_open_the_inbox` |
| The public listing stays public for the owner too | IT `ReviewListingTests.The_owning_providers_own_token_gets_nothing_extra_from_the_public_listing` |
| Statistics exclude unpublished reviews | IT `ReviewListingTests.Public_statistics_are_computed_over_published_reviews_only` |
| Hiding a review moves the statistics | IT `ReviewModerationTests.Hiding_takes_the_review_down_and_the_rating_is_recomputed_without_it` |
| Reply is not public until approved | IT `ReviewListingTests.A_reply_is_shown_publicly_only_once_approved`; `ProviderReplyTests.A_pending_reply_is_in_the_queue_and_approving_it_publishes_it` |
| Rejected reply leaves the review published | IT `ProviderReplyTests.A_rejected_reply_leaves_the_review_published` |
| Provider reports a review as abusive | IT `ReviewReportTests.The_owning_provider_can_report_a_review_of_their_business` |
| Reporting does not auto-hide | IT `ReviewReportTests.A_reported_review_stays_public_and_reaches_the_administrator_with_who_and_why` |
| Duplicate report by the same user | IT `ReviewReportTests.The_same_user_cannot_report_the_same_review_twice` |
| Unauthenticated caller attempts to report | IT `ReviewReportTests.An_anonymous_caller_cannot_report` |
| Administrator reviews what has been reported | IT `ReviewReportTests.The_most_reported_reviews_come_first` |
| A review is rejected (7.7) | IT `ReviewNotificationTests.Rejecting_a_review_tells_its_author_why_and_tells_the_salon_nothing`; AU `ReviewNotificationCopyTests.A_rejected_review_tells_its_author_by_name_and_gives_the_reason` |
| A published review is hidden (7.7) | IT `ReviewNotificationTests.Hiding_a_published_review_tells_its_author_nothing` |
| The moderator knows who reads the reason (7.7) | admin `useReviewModeration.spec.ts` "says who will read the reason, per decision" |
| Existing review after migration | IT `ReviewModerationMigrationTests.Existing_reviews_and_replies_are_published_and_their_counters_survive_as_the_baseline` |
| Existing provider replies after migration | same test |

## customer-discovery-journey (MODIFIED)

| Scenario | Test |
|---|---|
| Debounced typing / No results | unchanged by this change; pre-existing `booksy-customer-app` `search_bloc_test.dart` |
| Rated provider in results | IT `ProviderSearchRatingTests.A_rated_provider_comes_back_with_its_average_and_its_real_review_count`; CA `widgets_test.dart` "a rated provider shows its average and published count" |
| Unrated provider in results | IT `ProviderSearchRatingTests.An_unrated_provider_comes_back_with_zero_reviews_and_a_numeric_average`; CA `widgets_test.dart` "a provider with no published reviews says so instead of a zero"; CA `provider_detail_page_test.dart` "shows no image, no rating and no price band" |
| Sorting by rating descending | IT `ProviderSearchRatingTests.Highest_first_puts_every_unrated_provider_after_every_rated_one` |
| Sorting by rating ascending | IT `ProviderSearchRatingTests.Lowest_first_still_puts_unrated_providers_last_not_first` |
| Review count is real | IT `ProviderSearchRatingTests.The_provider_detail_customers_open_carries_the_real_count` and the three sibling `…carries_the_real_count` tests |

## customer-profile (MODIFIED) — the Vue web app's "نظرات من"

| Scenario | Test |
|---|---|
| Customer views their reviews | Vue `customer-reviews.service.spec.ts` "lists my reviews from the reviews API"; `CustomerReviewCard.spec.ts`; IT `ReviewListingTests.The_authors_list_names_the_salon_and_the_service_they_reviewed` |
| Customer edits recent review | Vue `ReviewForm.spec.ts` "opens pre-filled for an edit…"; `customer-reviews.service.spec.ts` "edits through the reviews API with the dimensions"; **new** `ReviewsModal.spec.ts` "says the edited review is saved and awaiting approval" (the toast used to say only "updated successfully") |
| Customer tries to edit old review | **new** Vue `CustomerReviewCard.spec.ts` "a published review past the edit window says why it can no longer be edited" (the message used to appear only as an error after an edit attempt the card never offered) |
| Customer tries to edit a rejected review | Vue `CustomerReviewCard.spec.ts` "shows a rejected review as rejected, with the reason, and offers no edit"; **new** "a rejected review is explained by its rejection, not by the edit window" |
| Customer sees a review awaiting approval | Vue `CustomerReviewCard.spec.ts` "marks a review that is awaiting approval as such" |
| Customer has no reviews | **not covered by a test.** `ReviewsModal.vue` renders the empty title and description; the scenario's "جستجوی خدمات" button is absent from the modal. Left as-is: the empty state predates this change and the button is a navigation affordance the change does not touch. |

The customer app mirrors this list in `MyReviewsPage` (`review_engagement_test.dart` "my reviews" group), but the scenarios name the web modal, so they are mapped there.
