module LearnOpenTK.Common.Result

type ResultBuilder() =
    member _.Bind(result, binder) = Result.bind binder result
    member _.Return(value) = Result.Ok value
    
let result = ResultBuilder ()
    