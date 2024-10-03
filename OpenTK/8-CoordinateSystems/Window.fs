module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// We can now move around objects. However, how can we move our "camera", or modify our perspective?
// In this tutorial, I'll show you how to set up a full projection/view/model (PVM) matrix.
// In addition, we'll make the rectangle rotate over time.

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
    Texture2: Texture
    // We create a double to hold how long has passed since the program was opened.
    Time: double
    // Then, we create two matrices to hold our view and projection. They're initialized at the bottom of OnLoad.
    // The view matrix is what you might consider the "camera". It represents the current viewport in the window.
    View: Matrix4
    // This represents how the vertices will be projected. It's hard to explain through comments,
    // so check out the web version for a good demonstration of what this does.
    Projection: Matrix4
}

let onLoad (window:GameWindow) : Result<Model, string> = Result.result {
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)

    // We enable depth testing here. If you try to draw something more complex than one plane without this,
    // you'll notice that polygons further in the background will occasionally be drawn over the top of the ones in the foreground.
    // Obviously, we don't want this, so we enable depth testing. We also clear the depth buffer in GL.Clear over in OnRenderFrame.
    GL.Enable EnableCap.DepthTest
    
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

    // shader.vert has been modified. Take a look at it after the explanation in OnRenderFrame.
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
    
    // For the view, we don't do too much here. Next tutorial will be all about a Camera class that will make it much easier to manipulate the view.
    // For now, we move it backwards three units on the Z axis.
    let view = Matrix4.CreateTranslation (0.0f, 0.0f, -3.0f)

    // For the matrix, we use a few parameters.
    //   Field of view. This determines how much the viewport can see at once. 45 is considered the most "realistic" setting, but most video games nowadays use 90
    //   Aspect ratio. This should be set to Width / Height.
    //   Near-clipping. Any vertices closer to the camera than this value will be clipped.
    //   Far-clipping. Any vertices farther away from the camera than this value will be clipped.
    let projection =
        Matrix4.CreatePerspectiveFieldOfView (
            MathHelper.DegreesToRadians(45f),
            (float32 window.Size.X) / (float32 window.Size.Y),
            0.1f,
            100.0f)

    // Now, head over to OnRenderFrame to see how we set up the model matrix.

    return
        { ElementBufferObject = elementBufferObject
          VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader
          Texture = texture
          Texture2 = texture2
          Time = 0
          View = view
          Projection = projection }
 }

let onRenderFrame
    (window:GameWindow)
    (e:FrameEventArgs)
    (model:Model)
    : Result<Model,string> = Result.result {
        
    // We add the time elapsed since last frame, times 4.0 to speed up animation, to the total amount of time passed.
    let model = { model with Time = model.Time + 4.0 * e.Time }

    // We clear the depth buffer in addition to the color buffer.
    GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

    GL.BindVertexArray model.VertexBufferObject

    model.Texture |> Textures.useTexture TextureUnit.Texture0
    model.Texture2 |> Textures.useTexture TextureUnit.Texture1
    model.Shader |> Shaders.useProgram

    // Finally, we have the model matrix. This determines the position of the model.
    let modelMat = Matrix4.Identity * Matrix4.CreateRotationX(float32 (MathHelper.DegreesToRadians model.Time))

    // Then, we pass all of these matrices to the vertex shader.
    // You could also multiply them here and then pass, which is faster, but having the separate matrices available is used for some advanced effects.

    // IMPORTANT: OpenTK's matrix types are transposed from what OpenGL would expect - rows and columns are reversed.
    // They are then transposed properly when passed to the shader. 
    // This means that we retain the same multiplication order in both OpenTK c# code and GLSL shader code.
    // If you pass the individual matrices to the shader and multiply there, you have to do in the order "model * view * projection".
    // You can think like this: first apply the modelToWorld (aka model) matrix, then apply the worldToView (aka view) matrix, 
    // and finally apply the viewToProjectedSpace (aka projection) matrix.
    do! model.Shader |> Shaders.setMatrix "model" modelMat
    do! model.Shader |> Shaders.setMatrix "view" model.View
    do! model.Shader |> Shaders.setMatrix "projection" model.Projection
    
    GL.DrawElements (PrimitiveType.Triangles, indices |> Array.length, DrawElementsType.UnsignedInt, 0)

    window.SwapBuffers ()

    return model
}

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let onResize (window:GameWindow) (_:ResizeEventArgs) =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)
    
let setup (window:GameWindow) =
    let mutable modelResult = Error "not loaded yet"
    
    window.add_UpdateFrame (onUpdateFrame window)
    window.add_Resize (onResize window)
    window.add_Load (fun _ ->
        modelResult <- onLoad window)
    window.add_RenderFrame (fun eventArgs ->
        modelResult <-
            modelResult
            |> Result.bind (onRenderFrame window eventArgs)
            |> Result.mapError (fun error ->
                printfn "Error: %s" error
                error))