open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop
open OpenTK.Mathematics

//open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4

open LearnOpenTK.Common

// type Window (gameWindowSettings:GameWindowSettings, nativeWindowSettings:NativeWindowSettings) =
//     inherit GameWindow (gameWindowSettings, nativeWindowSettings)
//     override this.OnUpdateFrame (e:FrameEventArgs) =
//         GL.Clear
//         if this.KeyboardState.IsKeyDown Keys.Escape then
//             this.Close ()
//         else
//             base.OnUpdateFrame e

type Model = {
    VertexBufferObject: int
    VertexArrayObject: int
    Shader: Shader }

let vertices = [|
    -0.5f; -0.5f; 0.0f; // Bottom-left vertex
     0.5f; -0.5f; 0.0f; // Bottom-right vertex
     0.0f;  0.5f; 0.0f|]// Top vertex

let onLoad () : Result<Model, string> =
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)
    let vertexBufferObject = GL.GenBuffer ()
    GL.BindBuffer (BufferTarget.ArrayBuffer, vertexBufferObject)
    GL.BufferData (
        BufferTarget.ArrayBuffer,
        (vertices |> Array.length) * sizeof<float32>,
        vertices,
        BufferUsageHint.StaticDraw)
    let vertexArrayObject = GL.GenVertexArray ()
    GL.BindVertexArray vertexArrayObject
    GL.VertexAttribPointer (0, 3, VertexAttribPointerType.Float, false, 3 * sizeof<float32>, 0)
    GL.EnableVertexAttribArray 0
    Shaders.init "shader.vert" "shader.frag"
    |> Result.map (fun shader ->
        shader |> Shaders.useProgram
        { VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader })

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit
    // shader.Use ()
    GL.BindVertexArray model.VertexBufferObject
    GL.DrawArrays (PrimitiveType.Triangles, 0, 3)
    window.SwapBuffers ()

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let onResize (window:GameWindow) (e:ResizeEventArgs) =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)

let onUnload (model:Model) =
    GL.BindBuffer (BufferTarget.ArrayBuffer, 0)
    GL.BindVertexArray 0
    GL.UseProgram 0
    GL.DeleteBuffer model.VertexBufferObject
    GL.DeleteVertexArray model.VertexArrayObject
    GL.DeleteProgram model.Shader.Handle

let projectName = System.Reflection.Assembly.GetCallingAssembly().GetName().Name
let title = $"LearnOpenTK - {projectName}"
printfn $"{title}"
printfn $"Current directory: {System.IO.Directory.GetCurrentDirectory()}"

let nativeWindowSettings = NativeWindowSettings ()
nativeWindowSettings.ClientSize <- Vector2i (800, 600)
nativeWindowSettings.Title <- title
nativeWindowSettings.Flags <- ContextFlags.ForwardCompatible

let window = new GameWindow (GameWindowSettings.Default, nativeWindowSettings)

window.add_UpdateFrame (onUpdateFrame window)
window.add_Resize (onResize window)

window.add_Load (fun _ ->
    onLoad () |> function
    | Ok model ->
        window.add_RenderFrame (onRenderFrame window model)
        window.add_Unload (fun _ -> onUnload model)
    | Error error ->
        printfn "%s" error)

window.Run ()