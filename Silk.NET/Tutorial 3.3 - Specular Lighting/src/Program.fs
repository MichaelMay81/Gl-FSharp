open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input

open Tutorial1_4_Abstractions
open Tutorial2_2_Camera

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
    LampPosition: Vector3

    Camera: Camera
    //Used to track change in mouse movement to allow for moving of the Camera
    LastMousePosition: Vector2 option }

let private vertices = [|
    //X    Y      Z       Normals
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
     0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
    -0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f;

    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f;
     0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f;
    -0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f;
    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f;

    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f;
    -0.5f;  0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f;
    -0.5f; -0.5f;  0.5f; -1.0f;  0.0f;  0.0f;
    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f;

     0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f;
     0.5f;  0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
     0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
     0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f;
     0.5f; -0.5f;  0.5f;  1.0f;  0.0f;  0.0f;
     0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f;

    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;
     0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;
     0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
     0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f;

    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f;
     0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
     0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
    -0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f;
    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f |]

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

let renderLampCube (model:Model) =
    model.LampShader |> Shaders.useProgram

    //The Lamp cube is going to be a scaled down version of the normal cubes verticies moved to a different screen location
    let lampMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateScale 0.2f
        * Matrix4x4.CreateTranslation model.LampPosition

    let shaderWerror func = model.LampShader |> func |> printError
    shaderWerror <| Shaders.setUniformMat4 "uModel" lampMatrix
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model.Camera |> Cameras.projectionMatrix model.Width model.Height)
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0, 36u)

let RenderLitCube (model:Model) =
    model.LightingShader |> Shaders.useProgram

    let shaderWerror func = model.LightingShader |> func |> printError

    //Slightly rotate the cube to give it an angled face to look at
    shaderWerror <| Shaders.setUniformMat4 "uModel" (25f |> degreesToRadians |> Matrix4x4.CreateRotationY)
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model.Camera |> Cameras.projectionMatrix model.Width model.Height)
    shaderWerror <| Shaders.setUniformVec3 "objectColor" (Vector3 (1f, 0.5f, 031f))
    shaderWerror <| Shaders.setUniformVec3 "lightColor" Vector3.One
    shaderWerror <| Shaders.setUniformVec3 "lightPos" model.LampPosition
    shaderWerror <| Shaders.setUniformVec3 "viewPos" model.Camera.Position
    
    //We're drawing with just vertices and no indicies, and it takes 36 verticies to have a six-sided textured cube
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0, 36u)

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

    let lampPosition =
        Vector3.Transform (
            model.LampPosition,
            Matrix4x4.CreateRotationY 0.01f)
        
    { model with
        Camera = { model.Camera with Position = cameraPosition }
        LampPosition = lampPosition }


let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head
    let mouse = inputContext.Mice |> Seq.head
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 6u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 3u 6u 3u

    //The lighting shader will give our main cube its colour multiplied by the light's intensity
    let lightingShaderOpt =
        Shaders.create gl "shader.vert" "lighting.frag"
        |> resultToOption
    //The Lamp shader uses a fragment shader that just colours it solid white so that we know it is the light source
    let lampShaderOpt =
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    let camera =
        { Position= Vector3.UnitZ * 6f
          Up= Vector3.UnitY
          Yaw= -90f
          Pitch= 0f
          Zoom = 45f }

    match lightingShaderOpt, lampShaderOpt with
    | Some lightingShader, Some lampShader ->
        {   Window = window
            Gl = gl
            vbo = vbo
            vao = vao
            LightingShader = lightingShader
            LampShader = lampShader
            LampPosition = Vector3 (1.2f, 1f, 2f)
            Camera = camera
            Width = window.Size.X
            Height = window.Size.Y
            Keyboard = keyboard
            Mouse = mouse
            LastMousePosition = None } |> Some
    | _ ->
        vbo.BufferObject |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        lightingShaderOpt |> Option.iter Shaders.dispose
        lampShaderOpt |> Option.iter Shaders.dispose
        None

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "3.3 - Specular Lighting"
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
        | None ->
            window.Close () )
    
    window.Run ()
    window.Dispose ()
    0