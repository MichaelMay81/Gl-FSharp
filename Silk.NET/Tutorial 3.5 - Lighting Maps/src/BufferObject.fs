namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

// type
type DataType =
    | Float
    | UInt

type BufferObject<'T> = {
    Handle : uint
    BufferType : BufferTargetARB
    Gl : GL
    DataType : DataType }

module BufferObjects =
    let dispose (bufferObject:BufferObject<'T>) : unit =
        bufferObject.Gl.DeleteBuffer bufferObject.Handle

    let bind (bufferObject:BufferObject<'T>) : unit =
        bufferObject.Gl.BindBuffer (bufferObject.BufferType, bufferObject.Handle)

    let private create (gl:GL) (bufferType:BufferTargetARB) (dataType:DataType) (data:'T []) : BufferObject<'T> =
        let handle = gl.GenBuffer ()
        gl.BindBuffer (bufferType, handle)

        gl.BufferData (
            bufferType,
            ReadOnlySpan<'T> data,
            BufferUsageARB.StaticDraw)

        { DataType = dataType
          BufferType = bufferType
          Gl = gl
          Handle = handle }

    let createFloat (gl:GL) (bufferType:BufferTargetARB) (data:float32 []) : BufferObject<float32> =
        create gl bufferType Float data

    let createUInt (gl:GL) (bufferType:BufferTargetARB) (data:uint32 []) : BufferObject<uint32> =
        create gl bufferType UInt data
