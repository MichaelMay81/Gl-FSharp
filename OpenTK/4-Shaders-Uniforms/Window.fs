module LearnOpenTK.Window

open System;
open System.Diagnostics;
open LearnOpenTK.Common
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Windowing.Desktop

// This project will explore how to use uniform variable type which allows you to assign values
// to shaders at any point during the project.

let private vertices = [|
    -0.5f; -0.5f; 0.0f; // Bottom-left vertex
     0.5f; -0.5f; 0.0f; // Bottom-right vertex
     0.0f;  0.5f; 0.0f|]// Top vertex

type Model = {
    // So we're going make the triangle pulsate between a color range.
    // In order to do that, we'll need a constantly changing value.
    // The stopwatch is perfect for this as it is constantly going up.
    Timer: Stopwatch
    VertexBufferObject: int
    VertexArrayObject: int
    Shader: Shader}

let onLoad () : Result<Model, string> =
    GL.ClearColor (0.2f, 0.3f, 0.3f, 1.0f)
    
    let vertexBufferObject = GL.GenBuffer ()

    GL.BindBuffer (BufferTarget.ArrayBuffer, vertexBufferObject)
    GL.BufferData (
        BufferTarget.ArrayBuffer,
        (vertices |> Array.length) * sizeof<float32>,
        vertices,
        BufferUsageHint.StaticDraw)

    let vertexArrayObject = GL.GenVertexArray ()
    GL.BindVertexArray vertexArrayObject

    GL.VertexAttribPointer (0, 3, VertexAttribPointerType.Float, false, 3 * sizeof<float32>, 0)
    GL.EnableVertexAttribArray 0

    let maxAttributeCount = GL.GetInteger GetPName.MaxVertexAttribs
    printfn "Maximum number of vertex attributes supported: %i" maxAttributeCount

    Shaders.init "Shaders/shader.vert" "Shaders/shader.frag"
    |> Result.map (fun shader ->
        shader |> Shaders.useProgram
        let timer = Stopwatch ()
        timer.Start ()

        { Timer = timer
          VertexBufferObject = vertexBufferObject
          VertexArrayObject = vertexArrayObject
          Shader = shader })

let onRenderFrame (window:GameWindow) (model:Model) (e:FrameEventArgs) =
    GL.Clear ClearBufferMask.ColorBufferBit

    model.Shader |> Shaders.useProgram

    // Here, we get the total seconds that have elapsed since the last time this method has reset
    // and we assign it to the timeValue variable so it can be used for the pulsating color.
    let timeValue = model.Timer.Elapsed.TotalSeconds

    // We're increasing / decreasing the green value we're passing into
    // the shader based off of timeValue we created in the previous line,
    // as well as using some built in math functions to help the change be smoother.
    let greenValue = (timeValue |> Math.Sin |> float32) / 2f + 0.5f
    
    // This gets the uniform variable location from the frag shader so that we can 
    // assign the new green value to it.
    let vertexColorLocation = GL.GetUniformLocation (model.Shader.Handle, "ourColor")

    // Here we're assigning the ourColor variable in the frag shader 
    // via the OpenGL Uniform method which takes in the value as the individual vec values (which total 4 in this instance).
    GL.Uniform4 (vertexColorLocation, 0f, greenValue, 0f, 1f)

    // You can alternatively use this overload of the same function.
    // GL.Uniform4(vertexColorLocation, new OpenTK.Mathematics.Color4(0f, greenValue, 0f, 0f));

    // Bind the VAO
    GL.BindVertexArray model.VertexBufferObject

    GL.DrawArrays (PrimitiveType.Triangles, 0, 3)

    window.SwapBuffers ()

let onUpdateFrame (window:GameWindow) (_:FrameEventArgs) =
    if window.KeyboardState.IsKeyDown Keys.Escape then
        window.Close ()

let onResize (window:GameWindow) (_:ResizeEventArgs) =
    GL.Viewport (0, 0, window.Size.X, window.Size.Y)