namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

type VertexArrayObject = {
    Handle : uint
    Gl : GL
    VertexBufferDataType : DataType }

module VertexArrayObjects =
    let dispose (vao:VertexArrayObject) =
        vao.Gl.DeleteVertexArray vao.Handle

    let bind (vao:VertexArrayObject) : unit =
        vao.Gl.BindVertexArray vao.Handle

    let vertexAttributePointer (index:uint) (count:uint) (vertexSize:uint) (offset:uint) (vao:VertexArrayObject) =
        let dataSize, vapType =
            match vao.VertexBufferDataType with
            | Float -> sizeof<float32>, VertexAttribPointerType.Float
            | UInt -> sizeof<uint32>, VertexAttribPointerType.UnsignedInt

        vao.Gl.VertexAttribPointer (
            index,
            int count,
            vapType,
            false,
            vertexSize * uint dataSize,
            IntPtr(int offset * dataSize).ToPointer ())
        vao.Gl.EnableVertexAttribArray index

    let create (gl:GL) (vbo:BufferObject<'V>) (ebo:BufferObject<uint32>) =
        let handle = gl.GenVertexArray ()

        gl.BindVertexArray handle
        vbo |> BufferObjects.bind
        ebo |> BufferObjects.bind

        { Handle = handle
          Gl = gl
          VertexBufferDataType = vbo.DataType }