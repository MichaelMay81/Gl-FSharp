open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop
open OpenTK.Mathematics

// This function runs on every update frame.
let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    // Check if the Escape button is currently being pressed.
    if window.KeyboardState.IsKeyDown Keys.Escape then
        // If it is, close the window.
        window.Close ()

let nativeWindowSettings = NativeWindowSettings (
    ClientSize = Vector2i (800, 600),
    Title = $"LearnOpenTK - 1 Creating a Window",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible)

// To create a new window, initialize a GameWindow, 
let window = new GameWindow (GameWindowSettings.Default, nativeWindowSettings)
// .. connect the update function
window.add_UpdateFrame (onUpdateFrame window)
// .. then call Run() on it.
window.Run ()

// And that's it! That's all it takes to create a window with OpenTK.