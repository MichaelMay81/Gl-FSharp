open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop

open LearnOpenTK

let nativeWindowSettings = NativeWindowSettings (
    ClientSize = Vector2i (800, 600),
    Title = "LearnOpenTK - 2 Hello Triangle",
    // This is needed to run on macos
    Flags = ContextFlags.ForwardCompatible)

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