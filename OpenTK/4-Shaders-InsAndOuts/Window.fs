module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// Here we'll be elaborating on what shaders can do from the Hello World project we worked on before.
// Specifically we'll be showing how shaders deal with input and output from the main program 
// and between each other.

let private vertices = [|
    -0.5f; -0.5f; 0.0f; // Bottom-left vertex
     0.5f; -0.5f; 0.0f; // Bottom-right vertex
     0.0f;  0.5f; 0.0f|]// Top vertex

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

    GL.VertexAttribPointer (0, 3, VertexAttribPointerType.Float, false, 3 * sizeof<float32>, 0)
    GL.EnableVertexAttribArray 0

    // Vertex attributes are the data we send as input into the vertex shader from the main program.
    // So here we're checking to see how many vertex attributes our hardware can handle.
    // OpenGL at minimum supports 16 vertex attributes. This only needs to be called 
    // when your intensive attribute work and need to know exactly how many are available to you.
    let maxAttributeCount = GL.GetInteger GetPName.MaxVertexAttribs
    printfn "Maximum number of vertex attributes supported: %i" maxAttributeCount

    Shaders.init "shader.vert" "shader.frag"
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