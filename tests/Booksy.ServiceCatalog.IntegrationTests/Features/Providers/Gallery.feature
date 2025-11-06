Feature: Gallery Management
  As a provider
  I want to manage my business gallery
  So that customers can see my work and facility

  Background:
    Given a provider "Beauty Salon" exists with active status
    And I am authenticated as the provider

  @smoke @gallery @upload
  Scenario: Upload gallery images
    When I upload 3 valid images to the gallery
    Then the response status code should be 200
    And the response should contain 3 image URLs
    And each image should have thumbnail, medium, and original sizes
    And the images should be displayed in order

  @gallery @upload @multiple
  Scenario: Upload multiple images at once
    When I upload 5 images simultaneously
    Then all 5 images should be uploaded successfully
    And display order should be assigned automatically

  @gallery @upload @negative @too-many
  Scenario: Cannot upload more than maximum allowed
    When I try to upload 11 images at once
    Then the response status code should be 400
    And the error should indicate maximum 10 images allowed

  @gallery @upload @negative @file-size
  Scenario: Cannot upload image exceeding size limit
    When I try to upload an image of 12MB
    Then the response status code should be 400
    And the error should indicate file size limit exceeded

  @gallery @upload @negative @invalid-format
  Scenario: Cannot upload invalid file format
    When I try to upload a PDF file as gallery image
    Then the response status code should be 400
    And the error should indicate invalid file format

  @gallery @view
  Scenario: View all gallery images
    Given the provider has 5 gallery images
    When I send a GET request to "/api/v1/providers/{providerId}/gallery"
    Then the response status code should be 200
    And the response should contain 5 images in correct order

  @gallery @metadata @update
  Scenario: Update image metadata
    Given the provider has a gallery image
    When I send a PUT request to update image metadata:
      | Field       | Value                  |
      | Caption     | Beautiful haircut      |
      | Description | Client transformation  |
      | Tags        | haircut, styling       |
    Then the response status code should be 200
    And the metadata should be updated

  @gallery @reorder
  Scenario: Reorder gallery images
    Given the provider has 4 gallery images
    When I send a PUT request to reorder images:
      | ImageId | NewOrder |
      | img-1   | 3        |
      | img-2   | 1        |
      | img-3   | 2        |
      | img-4   | 4        |
    Then the response status code should be 200
    And the images should be reordered correctly

  @gallery @delete @single
  Scenario: Delete a gallery image
    Given the provider has 5 gallery images
    When I send a DELETE request to remove one image
    Then the response status code should be 200
    And the provider should have 4 gallery images
    And remaining images should be reordered

  @gallery @delete @multiple
  Scenario: Delete multiple gallery images
    Given the provider has 10 gallery images
    When I send a DELETE request to remove 3 images
    Then the response status code should be 200
    And the provider should have 7 gallery images

  @gallery @featured
  Scenario: Set an image as featured
    Given the provider has multiple gallery images
    When I mark an image as featured
    Then the response status code should be 200
    And the image should be marked as featured
    And it should appear first in the gallery

  @gallery @featured @single
  Scenario: Only one image can be featured
    Given an image is already featured
    When I mark a different image as featured
    Then the previous featured image should be unfeatured
    And the new image should be featured

  @gallery @negative @unauthorized
  Scenario: Cannot upload to another provider's gallery
    Given another provider "Competitor" exists
    When I try to upload an image to the competitor's gallery
    Then the response status code should be 403

  @gallery @negative @unauthorized @delete
  Scenario: Cannot delete another provider's gallery images
    Given another provider has gallery images
    When I try to delete their images
    Then the response status code should be 403

  @gallery @public-view
  Scenario: Customer views provider gallery
    Given I am not authenticated
    And the provider has 6 public gallery images
    When I send a GET request to view the gallery
    Then the response status code should be 200
    And all 6 images should be visible

  @gallery @image-processing
  Scenario: Uploaded images are automatically processed
    When I upload a high-resolution image
    Then the response status code should be 200
    And a thumbnail version should be generated
    And a medium-sized version should be generated
    And the original should be preserved

  @gallery @storage
  Scenario: Images are stored with proper paths
    When I upload a gallery image
    Then the image should be stored at "/uploads/providers/{providerId}/gallery/"
    And the URL should be publicly accessible
