﻿open System.Numerics
open System.Linq
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Tutorial1_4_Abstractions
open Silk.NET.Input
open System

type Model = {
    Window: IWindow
    Gl: GL
    Keyboard: IKeyboard
    Mouse: IMouse

    Width: int
    Height: int

    vbo: BufferObject<float32>
    vao: VertexArrayObject
    LightingShaderOpt: Shader option
    LampShaderOpt: Shader option

    LampPosition: Vector3
    LampColor: Vector3

    Camera: Camera
    Zoom: float32
    LastMousePosition: Vector2 option
    StartTime: DateTime }

// The quad vertices data.
let private vertices = [|
    //X    Y      Z       Normals
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f
    0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f
    0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f
    0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f
    -0.5f;  0.5f; -0.5f;  0.0f;  0.0f; -1.0f
    -0.5f; -0.5f; -0.5f;  0.0f;  0.0f; -1.0f

    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f
    0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f
    0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f
    0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f
    -0.5f;  0.5f;  0.5f;  0.0f;  0.0f;  1.0f
    -0.5f; -0.5f;  0.5f;  0.0f;  0.0f;  1.0f

    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f
    -0.5f;  0.5f; -0.5f; -1.0f;  0.0f;  0.0f
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f
    -0.5f; -0.5f; -0.5f; -1.0f;  0.0f;  0.0f
    -0.5f; -0.5f;  0.5f; -1.0f;  0.0f;  0.0f
    -0.5f;  0.5f;  0.5f; -1.0f;  0.0f;  0.0f

    0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f
    0.5f;  0.5f; -0.5f;  1.0f;  0.0f;  0.0f
    0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f
    0.5f; -0.5f; -0.5f;  1.0f;  0.0f;  0.0f
    0.5f; -0.5f;  0.5f;  1.0f;  0.0f;  0.0f
    0.5f;  0.5f;  0.5f;  1.0f;  0.0f;  0.0f

    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f
    0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f
    0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f
    0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f
    -0.5f; -0.5f;  0.5f;  0.0f; -1.0f;  0.0f
    -0.5f; -0.5f; -0.5f;  0.0f; -1.0f;  0.0f

    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f
    0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f
    0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f
    0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f
    -0.5f;  0.5f;  0.5f;  0.0f;  1.0f;  0.0f
    -0.5f;  0.5f; -0.5f;  0.0f;  1.0f;  0.0f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.LightingShaderOpt |> Option.iter Shaders.dispose
    model.LampShaderOpt |> Option.iter Shaders.dispose

let OnMouseWheel (model:Model) (scrollWheel:ScrollWheel) : Model =
    { model with Zoom = Math.Clamp (model.Zoom - scrollWheel.Y, 1f, 45f) }

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

let private setUniform name value shader =
    shader |> Option.iter (Shaders.setUniform name value >> printError)
    shader

let private projMatrix (model:Model) =
    Matrix4x4.CreatePerspectiveFieldOfView (
        model.Zoom |> degreesToRadians,
        (float32 model.Width) / (float32 model.Height),
        0.1f,
        100f )

let renderLampCube (model:Model) =
    model.LampShaderOpt |> Option.iter Shaders.useProgram
    let lampMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateScale 0.2f
        * Matrix4x4.CreateTranslation model.LampPosition

    model.LampShaderOpt
    |> setUniform "uModel" (lampMatrix |> Shaders.M4)
    |> setUniform "uView" (model.Camera |> Cameras.viewMatrix |> Shaders.M4)
    |> setUniform "uProjection" (model |> projMatrix |> Shaders.M4)
    |> setUniform "color" (model.LampColor |> Shaders.V3)
    |> ignore
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)

let RenderLitCube (model:Model) =
    model.LightingShaderOpt |> Option.iter Shaders.useProgram

    //Setting a uniform.
    model.LightingShaderOpt
    |> setUniform "uModel" (25f |> degreesToRadians |> Matrix4x4.CreateRotationY |> Shaders.M4)
    |> setUniform "uView" (model.Camera |> Cameras.viewMatrix |> Shaders.M4)
    |> setUniform "uProjection" (model |> projMatrix |> Shaders.M4)
    |> setUniform "viewPos" (model.Camera.Position |> Shaders.V3)
    |> setUniform "material.ambient" (Vector3 (1f,0.5f,0.31f) |> Shaders.V3)
    |> setUniform "material.diffuse" (Vector3 (1f,0.5f,0.31f) |> Shaders.V3)
    |> setUniform "material.specular" (Vector3 (0.5f,0.5f,0.5f) |> Shaders.V3)
    |> setUniform "material.shininess" (32f |> Shaders.Float)
    |> ignore
    
    let diffuseColor = model.LampColor * Vector3 0.5f
    let ambientColor = diffuseColor * Vector3 0.2f

    model.LightingShaderOpt
    |> setUniform "light.ambient" (ambientColor |> Shaders.V3)
    |> setUniform "light.diffuse" (diffuseColor |> Shaders.V3)
    |> setUniform "light.specular" (Vector3.One |> Shaders.V3)
    |> setUniform "light.position" (model.LampPosition |> Shaders.V3)
    |> ignore

    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear
        (ClearBufferMask.ColorBufferBit |||
        ClearBufferMask.DepthBufferBit)
    
    //Binding and using our VAO and shader.
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

    let lampPosition = //model.LampPosition
        Vector3.Transform (
            model.LampPosition,
            Matrix4x4.CreateRotationY 0.01f)
        
    let difference = float32 (DateTime.UtcNow - model.StartTime).TotalSeconds
    let lightColor = Vector3 (
        MathF.Sin (difference * 2f),
        MathF.Sin (difference * 0.7f),
        MathF.Sin (difference * 1.3f) )

    { model with
        Camera = { model.Camera with Position = cameraPosition }
        LampColor = lightColor
        LampPosition = lampPosition }


let onLoad (window:IWindow) : Model =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards.FirstOrDefault ()
    let mouse = inputContext.Mice.FirstOrDefault ()
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 6u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 3u 6u 3u

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
          Pitch= 0f }

    {   Window = window
        Gl = gl
        vbo = vbo
        vao = vao
        LightingShaderOpt = lightingShaderOpt
        LampShaderOpt = lampShaderOpt
        LampPosition = Vector3 (1.2f, 1f, 2f)
        LampColor = Vector3.One
        Camera = camera
        Zoom = 45f
        Width = window.Size.X
        Height = window.Size.Y
        Keyboard = keyboard
        Mouse = mouse
        LastMousePosition = None
        StartTime = DateTime.UtcNow }

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "3.4 - Materials"
    options.PreferredDepthBufferBits <- 24
    use window = Window.Create options

    window.add_Load (fun _ ->
        let mutable model = onLoad window
        
        model.Keyboard.add_KeyDown (keyDown window)
        model.Mouse.add_MouseMove (fun _ pos -> model <- onMouseMove model pos)
        model.Mouse.add_Scroll (fun _ scrollWheel -> model <- OnMouseWheel model scrollWheel)

        window.add_Update (fun deltaTime -> model <- onUpdate model deltaTime)
        window.add_Render (onRender model)
        window.add_Closing (fun _ -> onClose model)
        window.add_FramebufferResize (onFramebufferResize model.Gl))
    
    window.Run ()
    window.Dispose ()
    0