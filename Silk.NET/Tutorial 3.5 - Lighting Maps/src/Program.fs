open System
open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.Input
open Silk.NET.OpenGL

open Tutorial1_4_Abstractions

type Model = {
    Window: IWindow
    Gl: GL
    Keyboard: IKeyboard
    Mouse: IMouse

    Width: int
    Height: int

    vbo: VertexBufferObject
    vao: VertexArrayObject
    LightingShader: Shader
    LampShader: Shader

    DiffuseMap: Texture
    SpecularMap: Texture

    Camera: Camera

    //Used to track change in mouse movement to allow for moving of the Camera
    LastMousePosition: Vector2 option

    //Track when the window started so we can use the time elapsed for animations
    StartTime: DateTime }

// The quad vertices data.
let private vertices = [|
    //X    Y      Z       Normals             U     V
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 1.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 1.0f; 0.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 1.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 0.0f; 0.0f;
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f; 0.0f; 1.0f;

    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 0.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 1.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 0.0f; 0.0f;
    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f; 0.0f; 1.0f;

    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f; 0.0f; 1.0f;
    -0.5f;  0.5f; -0.5f; -1.0f;  0.0f;  0.0f; 1.0f; 1.0f;
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f; 1.0f; 0.0f;
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f; 1.0f; 0.0f;
    -0.5f; -0.5f;  0.5f; -1.0f;  0.0f;  0.0f; 0.0f; 0.0f;
    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f; 0.0f; 1.0f;

     0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f; 0.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  1.0f;  0.0f;  0.0f; 1.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f; 1.0f; 0.0f;
     0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f; 1.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  1.0f;  0.0f;  0.0f; 0.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f; 0.0f; 1.0f;

    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f; 0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f; 1.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f; 1.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f; 1.0f; 0.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f; 0.0f; 0.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f; 0.0f; 1.0f;

    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f; 0.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f; 1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f; 1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f; 1.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f; 0.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f; 0.0f; 1.0f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo.BufferObject |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.LightingShader |> Shaders.dispose
    model.LampShader |> Shaders.dispose
    model.DiffuseMap |> Textures.dispose
    model.SpecularMap |> Textures.dispose

let OnMouseWheel (model:Model) (scrollWheel:ScrollWheel) : Model =
    { model with Camera = model.Camera |> Cameras.modifyZoom scrollWheel.Y }

let onMouseMove (model:Model) (position:Vector2) : Model =
    let lookSensitivity = 0.1f

    match model.LastMousePosition with
    | None -> { model with LastMousePosition = Some position }
    | Some lastMousePosition ->
        let xOffset = (position.X - lastMousePosition.X) * lookSensitivity
        let yOffset = (position.Y - lastMousePosition.Y) * lookSensitivity

        { model with
            LastMousePosition = Some position
            Camera = model.Camera |> Cameras.modifyDirection xOffset yOffset }

let private lampPosition (startTime: DateTime) =
    let difference = float32 (DateTime.UtcNow - startTime).TotalSeconds
    let interval = MathF.Sin difference
    Vector3.Transform (
            Vector3 (0f, 0.5f, 2f),
            Matrix4x4.CreateRotationY interval)

let renderLampCube (model:Model) =
    let shaderWerror func = model.LampShader |> func |> printError
    
    model.LampShader |> Shaders.useProgram

    //The Lamp cube is going to be a scaled down version of the normal cubes vertices moved to a different screen location
    let lampMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateScale 0.2f
        * Matrix4x4.CreateTranslation (lampPosition model.StartTime)

    shaderWerror <| Shaders.setUniformMat4 "uModel" lampMatrix
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model.Camera |> Cameras.projectionMatrix model.Width model.Height)

    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        36u)

