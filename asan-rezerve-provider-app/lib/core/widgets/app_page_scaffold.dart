import 'package:flutter/material.dart';
import '../../config/theme/app_tokens.dart';

/// The app's standard screen anatomy (DESIGN_LANGUAGE.md §1, §4.1): a blue
/// chrome layer carrying the title and actions, with the content on a white
/// sheet that slides up over it with a rounded top edge.
///
/// Every screen uses this instead of hand-rolling a `Scaffold` with a white
/// `AppBar` — overriding the themed blue chrome per-page is what made the app
/// drift away from the design in the first place.
///
/// The sheet is a [Material] (not a coloured `Container`) so list rows and
/// ink-splash widgets inside it still paint their ripples.
class AppPageScaffold extends StatelessWidget {
  /// Plain page title. Supply [titleWidget] instead when the header needs
  /// more than text (e.g. a title plus a count).
  final String? title;
  final Widget? titleWidget;

  /// Trailing chrome actions. Use the green accent for "add" affordances —
  /// brand blue disappears against the blue header.
  final List<Widget> actions;

  /// Shown before the title. Defaults to the framework back button when the
  /// route can pop; pass [automaticallyImplyLeading] `false` for root tabs.
  final Widget? leading;
  final bool automaticallyImplyLeading;

  /// Content of the white sheet.
  final Widget body;

  final Widget? bottomNavigationBar;
  final Widget? floatingActionButton;
  final FloatingActionButtonLocation? floatingActionButtonLocation;

  /// Pinned directly under the chrome, *above* the sheet's rounded edge —
  /// for headers that scroll with neither the chrome nor the body (e.g. a
  /// week strip). Rendered on the blue.
  final Widget? chromeFooter;

  const AppPageScaffold({
    super.key,
    this.title,
    this.titleWidget,
    this.actions = const [],
    this.leading,
    this.automaticallyImplyLeading = true,
    required this.body,
    this.bottomNavigationBar,
    this.floatingActionButton,
    this.floatingActionButtonLocation,
    this.chromeFooter,
  }) : assert(title != null || titleWidget != null,
            'a page needs either a title or a titleWidget');

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.appBar,
      appBar: AppBar(
        automaticallyImplyLeading: automaticallyImplyLeading,
        leading: leading,
        title: titleWidget ??
            Text(
              title!,
              style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: Colors.white,
              ),
            ),
        actions: actions,
      ),
      body: Column(
        children: [
          ?chromeFooter,
          Expanded(
            child: Material(
              color: Colors.white,
              clipBehavior: Clip.antiAlias,
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(AppRadius.panel),
              ),
              child: SizedBox(width: double.infinity, child: body),
            ),
          ),
        ],
      ),
      // The nav pill floats on WHITE, not on the chrome: the scaffold
      // background is blue, so without this the pill's side gutters and
      // bottom inset fill with blue and it reads as a full-width bar
      // instead of the floating pill the design calls for.
      bottomNavigationBar: bottomNavigationBar == null
          ? null
          : ColoredBox(color: Colors.white, child: bottomNavigationBar),
      floatingActionButton: floatingActionButton,
      floatingActionButtonLocation: floatingActionButtonLocation,
    );
  }
}
