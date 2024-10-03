module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

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

    // shader.frag has been modified yet again, take a look at it as well.
    let! shader = Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"
    shader |> Shaders.useProgram

    let vertexLocation = shader |> Shaders.getAttribLocation "aPosition"
    GL.EnableVertexAttribArray vertexLocation
    GL.VertexAttribPointer (vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 0)

    let texCoordLocation = shader |> Shaders.getAttribLocation "aTexCoord"
    GL.EnableVertexAttribArray texCoordLocation
    GL.VertexAttribPointer (texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 3 * sizeof<float32>)

    let! texture = Textures.loadFromFile "Resources/container.png"
    // Texture units are explained in Texture.fs, at the Use function.
    // First texture goes in texture unit 0.
    texture |> Textures.useTexture TextureUnit.Texture0
    
    let! texture2 = Textures.loadFromFile "Resources/awesomeface.png"
    // Then, the second goes in texture unit 1.
    texture |> Textures.useTexture TextureUnit.Texture1

    // Next, we must set up the samplers in the shaders to use the right textures.
    // The int we send to the uniform indicates which texture unit the sampler should use.
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

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    model.Shader |> Shaders.useProgram

    GL.BindVertexArray model.VertexBufferObject

    model.Texture |> Textures.useTexture TextureUnit.Texture0
    model.Texture2 |> Textures.useTexture TextureUnit.Texture1
    model.Shader |> Shaders.useProgram

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