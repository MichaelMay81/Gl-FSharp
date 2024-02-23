open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input
open System

open Tutorial1_4_Abstractions
open Tutorial2_2_Camera

type Model = {
    Window: IWindow
    Gl: GL
    Keyboard: IKeyboard
    Mouse: IMouse

    Width: int
    Height: int

    vbo: BufferObject<float32>
    vao: VertexArrayObject
    LightingShader: Shader
    LampShader: Shader

    Camera: Camera
    LastMousePosition: Vector2 option }

// The quad vertices data.
let private vertices = [|
    -0.5f; -0.5f; -0.5f;
     0.5f; -0.5f; -0.5f;
     0.5f;  0.5f; -0.5f;
     0.5f;  0.5f; -0.5f;
    -0.5f;  0.5f; -0.5f;
    -0.5f; -0.5f; -0.5f;

    -0.5f; -0.5f;  0.5f;
     0.5f; -0.5f;  0.5f;
     0.5f;  0.5f;  0.5f;
     0.5f;  0.5f;  0.5f;
    -0.5f;  0.5f;  0.5f;
    -0.5f; -0.5f;  0.5f;

    -0.5f;  0.5f;  0.5f;
    -0.5f;  0.5f; -0.5f;
    -0.5f; -0.5f; -0.5f;
    -0.5f; -0.5f; -0.5f;
    -0.5f; -0.5f;  0.5f;
    -0.5f;  0.5f;  0.5f;

     0.5f;  0.5f;  0.5f;
     0.5f;  0.5f; -0.5f;
     0.5f; -0.5f; -0.5f;
     0.5f; -0.5f; -0.5f;
     0.5f; -0.5f;  0.5f;
     0.5f;  0.5f;  0.5f;

    -0.5f; -0.5f; -0.5f;
     0.5f; -0.5f; -0.5f;
     0.5f; -0.5f;  0.5f;
     0.5f; -0.5f;  0.5f;
    -0.5f; -0.5f;  0.5f;
    -0.5f; -0.5f; -0.5f;

    -0.5f;  0.5f; -0.5f;
     0.5f;  0.5f; -0.5f;
     0.5f;  0.5f;  0.5f;
     0.5f;  0.5f;  0.5f;
    -0.5f;  0.5f;  0.5f;
    -0.5f;  0.5f; -0.5f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo |> BufferObjects.dispose
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

        let yaw = model.Camera.Yaw + xOffset
        let pitch = model.Camera.Pitch - yOffset
        
        let yaw = Math.Clamp (yaw, -100f, -80f)
        let pitch = Math.Clamp (pitch, -10f, 10f)

        { model with
            LastMousePosition = Some position
            Camera = { model.Camera with Yaw=yaw; Pitch=pitch }}

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear
        (ClearBufferMask.ColorBufferBit |||
        ClearBufferMask.DepthBufferBit)

    let lightingShaderWerror func = model.LightingShader |> func |> printError
    let lampShaderWerror func = model.LampShader |> func |> printError

    let viewMat = model.Camera |> Cameras.viewMatrix
    let projMat = model.Camera |> Cameras.projectionMatrix model.Width model.Height

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.LightingShader |> Shaders.useProgram

    //Setting a uniform.
    lightingShaderWerror <| Shaders.setUniformMat4 "uModel" (25f |> degreesToRadians |> Matrix4x4.CreateRotationY)
    lightingShaderWerror <| Shaders.setUniformMat4 "uView" viewMat
    lightingShaderWerror <| Shaders.setUniformMat4 "uProjection" projMat
    lightingShaderWerror <| Shaders.setUniformVec3 "objectColor" (Vector3 (1f, 0.5f, 031f))
    lightingShaderWerror <| Shaders.setUniformVec3 "lightColor" Vector3.One
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)

    model.LampShader |> Shaders.useProgram
    let lampMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateScale 0.2f
        * Matrix4x4.CreateTranslation (Vector3 (1.2f, 1f, 2f))

    lampShaderWerror <| Shaders.setUniformMat4 "uModel" lampMatrix
    lampShaderWerror <| Shaders.setUniformMat4 "uView" viewMat
    lampShaderWerror <| Shaders.setUniformMat4 "uProjection" projMat
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)


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

    { model with Camera = {model.Camera with Position = cameraPosition} }


let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head
    let mouse = inputContext.Mice |> Seq.head
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 3u 0u

    let lightingShaderOpt =
        Shaders.create gl "src/shader.vert" "src/lighting.frag"
        |> resultToOption

    let lampShaderOpt =
        Shaders.create gl "src/shader.vert" "src/shader.frag"
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
            Camera = camera
            Width = window.Size.X
            Height = window.Size.Y
            Keyboard = keyboard
            Mouse = mouse
            LastMousePosition = None } |> Some
    | _ ->
        vbo |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        lightingShaderOpt |> Option.iter Shaders.dispose
        lampShaderOpt |> Option.iter Shaders.dispose
        None

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "3.1 - Ambient Lighting"
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