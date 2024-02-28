open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop
open OpenTK.Mathematics

// type Window (gameWindowSettings:GameWindowSettings, nativeWindowSettings:NativeWindowSettings) =
//     inherit GameWindow (gameWindowSettings, nativeWindowSettings)
//     override this.OnUpdateFrame (e:FrameEventArgs) =
//         if this.KeyboardState.IsKeyDown Keys.Escape then
//             this.Close ()
//         else
//             base.OnUpdateFrame e

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let projectName = System.Reflection.Assembly.GetCallingAssembly().GetName().Name
let title = $"LearnOpenTK - {projectName}"
printfn $"{title}"
printfn $"Current directory: {System.IO.Directory.GetCurrentDirectory()}"

let nativeWindowSettings = NativeWindowSettings ()
nativeWindowSettings.ClientSize <- Vector2i (800, 600)
nativeWindowSettings.Title <- "LearnOpenTK - Creating a Window"
nativeWindowSettings.Flags <- ContextFlags.ForwardCompatible

let window = new GameWindow (GameWindowSettings.Default, nativeWindowSettings)

window.add_UpdateFrame (onUpdateFrame window)

window.Run ()