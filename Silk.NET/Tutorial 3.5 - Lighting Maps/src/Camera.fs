namespace Tutorial1_4_Abstractions

open System
open System.Numerics

type Camera = {
    Position: Vector3
    Up: Vector3
    Yaw: float32
    Pitch: float32
    Zoom: float32 }

module Cameras =
    let modifyZoom (zoomAmount:float32) (camera:Camera) : Camera =
        //We don't want to be able to zoom in too close or too far away so clamp to these values
        { camera with Zoom = Math.Clamp (camera.Zoom - zoomAmount, 1f, 45f) }

    let modifyDirection (xOffset:float32) (yOffset:float32) (camera:Camera) =
        let yaw = camera.Yaw + xOffset
        let pitch = camera.Pitch - yOffset

        { camera with
            //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
            Yaw = Math.Clamp (yaw, -100f, -80f)
            Pitch = Math.Clamp (pitch, -10f, 10f) }


    let direction (camera:Camera) : Vector3 =
        Vector3 (
            MathF.Cos(degreesToRadians(camera.Yaw)) * MathF.Cos(degreesToRadians(camera.Pitch)),
            MathF.Sin(degreesToRadians(camera.Pitch)),
            MathF.Sin(degreesToRadians(camera.Yaw)) * MathF.Cos(degreesToRadians(camera.Pitch)) )

    let front (camera:Camera) : Vector3 =
        camera |> direction |> Vector3.Normalize

    let viewMatrix (camera:Camera) =
        Matrix4x4.CreateLookAt (
            camera.Position,
            camera.Position + (camera |> front),
            camera.Up)

    let projectionMatrix (width:int) (height:int) (camera:Camera) =
        Matrix4x4.CreatePerspectiveFieldOfView (
            camera.Zoom |> degreesToRadians,
            (float32 width) / (float32 height),
            0.1f,
            100f )
