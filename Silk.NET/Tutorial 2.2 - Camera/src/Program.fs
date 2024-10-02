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
    Texture: Texture
    Shader: Shader

    Camera: Camera

    //Used to track change in mouse movement to allow for moving of the Camera
    LastMousePosition: Vector2 option }

let private vertices = [|
    //X    Y      Z     U   V
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f;
     0.5f; -0.5f; -0.5f;  1.0f; 0.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f;

    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 1.0f;
    -0.5f;  0.5f;  0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;

    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f;

     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;

    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  1.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;

    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo.BufferObject |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.Shader |> Shaders.dispose
    model.Texture |> Textures.dispose

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

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear
        (ClearBufferMask.ColorBufferBit |||
        ClearBufferMask.DepthBufferBit)

    let shaderWerror func = model.Shader |> func |> printError

    model.vao |> VertexArrayObjects.bind
    model.Texture |> Textures.bindSlot0
    model.Shader |> Shaders.useProgram
    shaderWerror <| Shaders.setUniformInt "uTexture0" 0

    //Use elapsed time to convert to radians to allow our cube to rotate over time
    let difference =
        model.Window.Time * 100.
        |> float32
        |> degreesToRadians

    let modelMat =
        Matrix4x4.CreateRotationY difference *
        Matrix4x4.CreateRotationX difference
    let viewMat = model.Camera |> Cameras.viewMatrix
    let projMat = model.Camera |> Cameras.projectionMatrix model.Width model.Height

    shaderWerror <| Shaders.setUniformMat4 "uModel" modelMat
    shaderWerror <| Shaders.setUniformMat4 "uView" viewMat
    shaderWerror <| Shaders.setUniformMat4 "uProjection" projMat
    |> ignore
    
    //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0, 36u)

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

    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt =
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    let textureOpt =
        Textures.createFromFile gl "silk.png"
        |> resultToOption

    //Used to track change in mouse movement to allow for moving of the Camera
    let camera = 
        {   Position= Vector3 (0f, 0f, 3f)
            Up= Vector3.UnitY
            Yaw= -90f
            Pitch= 0f
            Zoom = 45f }

    match shaderOpt, textureOpt with
    | Some shader, Some texture ->
        {   Window = window
            Gl = gl
            vbo = vbo
            vao = vao
            Shader = shader
            Texture = texture
            Camera = camera
            Width = window.Size.X
            Height = window.Size.Y
            Keyboard = keyboard
            Mouse = mouse
            LastMousePosition = None } |> Some
    | _ ->
        vbo.BufferObject |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        shaderOpt |> Option.iter Shaders.dispose
        textureOpt |> Option.iter Textures.dispose
        None

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "2.2 - Camera"
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