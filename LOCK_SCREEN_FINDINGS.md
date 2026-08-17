# Multi-monitor lock-screen findings

## Executive summary

The application must distinguish between two Windows surfaces:

1. The **screen-saver surface**, where a normal or registered screen-saver application can display full-screen content on multiple monitors.
2. The **Windows secure lock screen**, where Windows controls the desktop used for PIN, password, and Windows Hello authentication.

Our ordinary WPF windows can control the first surface, but they cannot remain visible above the second surface after Windows switches to the secure desktop.

## What is possible

We can reliably build a multi-monitor screen-saver experience that:

- Displays one full-screen window per monitor.
- Shows a different image on each monitor.
- Uses the monitor coordinates and dimensions reported by `MonitorService`.
- Dismisses on keyboard or mouse input.
- Activates before or during the configured screen-saver transition.
- Lets Windows handle authentication after the screen saver exits.

Microsoft documents support for screen savers on multiple monitors. A screen saver can display on all monitors, and custom monitor handling can be used to display different screen-saver content per monitor.

Reference: [Multiple Monitor Applications on Different Systems](https://learn.microsoft.com/en-us/windows/win32/gdi/multiple-monitor-applications-on-different-systems)

## What is not possible with ordinary WPF windows

When the user presses `Win + L`, Windows changes to the secure desktop. The normal user desktop is no longer the visible surface, and ordinary WPF windows cannot stay above the Windows PIN, password, or Hello interface.

Therefore, this sequence is expected:

```text
Our screen-saver images on all monitors
                ↓
Windows switches to the secure desktop
                ↓
Windows PIN / password / Hello screen
```

Pressing `Win + L` does not make the current WPF preview appear on the secure lock screen. The current application cannot replace that secure desktop with independent per-monitor windows.

## Native Windows lock-screen background

Windows has supported mechanisms for configuring a lock-screen image. These mechanisms configure an OS-managed lock-screen background; they do not provide a normal desktop application with arbitrary per-monitor lock-screen windows.

The documented configuration accepts a lock-screen image path or URL. Image cropping and scaling are handled by Windows, and behavior can vary by Windows edition, policy, and configuration.

References:

- [Configure the desktop and lock-screen backgrounds](https://learn.microsoft.com/en-us/windows/configuration/background/)
- [Personalization CSP](https://learn.microsoft.com/en-us/windows/client-management/mdm/personalization-csp)
- [Lock screen personalization sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-universal-samples/personalization/)

## What DisplayFusion does

DisplayFusion does not keep ordinary WPF windows above the secure desktop. It uses Windows integration in two related areas:

- It manages a real multi-monitor screen saver and can use a different screen saver on each monitor.
- It can configure the native Windows lock-screen image.
- It provides an option to select a screen saver for the Windows lock-screen state when no user is logged in.

This explains why DisplayFusion can appear to provide more lock-screen functionality than the current prototype. It integrates with Windows' screen-saver and lock-screen mechanisms rather than simply placing normal desktop windows above `Win + L`.

DisplayFusion also documents limitations around spanning the Windows lock screen beyond the primary monitor. Its behavior should not be interpreted as proof that an ordinary application can place independent windows on the secure desktop.

References:

- [DisplayFusion multi-monitor screen savers](https://www.displayfusion.com/features/screensavers/)
- [DisplayFusion beginner's guide](https://www.displayfusion.com/HelpGuide/DisplayFusionBeginnersGuide/)
- [DisplayFusion lock-screen limitation discussion](https://www.displayfusion.com/Discussions/View/before-i-buy-displayfusion-i-need-to-know-can-i-do-following/?ID=019c10ca-1b59-77fa-b897-7542b36978cd)

## Current prototype behavior

The current project contains:

- `MonitorService`, which enumerates monitors and provides monitor bounds.
- `LockScreenService`, which creates one borderless WPF window per monitor.
- `LockScreenWindow`, which displays the configured image and dismisses on input.
- A simple UI for selecting an image per monitor.
- A **Test Lock Screen** button for immediate preview.
- An optional timeout mode that activates the preview shortly before the configured Windows screen-saver timeout.

The current **Save** action applies the selected image paths to the running process. It does not yet persist those paths after the application closes.

The old `SecondaryBarWindow` files remain in the project for reference, but the active `MainWindow` no longer creates or uses them.

## Correct way to test

### Immediate preview

1. Start the application.
2. Select an image for each monitor.
3. Click **Save**.
4. Click **Test Lock Screen**.
5. Move the mouse or press a key to dismiss the preview.

### Timeout preview

1. Check **Activate after the Windows screen-saver timeout**.
2. Click **Save**.
3. Configure a short Windows screen-saver timeout.
4. Stop moving the mouse and typing.

The preview can appear during the screen-saver transition. Once Windows takes over the secure desktop, the WPF images disappear and Windows shows its own authentication surface.

### Actual Windows lock

Pressing `Win + L` tests the real secure lock screen. The current WPF images are not expected to remain visible after this transition.

## Recommended next architecture

To behave more like a multi-monitor screen-saver product, the next implementation should convert the preview into a real Windows screen saver:

1. Add screen-saver command-line handling, especially `/s` for screen-saver mode.
2. Package or register the application as a `.scr` screen-saver executable.
3. Allow Windows to launch it from its screen-saver settings.
4. Create one full-screen surface per monitor from the existing `MonitorService` data.
5. Preserve per-monitor image configuration.
6. Support screen-saver preview and exit behavior expected by Windows.
7. Separately investigate native Windows lock-screen background configuration.

The real screen saver can provide per-monitor images before authentication. It still cannot replace or overlay the Windows secure PIN/Hello desktop.

## Final conclusion

The desired behavior is partly achievable:

- **Different wallpapers on all monitors during the screen-saver phase:** yes.
- **Windows authentication after the screen saver:** yes, Windows remains responsible for it.
- **Independent custom WPF wallpapers remaining visible on every monitor after `Win + L`:** no, not through the normal desktop-window model.
- **A native Windows lock-screen background:** supported, but OS-managed and not equivalent to arbitrary per-monitor WPF windows.
