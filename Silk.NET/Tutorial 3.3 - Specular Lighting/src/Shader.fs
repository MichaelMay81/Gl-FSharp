namespace Tutorial1_4_Abstractions

open System
open System.IO
open System.Numerics
open Silk.NET.OpenGL

type Shader = {
    Handle: uint
    Gl: GL }

module Shaders =
    type UniformType =
    | Float of float
    | Int of int
    | M4 of Matrix4x4
    | V3 of Vector3

    let (|IsNullOrWhiteSpace|SomeString|) (str : string) =
        if str |> String.IsNullOrWhiteSpace
        then IsNullOrWhiteSpace
        else SomeString str
    
    let private loadShader (gl:GL) (shaderType:ShaderType) (path:string) : Result<uint, string> =
        let srcOpt =
            try
                File.ReadAllText path |> Ok
            with
            | :? FileNotFoundException as ex ->
                Error ex.Message

        srcOpt
        |> Result.bind (fun src ->
            let handle = gl.CreateShader shaderType
            gl.ShaderSource (handle, src)
            gl.CompileShader handle

            match gl.GetShaderInfoLog handle with
            | IsNullOrWhiteSpace ->
                Ok handle
            | SomeString infoLog ->
                Error $"Error compiling shader of type {shaderType}, failed with error {infoLog}")

    let dispose (shader:Shader) : unit =
        shader.Gl.DeleteProgram shader.Handle

    let setUniform (name:string) (value:UniformType) (shader:Shader) : Result<unit, string> =
        match shader.Gl.GetUniformLocation (shader.Handle, name), value with
        | -1, _ ->
            Error $"{name} uniform not found on shader."
        | location, Float value ->
            shader.Gl.Uniform1 (location, value)
            Ok ()
        | location, Int value ->
            shader.Gl.Uniform1 (location, value) 
            Ok ()
        | location, M4 value ->
            shader.Gl.UniformMatrix4 (location, 1u, false, &value.M11)
            Ok ()
        | location, V3 value ->
            shader.Gl.Uniform3 (location, value.X, value.Y, value.Z)
            Ok ()

    let useProgram (shader:Shader) =
        shader.Gl.UseProgram shader.Handle

    let create (gl:GL) (vertexPath:string) (fragmentPath:string) : Result<Shader, string> =
        let vertexRes = loadShader gl ShaderType.VertexShader vertexPath
        let fragmentRes = loadShader gl ShaderType.FragmentShader fragmentPath
        
        match vertexRes, fragmentRes with
        | Ok vertex, Ok fragment ->
            let handle = gl.CreateProgram ()
            gl.AttachShader (handle, vertex)
            gl.AttachShader (handle, fragment)
            gl.LinkProgram handle
            
            gl.DetachShader (handle, vertex)
            gl.DetachShader (handle, fragment)
            gl.DeleteShader vertex
            gl.DeleteShader fragment
            
            match gl.GetProgram (handle, GLEnum.LinkStatus) with
            | 0 ->
                Error $"Program failed to link with error: {gl.GetProgramInfoLog(handle)}"
            | _ ->
                Ok { Handle=handle; Gl=gl }
        | Error err1, Error err2 -> Error $"{err1} {err2}"
        | Error error, _
        | _, Error error -> Error error
        