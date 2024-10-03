open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop

open LearnOpenTK

let nativeWindowSettings = NativeWindowSettings (
    ClientSize = Vector2i (800, 600),
    Title = "LearnOpenTK - Transformations",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible)

let window = new GameWindow (GameWindowSettings.Default, nativeWindowSettings)
Window.setup window

window.Run ()