# Unity 2022.3.28f1 Game view scale

Read-only inspection of Main `UserSettings/Layouts/default-2022.dwlt` and installed `UnityEditor.CoreModule.dll`, 2026-10-16.

The saved Free Aspect view has low resolution for aspect ratios enabled, target706×442.8, logical draw706×442.8, scale1.25, centered translation353×221.4. The physical window size882.5 is exactly706×1.25.

Installed `UnityEditor.GameView.get_minScale` applies `Mathf.Max(fitScale, EditorGUIUtility.pixelsPerPoint)` when low-resolution Free Aspect is enabled. `ScaleThatFitsTargetInView` first converts target pixels to points; `ConfigureZoomArea` enforces this minimum. At Windows125% DPI the displayed1.3 is the rounded1.25 fit scale. It does not independently establish cropping and should not be forcibly changed to1 or translation0.

No layout or Assets change was made. Actual camera framing, native UI and viewport checks remain the appropriate evidence. Review the visible Editor after batch validation before altering any user layout.
