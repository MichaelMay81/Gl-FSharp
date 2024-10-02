module LearnOpenTK.Window

open System;
open System.Diagnostics;
open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// Because we're adding a texture, we modify the vertex array to include texture coordinates.
// Texture coordinates range from 0.0 to 1.0, with (0.0, 0.0) representing the bottom left, and (1.0, 1.0) representing the top right.
// The new layout is three floats to create a vertex, then two floats to create the coordinates.
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
    // For documentation on this, check Texture.fs.
    Texture: Texture }

let onLoad () : Result<Model, string> =
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)
    
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

    // The shaders have been modified to include the texture coordinates, check them out after finishing the OnLoad function.
    let shaderResult = Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"

    let textureResult = Textures.loadFromFile "Resources/container.png"

    match shaderResult, textureResult with
    | Ok shader, Ok texture ->
        shader |> Shaders.useProgram
        let timer = Stopwatch ()
        timer.Start ()

        // Because there's now 5 floats between the start of the first vertex and the start of the second,
        // we modify the stride from 3 * sizeof(float) to 5 * sizeof(float).
        // This will now pass the new vertex array to the buffer.
        let vertexLocation = shader |> Shaders.getAttribLocation "aPosition"
        GL.EnableVertexAttribArray vertexLocation
        GL.VertexAttribPointer (vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 0)

        // Next, we also setup texture coordinates. It works in much the same way.
        // We add an offset of 3, since the texture coordinates comes after the position data.
        // We also change the amount of data to 2 because there's only 2 floats for texture coordinates.
        let texCoordLocation = shader |> Shaders.getAttribLocation "aTexCoord"
        GL.EnableVertexAttribArray texCoordLocation
        GL.VertexAttribPointer (texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof<float32>, 3 * sizeof<float32>)

        texture |> Textures.useTexture TextureUnit.Texture0

        { ElementBufferObject = elementBufferObject
          VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader
          Texture = texture} |> Ok
    | Error error1, Error error2 ->
        Error (error1 + error2)
    | Error error, _ | _, Error error ->
        Error error

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    model.Shader |> Shaders.useProgram

    GL.BindVertexArray model.VertexBufferObject

    model.Texture |> Textures.useTexture TextureUnit.Texture0
    model.Shader |> Shaders.useProgram

    GL.DrawElements (PrimitiveType.Triangles, indices |> Array.length, DrawElementsType.UnsignedInt, 0)

    window.SwapBuffers ()

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let onResize (window:GameWindow) (_:ResizeEventArgs) =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)