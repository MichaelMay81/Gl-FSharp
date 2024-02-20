namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

//The data type of the BufferObject.
//Holds the same metadata as the generic BufferObject type, but provides runtime type decisions. 
type DataType =
    | Float
    | UInt

//Our buffer object abstraction.
type BufferObject<'T> = {
    Handle : uint
    BufferType : BufferTargetARB
    Gl : GL
    DataType : DataType }

module BufferObjects =
    let dispose (bufferObject:BufferObject<'T>) : unit =
        //Remember to delete our buffer.
        bufferObject.Gl.DeleteBuffer bufferObject.Handle

    let bind (bufferObject:BufferObject<'T>) : unit =
        //Binding the buffer object, with the correct buffer type.
        bufferObject.Gl.BindBuffer (bufferObject.BufferType, bufferObject.Handle)

    let private create (gl:GL) (bufferType:BufferTargetARB) (dataType:DataType) (data:'T []) : BufferObject<'T> =
        //Getting the handle, and then uploading the data to said handle.
        let handle = gl.GenBuffer ()
        gl.BindBuffer (bufferType, handle)
        gl.BufferData (
            bufferType,
            ReadOnlySpan<'T> data,
            BufferUsageARB.StaticDraw)

        //Setting the gl instance and storing our buffer type.
        { DataType = dataType
          BufferType = bufferType
          Gl = gl
          Handle = handle }

    let createFloat (gl:GL) (bufferType:BufferTargetARB) (data:float32 []) : BufferObject<float32> =
        create gl bufferType Float data

    let createUInt (gl:GL) (bufferType:BufferTargetARB) (data:uint32 []) : BufferObject<uint32> =
        create gl bufferType UInt data
