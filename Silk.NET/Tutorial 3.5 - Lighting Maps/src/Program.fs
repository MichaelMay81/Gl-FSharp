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
    LightingShader: Shader
    LampShader: Shader
    LampPosition: Vector3

    DiffuseMap: Texture
    SpecularMap: Texture

    Camera: Camera
    Zoom: float32
    LastMousePosition: Vector2 option
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
    model.vbo |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.LightingShader |> Shaders.dispose
    model.LampShader |> Shaders.dispose
    model.DiffuseMap |> Textures.dispose
    model.SpecularMap |> Textures.dispose

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

let private projMatrix (model:Model) =
    Matrix4x4.CreatePerspectiveFieldOfView (
        model.Zoom |> degreesToRadians,
        (float32 model.Width) / (float32 model.Height),
        0.1f,
        100f )

let renderLampCube (model:Model) =
    let shaderWerror func = model.LampShader |> func |> printError
    
    model.LampShader |> Shaders.useProgram
    let lampMatrix =
        Matrix4x4.Identity
        * Matrix4x4.CreateScale 0.2f
        * Matrix4x4.CreateTranslation model.LampPosition

    shaderWerror <| Shaders.setUniformMat4 "uModel" lampMatrix
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model |> projMatrix)
    shaderWerror <| Shaders.setUniformVec3 "color" Vector3.One

    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)

let RenderLitCube (model:Model) =
    let shaderWerror func = model.LightingShader |> func |> printError
    
    model.LightingShader |> Shaders.useProgram

    model.DiffuseMap |> Textures.bindSlot0
    model.SpecularMap |> (Textures.bind TextureUnit.Texture1)

    //Setting a uniform.
    shaderWerror <| Shaders.setUniformMat4 "uModel" Matrix4x4.Identity
    shaderWerror <| Shaders.setUniformMat4 "uView" (model.Camera |> Cameras.viewMatrix)
    shaderWerror <| Shaders.setUniformMat4 "uProjection" (model |> projMatrix)
    shaderWerror <| Shaders.setUniformVec3 "viewPos" (model.Camera.Position)
    shaderWerror <| Shaders.setUniformInt "material.diffuse" 0
    shaderWerror <| Shaders.setUniformInt "material.specular" 1
    shaderWerror <| Shaders.setUniformFloat "material.shininess" 32f
    
    let diffuseColor = Vector3 0.5f
    let ambientColor = diffuseColor * Vector3 0.2f

    shaderWerror <| Shaders.setUniformVec3 "light.ambient" ambientColor
    shaderWerror <| Shaders.setUniformVec3 "light.diffuse" diffuseColor
    shaderWerror <| Shaders.setUniformVec3 "light.specular" Vector3.One
    shaderWerror <| Shaders.setUniformVec3 "light.position" model.LampPosition

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

    { model with
        Camera = { model.Camera with Position = cameraPosition }
        LampPosition = lampPosition }


let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards.FirstOrDefault ()
    let mouse = inputContext.Mice.FirstOrDefault ()
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 8u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 3u 8u 3u
    vao |> VertexArrayObjects.vertexAttributePointer 2u 2u 8u 6u

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

    let diffuseMapOpt = Textures.createFromFile gl "../Assets/silkBoxed.png" |> resultToOption
    let specularMapOpt = Textures.createFromFile gl "../Assets/silkSpecular.png" |> resultToOption

    match lightingShaderOpt, lampShaderOpt, diffuseMapOpt, specularMapOpt with
    | Some lightingShader, Some lampShader, Some diffuseMap, Some specularMap ->
        Some {  Window = window
                Gl = gl
                vbo = vbo
                vao = vao
                LightingShader = lightingShader
                LampShader = lampShader
                LampPosition = Vector3 (1.2f, 0.5f, 2f)
                DiffuseMap = diffuseMap
                SpecularMap = specularMap
                Camera = camera
                Zoom = 45f
                Width = window.Size.X
                Height = window.Size.Y
                Keyboard = keyboard
                Mouse = mouse
                LastMousePosition = None
                StartTime = DateTime.UtcNow }
    | _ ->
        vbo |> BufferObjects.dispose
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