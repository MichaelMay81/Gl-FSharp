namespace LearnOpenTK.Common

[<Measure>]
type radian

[<Measure>]
type degree

module MathHelper =
    open OpenTK.Mathematics
    
    let radiansToDegrees (radians:float32<radian>) : float32<degree>=
        radians
        |> float32
        |> MathHelper.RadiansToDegrees
        |> (*) 1f<degree>
        
    let degreesToRadians (degrees:float32<degree>) : float32<radian> =
        degrees
        |> float32
        |> MathHelper.DegreesToRadians
        |> (*) 1f<radian>

    let clamp (min:float32) (max:float32) (n:float32<'T>) : float32<'T> =
        (n |> float32, min, max)
        |> MathHelper.Clamp
        |> LanguagePrimitives.Float32WithMeasure