import 'package:flutter/material.dart';

import '../../config/theme/app_tokens.dart';
import '../constants/app_strings.dart';

/// A push that arrived while the app is on screen, as the app shows it.
class PushNotice {
  /// Title and body, one per line.
  final String text;

  /// The push's data, which decides where «مشاهده» goes.
  final Map<String, dynamic> data;

  const PushNotice({required this.text, required this.data});
}

/// The notice for a foreground push, or null when it has nothing to read (the badge still moves).
PushNotice? pushNoticeFrom({
  required String? title,
  required String? body,
  required Map<String, dynamic> data,
}) {
  final lines = [title, body].whereType<String>().map((l) => l.trim()).where((l) => l.isNotEmpty).toList();
  if (lines.isEmpty) return null;
  return PushNotice(text: lines.join('\n'), data: data);
}

/// Shows [notice] as a snackbar with a way to open what it is about.
///
/// Neither Android nor the browser shows a banner for the app in front, and taking the screen the person is using
/// would be worse than a banner — so a snackbar, which leaves the screen alone. [messenger] is null before the app
/// has a screen; then nothing is shown.
void showPushNotice(
  ScaffoldMessengerState? messenger,
  PushNotice notice, {
  required void Function(Map<String, dynamic> data) onOpen,
}) {
  if (messenger == null) return;
  messenger
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        duration: const Duration(seconds: 6),
        // Floating above the bottom bar, like every other snackbar in this app (AppSnackbar).
        behavior: SnackBarBehavior.floating,
        margin: const EdgeInsets.fromLTRB(AppSpacing.md, 0, AppSpacing.md, 104),
        content: Row(
          children: [
            const Icon(Icons.notifications_active_outlined, color: Colors.white, size: 20),
            const SizedBox(width: 8),
            Expanded(child: Text(notice.text)),
          ],
        ),
        action: SnackBarAction(label: AppStrings.pushNoticeOpen, onPressed: () => onOpen(notice.data)),
      ),
    );
}
