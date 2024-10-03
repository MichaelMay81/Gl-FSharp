module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// So you can set up OpenGL, you can draw basic shapes without wasting vertices, and you can texture them.
// There's one big thing left, though: moving the shapes.
// To do this, we use linear algebra to move the vertices in the vertex shader.

// Just as a disclaimer: this tutorial will NOT explain linear algebra or matrices; those topics are wayyyyy too complex to do with comments.
// If you want a more detailed understanding of what's going on here, look at the web version of this tutorial instead.
// A deep understanding of linear algebra won't be necessary for this tutorial as OpenTK includes built-in matrix types that abstract over the actual math.

// Head down to RenderFrame to see how we can apply transformations to our shape.

let private vertices = [|
     // Position        Texture coordinates
     0.5f;  0.5f; 0.0f; 1.0f; 1.0f; // top right
     0.5f; -0.5f; 0.0f; 1.0f; 0.0f; // bottom right
    -0.5f; -0.5f; 0.0f; 0.0f; 0.0f; // bottom left
    -0.5f;  0.5f; 0.0f; 0.0f; 1.0f|]  // top left

let private indices = [|
    0u; 1u; 3u
    1u; 2u; 3u |]

type Model = {
    ElementBufferObject: int
    VertexBufferObject: int
    VertexArrayObject: int
    Shader: Shader
    Texture: Texture
    Texture2: Texture }

let onLoad () : Result<Model, string> = Result.result {
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)
    
    let vertexArrayObject = GL.GenVertexArray ()
    GL.BindVertexArray vertexArrayObject

    let vertexBufferObject = GL.GenBuffer ()
    GL.BindBuffer (BufferTarget.ArrayBuffer, vertexBufferObject)
    GL.BufferData (
        BufferTarget.ArrayBuffer,
        (vertices |> Array.length) * sizeof<float32>,
        vertices,
        BufferUsageHint.StaticDraw)

    let elementBufferObject = GL.GenBuffer ()
    GL.BindBuffer (BufferTarget.ElementArrayBuffer, elementBufferObject)
    GL.BufferData (
        BufferTarget.ElementArrayBuffer,
        (indices |> Array.length) * sizeof<float32>,
        indices,
        BufferUsageHint.StaticDraw)

    // shader.vert has been modified, take a look at it as well.
    let! shader = Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"
    shader |> Shaders.useProgram

    let vertexLocation = shader |> Shaders.getAttribLocation "aPosition"
    GL.EnableVertexAttribArray vertexLocation
    GL.VertexAttribPointer (vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 0)

    let texCoordLocation = shader |> Shaders.getAttribLocation "aTexCoord"
    GL.EnableVertexAttribArray texCoordLocation
    GL.VertexAttribPointer (texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 3 * sizeof<float32>)

    let! texture = Textures.loadFromFile "container.png"
    texture |> Textures.useTexture TextureUnit.Texture0
    
    let! texture2 = Textures.loadFromFile "awesomeface.png"
    texture |> Textures.useTexture TextureUnit.Texture1

    do! shader |> Shaders.setInt "texture0" 0
    do! shader |> Shaders.setInt "texture1" 1
    
    return
        { ElementBufferObject = elementBufferObject
          VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader
          Texture = texture
          Texture2 = texture2 }
 }

let onRenderFrame (window:GameWindow) (model:Model) (_:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    GL.BindVertexArray model.VertexBufferObject

    // Note: The matrices we'll use for transformations are all 4x4.

    // We start with an identity matrix. This is just a simple matrix that doesn't move the vertices at all.
    let transform = Matrix4.Identity

    // The next few steps just show how to use OpenTK's matrix functions, and aren't necessary for the transform matrix to actually work.
    // If you want, you can just pass the identity matrix to the shader, though it won't affect the vertices at all.

    // A fact to note about matrices is that the order of multiplications matter. "matrixA * matrixB" and "matrixB * matrixA" mean different things.
    // A VERY important thing to know is that OpenTK matrices are so called row-major. We won't go into the full details here, but here is a good place to read more about it:
    // https://www.scratchapixel.com/lessons/mathematics-physics-for-computer-graphics/geometry/row-major-vs-column-major-vector
    // What it means for us is that we can think of matrix multiplication as going left to right.
    // So "rotate * translate" means rotate (around the origin) first and then translate, as opposed to "translate * rotate" which means translate and then rotate (around the origin).

    // To combine two matrices, you multiply them. Here, we combine the transform matrix with another one created by OpenTK to rotate it by 20 degrees.
    // Note that all Matrix4.CreateRotation functions take radians, not degrees. Use MathHelper.DegreesToRadians() to convert to radians, if you want to use degrees.
    let transform = transform * Matrix4.CreateRotationZ (MathHelper.DegreesToRadians 20f)

    // Next, we scale the matrix. This will make the rectangle slightly larger.
    let transform = transform * Matrix4.CreateScale 1.1f

    // Then, we translate the matrix, which will move it slightly towards the top-right.
    // Note that we aren't using a full coordinate system yet, so the translation is in normalized device coordinates.
    // The next tutorial will be about how to set one up so we can use more human-readable numbers.
    let transform = transform * Matrix4.CreateTranslation (0.1f, 0.1f, 0.0f)

    model.Texture |> Textures.useTexture TextureUnit.Texture0
    model.Texture2 |> Textures.useTexture TextureUnit.Texture1
    model.Shader |> Shaders.useProgram

    // Now that the matrix is finished, pass it to the vertex shader.
    // Go over to shader.vert to see how we finally apply this to the vertices.
    model.Shader
    |> Shaders.setMatrix "transform" transform
    |> Result.mapError (printfn "%s")
    |> ignore

    // And that's it for now! In the next tutorial, we'll see how to set up a full coordinates system.    

    GL.DrawElements (PrimitiveType.Triangles, indices |> Array.length, DrawElementsType.UnsignedInt, 0)

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