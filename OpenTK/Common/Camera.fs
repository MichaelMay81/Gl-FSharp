namespace LearnOpenTK.Common

open System
open OpenTK.Mathematics
open MathHelper

// This is the camera type as it could be set up after the tutorials on the website.
// It is important to note there are a few ways you could have set up this camera.
// For example, you could have also managed the player input inside the camera class,
// and a lot of the properties could have been made into functions.

// TL;DR: This is just one of many ways in which we could have set up the camera.
// Check out the web version if you don't know why we are doing a specific thing or want to know more about the code.

// Those vectors are directions pointing outwards from the camera to define how it rotated.
type DirectionVectors = {
    Front: Vector3
    Up: Vector3
    Right: Vector3 }

type EulerAngles = {
    // Rotation around the X axis (radians)
    Pitch: float32<radian>
    // Rotation around the Y axis (radians)
    Yaw: float32<radian> }

type Camera = {
    Orientation: DirectionVectors
    EulerAnglesCache: EulerAngles
    // The field of view of the camera (radians)
    Fov: float32<radian>
    // The position of the camera
    Position: Vector3
    // This is simply the aspect-ratio of the viewport, used for the projection matrix.
    AspectRatio: float32 } with
    static member Init (position:Vector3) (aspectRatio:float32) = { 
        Orientation = {
            Front = -Vector3.UnitZ
            Up = Vector3.UnitY
            Right = Vector3.UnitX }
        EulerAnglesCache = {
            Pitch = 0.0f<radian>
            Yaw = -MathHelper.PiOver2 * 1f<radian> } // Without this, you would be started rotated 90 degrees right.
        Fov = MathHelper.PiOver2 * 1f<radian>
        Position = position
        AspectRatio = aspectRatio }

module Cameras =
    // This function is going to calculate the direction vertices using some of the math learned in the web tutorials.
    let private eulerAnglesToDirectionVectors (eulerAngles:EulerAngles) : DirectionVectors =
        let pitch = eulerAngles.Pitch |> float32
        let yaw = eulerAngles.Yaw |> float32
        
        // First, the front matrix is calculated using some basic trigonometry.
        let frontX = MathF.Cos(pitch) * MathF.Cos(yaw)
        let frontY = MathF.Sin(pitch)
        let frontZ = MathF.Cos(pitch) * MathF.Sin(yaw)
        
        // We need to make sure the vectors are all normalized, as otherwise we would get some funky results.
        let front = Vector3 (frontX, frontY, frontZ) |> Vector3.Normalize
        
        // Calculate both the right and the up vector using cross product.
        // Note that we are calculating the right from the global up; this behaviour might
        // not be what you need for all cameras so keep this in mind if you do not want a FPS camera.
        let right =
            (front, Vector3.UnitY)
            |> Vector3.Cross
            |> Vector3.Normalize
        let up =
            (right, front)
            |> Vector3.Cross
            |> Vector3.Normalize
        
        { Front=front; Right=right; Up=up }
    
    let updatePitchYaw
        (update: {| Pitch: float32<degree>; Yaw: float32<degree> |}  -> {| Pitch: float32<degree>; Yaw: float32<degree> |})
        (camera:Camera)
        : Camera =
        
        let eulerAngles =
            update {|
                Pitch = camera.EulerAnglesCache.Pitch |> radiansToDegrees
                Yaw = camera.EulerAnglesCache.Yaw |> radiansToDegrees|}
        
        // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down, and a bunch
        // of weird "bugs" when you are using euler angles for rotation.
        // If you want to read more about this you can try researching a topic called gimbal lock
        let angle =
            eulerAngles.Pitch
            |> clamp -89f 89f
        
        // We convert from degrees to radians to improve performance.
        let eulerAngles =
            { camera.EulerAnglesCache with
                Pitch = angle |> degreesToRadians
                Yaw = eulerAngles.Yaw |> degreesToRadians }
        
        let directionVectors =
            eulerAngles
            |> eulerAnglesToDirectionVectors 
        
        { camera with
            Orientation = directionVectors
            EulerAnglesCache = eulerAngles }
        
    // The field of view (FOV) is the vertical angle of the camera view.
    // This has been discussed more in depth in a previous tutorial,
    // but in this tutorial, you have also learned how we can use this to simulate a zoom feature.
    // We convert from degrees to radians to improve performance.
    let updateFov
        (updateFov: float32<degree> -> float32<degree>)
        (camera:Camera)
        : Camera =
            
        let fov =
            camera.Fov
            |> radiansToDegrees
            |> updateFov
            |> clamp 1f 90f
            |> degreesToRadians
        
        { camera with Fov = fov }
        
    // Get the view matrix using the amazing LookAt function described more in depth on the web tutorials
    let getViewMatrix (camera:Camera) : Matrix4 =
        Matrix4.LookAt (camera.Position, camera.Position + camera.Orientation.Front, camera.Orientation.Up)
        
    // Get the projection matrix using the same method we have used up until this point
    let getProjectionMatrix (camera:Camera) : Matrix4 =
        Matrix4.CreatePerspectiveFieldOfView (camera.Fov |> float32, camera.AspectRatio, 0.01f, 100f)
    