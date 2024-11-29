namespace LearnOpenTK.Common

open System.IO
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics

// A simple type meant to help create shaders.
type Shader = {
    Handle: int
    UniformLocations: Map<string, int> }

// A simple module meant to help create shaders.
module Shaders =

    // Uniform setters
    // Uniforms are variables that can be set by user code, instead of reading them from the VBO.
    // You use VBOs for vertex-related data, and uniforms for almost everything else.

    // Setting a uniform is almost always the exact same, so I'll explain it here once, instead of in every method:
    //     1. Bind the program you want to set the uniform on
    //     2. Get a handle to the location of the uniform with GL.GetUniformLocation.
    //     3. Use the appropriate GL.Uniform* function to set the uniform.

    let private getLocation (name:string) (shader:Shader) : Result<int,string> =
        shader.UniformLocations
        |> Map.tryFind name
        |> function
        | None -> Error $"{name} uniform not found on shader."
        | Some location ->
            Ok location

    /// <summary>
    /// Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    let setInt (name:string) (data:int) (shader:Shader) : Result<unit, string> =
        getLocation name shader
        |> Result.map (fun location ->
            GL.UseProgram shader.Handle
            GL.Uniform1 (location, data))

    /// <summary>
    /// Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    let setFloat (name:string) (data:float) (shader:Shader) : Result<unit, string> =
        getLocation name shader
        |> Result.map (fun location ->
            GL.UseProgram shader.Handle
            GL.Uniform1 (location, data))
    
    /// <summary>
    /// Set a uniform Matrix4 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <remarks>
    ///   <para>
    ///   The matrix is transposed before being sent to the shader.
    ///   </para>
    /// </remarks>
    let setMatrix (name:string) (data:Matrix4) (shader:Shader) : Result<unit, string> =
        getLocation name shader
        |> Result.map (fun location ->
            GL.UseProgram shader.Handle
            GL.UniformMatrix4 (location, true, ref data))

    /// <summary>
    /// Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    let setVector3 (name:string) (data:Vector3) (shader:Shader) : Result<unit, string> =
        getLocation name shader
        |> Result.map (fun location ->
            GL.UseProgram shader.Handle
            GL.Uniform3 (location, data))

    // The shader sources provided with this project use hardcoded layout(location)-s. If you want to do it dynamically,
    // you can omit the layout(location=X) lines in the vertex shader, and use this in VertexAttribPointer instead of the hardcoded values.
    let getAttribLocation (attribName:string) (shader:Shader) : int =
        GL.GetAttribLocation (shader.Handle, attribName)

    // A wrapper function that enables the shader program.
    let useProgram (shader:Shader) =
        GL.UseProgram shader.Handle

    let private linkProgram (program:int) : Result<unit, string> =
        // We link the program
        GL.LinkProgram program

        // Check for linking errors
        let code = GL.GetProgram (program, GetProgramParameterName.LinkStatus)
        match code = int All.True with
        | true -> Ok ()
        | _ ->
            // We can use `GL.GetProgramInfoLog(program)` to get information about the error.
            let infoLog = GL.GetProgramInfoLog program
            Error $"Error occurred whilst linking Program({program}).\n\n{infoLog}"

    let private compileShader (shader:int) : Result<int, string> =
        // Try to compile the shader
        GL.CompileShader shader

        // Check for compilation errors
        let code = GL.GetShader (shader, ShaderParameter.CompileStatus)
        match code = int All.True with
        | true -> Ok shader
        | _ ->
            // We can use `GL.GetShaderInfoLog(shader)` to get information about the error.
            let infoLog = GL.GetShaderInfoLog shader
            Error $"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}"

    let private createShader (path:string) (shaderType:ShaderType) : Result<int, string> =
        let path = Path.Combine (System.AppContext.BaseDirectory, path)
        
        // Load shader and compile
        try File.ReadAllText path |> Ok
        with
        | :? DirectoryNotFoundException
        | :? FileNotFoundException ->
            Error $"Couldn't find shader {path}"
        |> Result.bind (fun shaderSource ->
            // GL.CreateShader will create an empty shader (obviously). The ShaderType enum denotes which type of shader will be created.
            let vertexShader = GL.CreateShader shaderType

            // Now, bind the GLSL source code
            GL.ShaderSource (vertexShader, shaderSource)
        
            // And then compile
            compileShader vertexShader)

    // This is how you create a simple shader.
    // Shaders are written in GLSL, which is a language very similar to C in its semantics.
    // The GLSL source is compiled *at runtime*, so it can optimize itself for the graphics card it's currently being used on.
    // A commented example of GLSL can be found in shader.vert.
    let init (vertPath:string) (fragPath:string) : Result<Shader, string> =
        // There are several different types of shaders, but the only two you need for basic rendering are the vertex and fragment shaders.
        // The vertex shader is responsible for moving around vertices, and uploading that data to the fragment shader.
        //   The vertex shader won't be too important here, but they'll be more important later.
        // The fragment shader is responsible for then converting the vertices to "fragments", which represent all the data OpenGL needs to draw a pixel.
        //   The fragment shader is what we'll be using the most here.
        let vertShaderResult = createShader vertPath ShaderType.VertexShader
        let fragShaderResult = createShader fragPath ShaderType.FragmentShader

        match vertShaderResult, fragShaderResult with
        | Ok vertexShader, Ok fragmentShader ->
            // These two shaders must then be merged into a shader program, which can then be used by OpenGL.
            // To do this, create a program...
            let handle = GL.CreateProgram ()

            // Attach both shaders...
            GL.AttachShader (handle, vertexShader)
            GL.AttachShader (handle, fragmentShader)

            // And then link them together.
            linkProgram handle
            |> Result.map (fun _ ->
                // When the shader program is linked, it no longer needs the individual shaders attached to it; the compiled code is copied into the shader program.
                // Detach them, and then delete them.
                GL.DetachShader (handle, vertexShader)
                GL.DetachShader (handle, fragmentShader)
                GL.DeleteShader fragmentShader
                GL.DeleteShader vertexShader

                // The shader is now ready to go, but first, we're going to cache all the shader uniform locations.
                // Querying this from the shader is very slow, so we do it once on initialization and reuse those values
                // later.

                // First, we have to get the number of active uniforms in the shader.
                let numberOfUniforms = GL.GetProgram (handle, GetProgramParameterName.ActiveUniforms)
                
                let uniformLocations =
                    // Loop over all the uniforms,
                    [ 0 .. numberOfUniforms ]
                    |> List.map (fun i ->
                        // get the name of this uniform,
                        let key, _, _ = GL.GetActiveUniform (handle, i)
                        // get the location,
                        let location = GL.GetUniformLocation (handle, key)
                        key, location)
                    // Next, create the Map to hold the locations.
                    |> Map

                { Handle = handle
                  UniformLocations = uniformLocations }
            )
        | Error err1, Error err2 ->
            Error (err1 + "\n" + err2)
        | Error err, _
        | _, Error err ->
            Error err