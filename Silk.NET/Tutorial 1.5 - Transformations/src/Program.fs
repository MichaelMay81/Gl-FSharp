﻿open System
open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input

open Tutorial1_4_Abstractions
open Tutorial1_5_Transformations

type Model = {
    Window : IWindow
    Gl : GL
    Keyboard: IKeyboard

    vbo : VertexBufferObject
    ebo : ElementBufferObject
    vao : VertexArrayObject
    Texture : Texture
    Shader : Shader
    //Creating transforms for the transformations
    Transforms : Transform[]}

let private vertices = [|
    //X    Y      Z     U   V
     0.5f;  0.5f; 0.0f; 1.0f; 0.0f;
     0.5f; -0.5f; 0.0f; 1.0f; 1.0f;
    -0.5f; -0.5f; 0.0f; 0.0f; 1.0f;
    -0.5f;  0.5f; 0.0f; 0.0f; 0.0f |]

let private indices = [|
    0u; 1u; 3u
    1u; 2u; 3u |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo.BufferObject |> BufferObjects.dispose
    model.ebo.BufferObject |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.Shader |> Shaders.dispose
    model.Texture |> Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    model.vao |> VertexArrayObjects.bind
    model.Texture |> Textures.bindSlot0
    model.Shader |> Shaders.useProgram
    let shaderWerror func = model.Shader |> func |> printError

    Shaders.setUniformInt "uTexture0" 0 |> shaderWerror
    
    model.Transforms
    |> Array.iter (fun transform ->
        //Using the transformations.
        Shaders.setUniformMat4 "uModel" (transform |> Transforms.viewMatrix) |> shaderWerror

        model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ()) )

let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head

    let gl = GL.GetApi window

    let ebo = BufferObjects.createEBO gl BufferTargetARB.ElementArrayBuffer indices
    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo (Some ebo)
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt = 
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    let textureOpt =
        Textures.createFromFile gl "silk.png"
        |> resultToOption

    let transforms =[|
        //Unlike in the transformation, because of our abstraction, order doesn't matter here.
        //Translation.
        { Transforms.init with Position = Vector3 (0.5f,0.5f,0f)}
        //Rotation.
        { Transforms.init with Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)}
        //Scaling.
        { Transforms.init with Scale = 0.5f }
        //Mixed transformation.
        { Transforms.init with
            Position = Vector3 (-0.5f, 0.5f, 0f)
            Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)
            Scale = 0.5f } |]

    match shaderOpt, textureOpt with
    | Some shader, Some texture ->
        Some {  Window = window
                Gl = gl
                Keyboard = keyboard
                vbo = vbo
                ebo = ebo
                vao = vao
                Shader = shader
                Texture = texture
                Transforms = transforms }
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
    options.Title <- "1.5 - Transformations"
    use window = Window.Create options

    window.add_Load (fun _ ->
        match onLoad window with
        | Some model ->
            window.add_Render (onRender model)
            window.add_Closing (fun _ -> onClose model)
        | None ->
            window.Close () )
    
    window.Run ()
    window.Dispose ()
    0