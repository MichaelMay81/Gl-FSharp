namespace Tutorial1_4_Abstractions

open System
open System.Numerics

type Camera = {
    Position: Vector3
    Up: Vector3
    Yaw: float32
    Pitch: float32 }

module Cameras =
    let direction (camera:Camera) : Vector3 =
        Vector3 (
            MathF.Cos(degreesToRadians(camera.Yaw)) * MathF.Cos(degreesToRadians(camera.Pitch)),
            MathF.Sin(degreesToRadians(camera.Pitch)),
            MathF.Sin(degreesToRadians(camera.Yaw)) * MathF.Cos(degreesToRadians(camera.Pitch))
        )

    let front (camera:Camera) : Vector3 =
        camera |> direction |> Vector3.Normalize

    let viewMatrix (camera:Camera) =
        Matrix4x4.CreateLookAt (
            camera.Position,
            camera.Position + (camera |> front),
            camera.Up)
