import 'package:booksy_customer_app/core/push/push_click_message.dart';
import 'package:flutter_test/flutter_test.dart';

/// A notification tapped while the app is already open in a tab reaches the page as a message from the push service
/// worker (web/push/firebase-messaging-sw.js), carrying the push's data. The page hears messages from every service
/// worker of the site, so only ours, in the shape it sends, may move the screen.
void main() {
  test('our tap message yields the push data', () {
    final data = pushClickData({
      'type': pushClickMessageType,
      'data': {'bookingId': 'b1', 'notificationId': 'n1'},
    });

    expect(data, {'bookingId': 'b1', 'notificationId': 'n1'});
  });

  test('the type the worker sends is the one the page listens for', () {
    // web/push/firebase-messaging-sw.js posts this literal; renaming one side alone would silently drop every tap.
    expect(pushClickMessageType, 'booksy-push-open');
  });

  test('a tap with no data still opens something (the inbox decides)', () {
    expect(pushClickData({'type': pushClickMessageType}), isEmpty);
  });

  test('other messages are ignored', () {
    expect(pushClickData(null), isNull);
    expect(pushClickData('booksy-push-open'), isNull);
    expect(pushClickData({'type': 'push-received', 'data': {'bookingId': 'b1'}}), isNull,
        reason: "Firebase's own foreground message must not be taken for a tap");
    expect(pushClickData({'data': {'bookingId': 'b1'}}), isNull);
  });

  test('values arrive as text, whatever the worker passed', () {
    expect(pushClickData({'type': pushClickMessageType, 'data': {'bookingId': 42}}), {'bookingId': '42'});
  });
}
