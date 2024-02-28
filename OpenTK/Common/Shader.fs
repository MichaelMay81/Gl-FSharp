module LearnOpenTK.Common

open System
open System.IO
open System.Text
open System.Collections.Generic
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics

type Shader = {
    Handle: int
    UniformLocations: Map<string, int> }

module Shaders =
    let useProgram (shader:Shader) =
        GL.UseProgram shader.Handle

    let private compileShader (shader:int) : Result<int, string> =
        GL.CompileShader shader
        let code = GL.GetShader (shader, ShaderParameter.CompileStatus)
        match code = int All.True with
        | true -> Ok shader
        | _ ->
            let infoLog = GL.GetShaderInfoLog shader
            Error $"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}"

    let private createShader (path:string) (shaderType:ShaderType) : Result<int, string> =
        try File.ReadAllText path |> Ok
        with
        | :? DirectoryNotFoundException
        | :? FileNotFoundException ->
            Error $"Couldn't find shader {path}"
        |> Result.bind (fun shaderSource ->
            let vertexShader = GL.CreateShader shaderType
            GL.ShaderSource (vertexShader, shaderSource)
        
            compileShader vertexShader)

    let private linkProgram (program:int) : Result<unit, string> =
        GL.LinkProgram program
        let code = GL.GetProgram (program, GetProgramParameterName.LinkStatus)
        match code = int All.True with
        | true -> Ok ()
        | _ ->
            Error $"Error occurred whilst linking Program({program})"

    let init (vertPath:string) (fragPath:string) : Result<Shader, string> =
        // let shaderSource = File.ReadAllText vertPath
        // let vertexShader = GL.CreateShader ShaderType.VertexShader
        // GL.ShaderSource (vertexShader, shaderSource)
        
        // let vertShaderResult = compileShader vertexShader
        
        // let shaderSource = File.ReadAllText fragPath
        // let fragmentShader = GL.CreateShader ShaderType.FragmentShader
        // GL.ShaderSource (fragmentShader, shaderSource)

        // let fragShaderResult = compileShader fragmentShader

        let vertShaderResult = createShader vertPath ShaderType.VertexShader
        let fragShaderResult = createShader fragPath ShaderType.FragmentShader

        match vertShaderResult, fragShaderResult with
        | Ok vertexShader, Ok fragmentShader ->
            let handle = GL.CreateProgram ()

            GL.AttachShader (handle, vertexShader)
            GL.AttachShader (handle, fragmentShader)

            linkProgram handle
            |> Result.map (fun _ ->
                GL.DetachShader (handle, vertexShader)
                GL.DetachShader (handle, fragmentShader)
                GL.DeleteShader fragmentShader
                GL.DeleteShader vertexShader

                let numberOfUniforms = GL.GetProgram (handle, GetProgramParameterName.ActiveUniforms)
                
                let uniformLocations =
                    [ 0 .. numberOfUniforms ]
                    |> List.map (fun i ->
                        let key, _, _ = GL.GetActiveUniform (handle, i)
                        let location = GL.GetUniformLocation (handle, key)
                        key, location)
                    |> Map

                { Handle = handle
                  UniformLocations = uniformLocations }
            )
        | Error err1, Error err2 ->
            Error (err1 + "\n" + err2)
        | Error err, _
        | _, Error err ->
            Error err