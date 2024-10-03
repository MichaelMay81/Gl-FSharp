module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// In this project, we will be assigning 3 colors to the triangle, one for each vertex.
// The output will be an interpolated value based on the distance from each vertex.
// If you want to look more into it, the in-between step is called a Rasterizer.

let private vertices = [|
     // positions        // colors
     0.5f; -0.5f; 0.0f;  1.0f; 0.0f; 0.0f;   // bottom right
    -0.5f; -0.5f; 0.0f;  0.0f; 1.0f; 0.0f;   // bottom left
     0.0f;  0.5f; 0.0f;  0.0f; 0.0f; 1.0f |] // top

type Model = {
    VertexBufferObject: int
    VertexArrayObject: int
    Shader: Shader}

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

    // Just like before, we create a pointer for the 3 position components of our vertices.
    // The only difference here is that we need to account for the 3 color values in the stride variable.
    // Therefore, the stride contains the size of 6 floats instead of 3.
    GL.VertexAttribPointer (
        0,
        3,
        VertexAttribPointerType.Float,
        false,
        6 * sizeof<float32>,
        0)
    GL.EnableVertexAttribArray 0

    // We create a new pointer for the color values.
    // Much like the previous pointer, we assign 6 in the stride value.
    // We also need to correctly set the offset to get the color values.
    // The color data starts after the position data, so the offset is the size of 3 floats.
    GL.VertexAttribPointer (
        1,
        3,
        VertexAttribPointerType.Float,
        false,
        6 * sizeof<float32>,
        3 * sizeof<float32>)
    GL.EnableVertexAttribArray 1

    let maxAttributeCount = GL.GetInteger GetPName.MaxVertexAttribs
    printfn "Maximum number of vertex attributes supported: %i" maxAttributeCount

    Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"
    |> Result.map (fun shader ->
        shader |> Shaders.useProgram

        { VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader })

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    model.Shader |> Shaders.useProgram

    GL.BindVertexArray model.VertexBufferObject

    GL.DrawArrays (PrimitiveType.Triangles, 0, 3)

    window.SwapBuffers ()

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let onResize (window:GameWindow) (_:ResizeEventArgs) =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)
    
let setup (window:GameWindow) =
    window.add_UpdateFrame (onUpdateFrame window)
    window.add_Resize (onResize window)

    window.add_Load (fun _ ->
        onLoad () |> function
        | Ok model ->
            window.add_RenderFrame (onRenderFrame window model)
        | Error error ->
            printfn "%s" error)