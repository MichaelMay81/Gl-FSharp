open System
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Tutorial1_4_Abstractions
open Silk.NET.Input

type Model = {
    Window : IWindow
    Gl : GL
    Keyboard: IKeyboard

    //Our new abstracted objects, here we specify what the types are.
    vbo : VertexBufferObject
    ebo : ElementBufferObject
    vao : VertexArrayObject

    Texture : Texture
    Shader : Shader}

let private vertices = [|
    0.5f;  0.5f; 0.0f; 1.0f; 1.0f
    0.5f; -0.5f; 0.0f; 1.0f; 0.0f
    -0.5f; -0.5f; 0.0f; 0.0f; 0.0f
    -0.5f;  0.5f; 0.0f; 0.0f; 1.0f |]

let private indices = [|
    0u; 1u; 3u
    1u; 2u; 3u |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    //Remember to dispose all the instances.
    model.vbo.BufferObject |> BufferObjects.dispose
    model.ebo.BufferObject |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.Shader |> Shaders.dispose
    model.Texture |> Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.Shader |> Shaders.useProgram

    model.Texture |> Textures.bindSlot0

    //Setting a uniform.
    model.Shader
    |> Shaders.setUniformInt "uTexture" 0
    |> printError

    model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ())

let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head

    let gl = GL.GetApi window

    //Instantiating our new abstractions
    let ebo = BufferObjects.createEBO gl BufferTargetARB.ElementArrayBuffer indices
    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo (Some ebo)
    
    //Telling the VAO object how to lay out the attribute pointers
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt = 
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    let textureOpt =
        Textures.createFromFile gl "silk.png"
        |> resultToOption

    match shaderOpt, textureOpt with
    | Some shader, Some texture ->
        Some {  Window = window
                Gl = gl
                Keyboard = keyboard
                vbo = vbo
                ebo = ebo
                vao = vao
                Shader = shader
                Texture = texture }
    | _ ->
        vbo.BufferObject |> BufferObjects.dispose
        ebo.BufferObject |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        shaderOpt |> Option.iter Shaders.dispose
        textureOpt |> Option.iter Textures.dispose
        None

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "1.4 - Abstractions"
    use window = Window.Create options

    window.add_Load (fun _ ->
        match onLoad window with
        | Some model ->
            model.Keyboard.add_KeyDown (keyDown window)
            window.add_Render (onRender model)
            window.add_Closing (fun _ -> onClose model)
        | None ->
            window.Close () )
    
    window.Run ()
    window.Dispose ()
    0