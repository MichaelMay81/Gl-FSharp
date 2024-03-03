open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop

open LearnOpenTK

let nativeWindowSettings = NativeWindowSettings ()
nativeWindowSettings.ClientSize <- Vector2i (800, 600)
nativeWindowSettings.Title <- "LearnOpenTK - 4 Shaders More Attributes"
// This is needed to run on macos
nativeWindowSettings.Flags <- ContextFlags.ForwardCompatible

let window = new GameWindow (GameWindowSettings.Default, nativeWindowSettings)

window.add_UpdateFrame (Window.onUpdateFrame window)
window.add_Resize (Window.onResize window)

window.add_Load (fun _ ->
    Window.onLoad () |> function
    | Ok model ->
        window.add_RenderFrame (Window.onRenderFrame window model)
    | Error error ->
        printfn "%s" error)

window.Run ()