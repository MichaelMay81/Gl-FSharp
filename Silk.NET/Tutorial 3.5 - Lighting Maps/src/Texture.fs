namespace Tutorial1_4_Abstractions

open System
open System.IO
open Silk.NET.OpenGL
open StbImageSharp

type Texture = {
    Handle : uint
    Gl : GL }

module Textures =
    let dispose (texture:Texture) =
        texture.Gl.DeleteTexture texture.Handle

    let bind (textureSlot:TextureUnit) (texture:Texture) =
        texture.Gl.ActiveTexture textureSlot
        texture.Gl.BindTexture (TextureTarget.Texture2D, texture.Handle)

    let bindSlot0 (texture:Texture) =
        bind TextureUnit.Texture0 texture

    let SetParameters (gl:GL) =
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge)
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge)
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.LinearMipmapLinear)
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear)
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0)
        gl.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8)

        gl.GenerateMipmap TextureTarget.Texture2D

    let create (gl:GL) (data:ReadOnlySpan<byte>) (width:uint) (height:uint) =
        let texture = {
            Handle = gl.GenTexture ()
            Gl = gl }
        
        bindSlot0 texture

        gl.TexImage2D (
            TextureTarget.Texture2D,
            0,
            InternalFormat.Rgba,
            width,
            height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            data)

        SetParameters gl

        texture

    let createFromFile (gl:GL) (path:string) =
        try
            let data = File.ReadAllBytes path
            let image = ImageResult.FromMemory (data, ColorComponents.RedGreenBlueAlpha)
            create gl (ReadOnlySpan<byte> image.Data) (uint image.Width) (uint image.Height)
            |> Ok
        with
        | :? FileNotFoundException
        | :? DirectoryNotFoundException as ex ->
            Error ex.Message
