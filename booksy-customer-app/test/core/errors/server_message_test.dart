import 'package:booksy_customer_app/core/errors/server_message.dart';
import 'package:flutter_test/flutter_test.dart';

/// The customer's words from any of the API's error shapes
/// (openspec/changes/_inline/customer-reviews-and-nahal-seed).
void main() {
  test('the host envelope: its message', () {
    expect(
      serverMessage({
        'success': false,
        'message': 'به نظر خودتان نمی‌توانید رأی بدهید.',
        'error': {'code': 'ACCESS_FORBIDDEN', 'message': 'به نظر خودتان نمی‌توانید رأی بدهید.'},
      }),
      'به نظر خودتان نمی‌توانید رأی بدهید.',
    );
  });

  test('a controller\'s list of errors: the first one\'s message, never the map printed', () {
    expect(
      serverMessage({
        'success': false,
        'errors': [
          {'code': 'ERR_FORBIDDEN', 'message': 'فقط برای نوبت‌های خودتان می‌توانید نظر ثبت کنید.', 'field': null}
        ],
      }),
      'فقط برای نوبت‌های خودتان می‌توانید نظر ثبت کنید.',
    );
  });

  test('a validation failure: the field\'s words, not the English headline', () {
    expect(
      serverMessage({
        'message': "Validation failed for property 'Rating': امتیاز کلی باید بین ۱ تا ۵ ستاره باشد.",
        'error': {
          'errors': {
            'Rating': ['امتیاز کلی باید بین ۱ تا ۵ ستاره باشد.']
          }
        },
      }),
      'امتیاز کلی باید بین ۱ تا ۵ ستاره باشد.',
    );
  });

  test('a state refusal keeps its message, not the metadata beside it', () {
    expect(
      serverMessage({
        'message': 'فقط به نظرهای منتشرشده می‌توانید رأی بدهید.',
        'error': {
          'errors': {
            'aggregateName': ['Review']
          }
        },
      }),
      'فقط به نظرهای منتشرشده می‌توانید رأی بدهید.',
    );
  });

  test('nothing to say is null', () {
    expect(serverMessage(null), isNull);
    expect(serverMessage('<html>'), isNull);
    expect(serverMessage({'success': false}), isNull);
    expect(serverMessage({'message': '  '}), isNull);
  });
}