let RenderLitCube (model:Model) =
    let shaderWerror func = model.LightingShader |> func |> printError
    
    model.LightingShader |> Shaders.useProgram

    //Bind the diffuse map and set to use texture0.
    model.DiffuseMap |> Textures.bindSlot0
    //Bind the specular map and set to use texture1.
    model.SpecularMap |> (Textures.bind TextureUnit.Texture1)

    //Set up the coordinate systems for our view
    shaderWerror <| Shaders.setUniformMat4 "uModel" Matrix4x4.Identity
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model.Camera |> Cameras.projectionMatrix model.Width model.Height)
    //Let the shaders know where the Camera is looking from
    shaderWerror <| Shaders.setUniformVec3 "viewPos" model.Camera.Position
    //Configure the materials variables.
    //Diffuse is set to 0 because our diffuseMap is bound to Texture0
    shaderWerror <| Shaders.setUniformInt "material.diffuse" 0
    //Specular is set to 1 because our diffuseMap is bound to Texture1
    shaderWerror <| Shaders.setUniformInt "material.specular" 1
    shaderWerror <| Shaders.setUniformFloat "material.shininess" 32f
    
    let diffuseColor = Vector3 0.5f
    let ambientColor = diffuseColor * Vector3 0.2f

    shaderWerror <| Shaders.setUniformVec3 "light.ambient" ambientColor
    shaderWerror <| Shaders.setUniformVec3 "light.diffuse" diffuseColor
    shaderWerror <| Shaders.setUniformVec3 "light.specular" Vector3.One
    shaderWerror <| Shaders.setUniformVec3 "light.position" (lampPosition model.StartTime)

    //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        36u)

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear
        (ClearBufferMask.ColorBufferBit |||
        ClearBufferMask.DepthBufferBit)
    
    model.vao |> VertexArrayObjects.bind
    
    RenderLitCube model
    renderLampCube model
    
let onUpdate (model:Model) (deltaTime:float) : Model =
    let moveSpeed = 2.5f * float32 deltaTime

    let ifKeyIsPressed key changePos position =
        if model.Keyboard.IsKeyPressed key then
            position + changePos
        else position

    let moveForward = moveSpeed * (model.Camera |> Cameras.front)
    let MoveRight =
        Vector3.Cross (model.Camera |> Cameras.front, model.Camera.Up)
        |> Vector3.Normalize
        |> (*) moveSpeed

    let cameraPosition =
        model.Camera.Position
        |> ifKeyIsPressed Key.W moveForward
        |> ifKeyIsPressed Key.S -moveForward
        |> ifKeyIsPressed Key.A -MoveRight
        |> ifKeyIsPressed Key.D MoveRight

    { model with Camera = { model.Camera with Position = cameraPosition } }


let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head
    let mouse = inputContext.Mice |> Seq.head
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 8u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 3u 8u 3u
    vao |> VertexArrayObjects.vertexAttributePointer 2u 2u 8u 6u

    //The lighting shader will give our main cube its colour multiplied by the light's intensity
    let lightingShaderOpt =
        Shaders.create gl "shader.vert" "lighting.frag"
        |> resultToOption

    //The Lamp shader uses a fragment shader that just colours it solid white so that we know it is the light source
    let lampShaderOpt =
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    //Start a camera at position 6 on the Z axis
    let camera =
        { Position= Vector3.UnitZ * 6f
          Up= Vector3.UnitY
          Yaw= -90f
          Pitch= 0f
          Zoom = 45f}

    let diffuseMapOpt = Textures.createFromFile gl "silkBoxed.png" |> resultToOption
    let specularMapOpt = Textures.createFromFile gl "silkSpecular.png" |> resultToOption

    match lightingShaderOpt, lampShaderOpt, diffuseMapOpt, specularMapOpt with
    | Some lightingShader, Some lampShader, Some diffuseMap, Some specularMap ->
        Some {  Window = window
                Gl = gl
                vbo = vbo
                vao = vao
                LightingShader = lightingShader
                LampShader = lampShader
                DiffuseMap = diffuseMap
                SpecularMap = specularMap
                Camera = camera
                Width = window.Size.X
                Height = window.Size.Y
                Keyboard = keyboard
                Mouse = mouse
                LastMousePosition = None
                StartTime = DateTime.UtcNow }
    | _ ->
        vbo.BufferObject |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        lightingShaderOpt |> Option.iter Shaders.dispose
        lampShaderOpt |> Option.iter Shaders.dispose
        diffuseMapOpt |> Option.iter Textures.dispose
        specularMapOpt |> Option.iter Textures.dispose

        None

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "3.5 - Lighting Maps"
    options.PreferredDepthBufferBits <- 24
    use window = Window.Create options

    window.add_Load (fun _ ->
        match onLoad window with
        | Some model ->
            let mutable model = model

            model.Keyboard.add_KeyDown (keyDown window)
            model.Mouse.add_MouseMove (fun _ pos -> model <- onMouseMove model pos)
            model.Mouse.add_Scroll (fun _ scrollWheel -> model <- OnMouseWheel model scrollWheel)

            window.add_Update (fun deltaTime -> model <- onUpdate model deltaTime)
            window.add_Render (onRender model)
            window.add_Closing (fun _ -> onClose model)
            window.add_FramebufferResize (onFramebufferResize model.Gl)
        | None ->
            window.Close () )
    
    window.Run ()
    window.Dispose ()
    0