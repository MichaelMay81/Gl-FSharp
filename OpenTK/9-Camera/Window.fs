module LearnOpenTK.Window

open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// We now have a rotating rectangle but how can we make the view move based on the users input?
// In this tutorial we will take a look at how you could implement a camera class
// and start responding to user input.
// You can move to the camera class to see a lot of the new code added.
// Otherwise, you can move to Load to see how the camera is initialized.

// In reality, we can't move the camera, but we actually move the rectangle.
// This will be explained more in depth in the web version, however it pretty much gives us the same result
// as if the view itself was moved.

let private vertices = [|
     // Position        Texture coordinates
     0.5f;  0.5f; 0.0f; 1.0f; 1.0f; // top right
     0.5f; -0.5f; 0.0f; 1.0f; 0.0f; // bottom right
    -0.5f; -0.5f; 0.0f; 0.0f; 0.0f; // bottom left
    -0.5f;  0.5f; 0.0f; 0.0f; 1.0f|]// top left

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
    
    // The view and projection matrices have been removed as we don't need them here anymore.
    // They can now be found in the new camera record.

    // We need the new camera record so it can hold the view and projection matrix.
    // Finally, we add the last position of the mouse so we can calculate the mouse offset easily.
    Camera: Camera
    LastPos: Vector2 option    
    Time: double
}

let onLoad (window:GameWindow) : Result<Model, string> = Result.result {
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)

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
    
    // We initialize the camera so that it is 3 units back from where the rectangle is.
    // We also give it the proper aspect ratio.
    let camera =
        Camera.Init
            (Vector3.UnitZ * 3f)
            (float32 window.Size.X / float32 window.Size.Y)
    
    // We make the mouse cursor invisible and captured so we can have proper FPS-camera movement.
    window.CursorState <- CursorState.Grabbed
    
    return
        { ElementBufferObject = elementBufferObject
          VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader
          Texture = texture
          Texture2 = texture2
          Camera = camera
          LastPos = None
          Time = 0 }
 }

let onRenderFrame
    (window:GameWindow)
    (e:FrameEventArgs)
    (model:Model)
    : Result<Model,string> = Result.result {
        
    let model = { model with Time = model.Time + 4.0 * e.Time }

    GL.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

    GL.BindVertexArray model.VertexBufferObject

    model.Texture |> Textures.useTexture TextureUnit.Texture0
    model.Texture2 |> Textures.useTexture TextureUnit.Texture1
    model.Shader |> Shaders.useProgram

    let modelMat = Matrix4.Identity * Matrix4.CreateRotationX(float32 (MathHelper.DegreesToRadians model.Time))
    do! model.Shader |> Shaders.setMatrix "model" modelMat
    do! model.Shader |> Shaders.setMatrix "view" (Cameras.getViewMatrix model.Camera)
    do! model.Shader |> Shaders.setMatrix "projection" (Cameras.getProjectionMatrix model.Camera)
    
    GL.DrawElements (PrimitiveType.Triangles, indices |> Array.length, DrawElementsType.UnsignedInt, 0)

    window.SwapBuffers ()

    return model
}

let onUpdateFrame
    (window:GameWindow)
    (e:FrameEventArgs)
    (model:Model)
    : Model option =
        
    let input = window.KeyboardState
        
    // Check to see if the window is focused
    if (not window.IsFocused) then
        model |> Some
    elif  input.IsKeyDown Keys.Escape then
        None
    else    
        let cameraSpeed = 1.5f
        let sensitivity = 0.2f<degree>
        
        let orientation = model.Camera.Orientation
        let positionCorrection =
            if input.IsKeyDown Keys.W then // Forward
                orientation.Front * cameraSpeed * (float32 e.Time)
            elif input.IsKeyDown Keys.S then // Backwards
                - orientation.Front * cameraSpeed * (float32 e.Time)
            elif input.IsKeyDown Keys.A then // Left
                - orientation.Right * cameraSpeed * (float32 e.Time)
            elif input.IsKeyDown Keys.D then // Right
                orientation.Right * cameraSpeed * (float32 e.Time)
            elif input.IsKeyDown Keys.Space then // Up
                orientation.Up * cameraSpeed * (float32 e.Time)
            elif input.IsKeyDown Keys.LeftShift then // Down
                - orientation.Up * cameraSpeed * (float32 e.Time)
            else Vector3.Zero
            
        let camera = {
            model.Camera with
                Position = model.Camera.Position + positionCorrection }
        
        // Get the mouse state
        let mouse = window.MouseState
        
        let newPos, camera =
            match model.LastPos with
            | None -> // This option is initially set to None.
                Vector2 (mouse.X, mouse.Y), camera
            | Some lastPos ->
                // Calculate the offset of the mouse position
                let deltaX = mouse.X - lastPos.X
                let deltaY = mouse.Y - lastPos.Y
                
                // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                let camera =
                    camera
                    |> Cameras.updatePitchYaw
                        (fun eulerAngles ->
                            {| Yaw = eulerAngles.Yaw + (deltaX * sensitivity)
                               // Reversed since y-coordinates range from bottom to top
                               Pitch = eulerAngles.Pitch - (deltaY * sensitivity) |})
                        
                Vector2(mouse.X, mouse.Y), camera
                
        { model with
            Camera = camera
            LastPos = Some newPos } |> Some

// In the mouse wheel function, we manage all the zooming of the camera.
// This is simply done by changing the FOV of the camera.
let onMouseWheel (_:GameWindow) (e:MouseWheelEventArgs) (model:Model) : Model =
    { model with
        Model.Camera =
            model.Camera
            |> Cameras.updateFov (fun fov -> fov - e.OffsetY * 1f<degree>) }
        
let onResize (window:GameWindow) (_:ResizeEventArgs) (model:Model) : Model =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)
    // We need to update the aspect ratio once the window has been resized.
    { model with Model.Camera.AspectRatio = float32 window.Size.X / float32 window.Size.Y }
    
let resultToOption = function
    | Ok okValue ->
        Some okValue
    | Error errorValue ->
        printfn "Error: %s" errorValue
        None

let setup (window:GameWindow) : unit =
    let mutable modelResult = None
    
    let updateModel update args =
        modelResult <- modelResult |> Option.map (update window args)
    
    let updateModelFromResult update args =
        modelResult <- modelResult |> Option.bind (update window args >> resultToOption)
    
    window.add_Load (fun _ ->
        modelResult <- onLoad window |> resultToOption)
    window.add_Resize (updateModel onResize)
    window.add_RenderFrame (updateModelFromResult onRenderFrame)
    window.add_MouseWheel (updateModel onMouseWheel)
    window.add_UpdateFrame (fun args ->
        modelResult <-
            modelResult
            |> Option.bind (fun model ->
                let model = onUpdateFrame window args model
                if model |> Option.isNone then
                    window.Close()
                model))