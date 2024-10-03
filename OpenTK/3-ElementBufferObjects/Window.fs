module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// So you've drawn the first triangle. But what about drawing multiple?
// You may consider just adding more vertices to the array, and that would technically work, but say you're drawing a rectangle.
// It only needs four vertices, but since OpenGL works in triangles, you'd need to define 6.
// Not a huge deal, but it quickly adds up when you get to more complex models. For example, a cube only needs 8 vertices, but
// doing it that way would need 36 vertices!

// OpenGL provides a way to reuse vertices, which can heavily reduce memory usage on complex objects.
// This is called an Element Buffer Object. This tutorial will be all about how to set one up.
    
// We modify the vertex array to include four vertices for our rectangle.
let private vertices = [|
     0.5f;  0.5f; 0.0f; // top right
     0.5f; -0.5f; 0.0f; // bottom right
    -0.5f; -0.5f; 0.0f; // bottom left
    -0.5f;  0.5f; 0.0f|]// top left

// Then, we create a new array: indices.
// This array controls how the EBO will use those vertices to create triangles
let private indices = [|
    // Note that indices start at 0!
    0; 1; 3  // The first triangle will be the top-right half of the triangle
    1; 2; 3|]// Then the second will be the bottom-left half of the triangle

type Model = {
    VertexBufferObject: int
    VertexArrayObject: int
    Shader: Shader
    // Add a handle for the EBO
    ElementBufferObject: int}

// Now, we start initializing OpenGL.
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

    // We create/bind the Element Buffer Object EBO the same way as the VBO, except there is a major difference here which can be REALLY confusing.
    // The binding spot for ElementArrayBuffer is not actually a global binding spot like ArrayBuffer is. 
    // Instead it's actually a property of the currently bound VertexArrayObject, and binding an EBO with no VAO is undefined behaviour.
    // This also means that if you bind another VAO, the current ElementArrayBuffer is going to change with it.
    // Another sneaky part is that you don't need to unbind the buffer in ElementArrayBuffer as unbinding the VAO is going to do this,
    // and unbinding the EBO will remove it from the VAO instead of unbinding it like you would for VBOs or VAOs.
    let elementBufferObject = GL.GenBuffer ()
    GL.BindBuffer (BufferTarget.ElementArrayBuffer, elementBufferObject)
    // We also upload data to the EBO the same way as we did with VBOs.
    GL.BufferData (
        BufferTarget.ElementArrayBuffer,
        (indices |> Array.length) * sizeof<int>,
        indices,
        BufferUsageHint.StaticDraw)
    // The EBO has now been properly setup. Go to the Render function to see how we draw our rectangle now!

    Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"
    |> Result.map (fun shader ->
        // Now, enable the shader.
        // Just like the VBO, this is global, so every function that uses a shader will modify this one until a new one is bound instead.
        shader |> Shaders.useProgram

        { VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader
          ElementBufferObject = elementBufferObject })

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    model.Shader |> Shaders.useProgram

    // Because ElementArrayObject is a property of the currently bound VAO,
    // the buffer you will find in the ElementArrayBuffer will change with the currently bound VAO.
    GL.BindVertexArray model.VertexBufferObject

    // Then replace your call to DrawTriangles with one to DrawElements
    // Arguments:
    //   Primitive type to draw. Triangles in this case.
    //   How many indices should be drawn. Six in this case.
    //   Data type of the indices. The indices are an unsigned int, so we want that here too.
    //   Offset in the EBO. Set this to 0 because we want to draw the whole thing.
    GL.DrawElements (
        PrimitiveType.Triangles,
        indices |> Array.length,
        DrawElementsType.UnsignedInt,
        0)

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