open Silk.NET.Input
open Silk.NET.Maths
open Silk.NET.Windowing

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onLoad (window:IWindow) =
    //Set-up input context.
    let input = window.CreateInput ()
    input.Keyboards
    |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (keyDown window))
    
[<EntryPoint>]
let main _ =
    //Create a window.
    use window:IWindow = Window.Create WindowOptions.Default
    window.Size <- Vector2D<int> (800, 600)
    window.Title <- "LearnOpenGL with Silk.NET"

    //Assign events.
    window.add_Load (fun _ -> onLoad window)
    window.add_Update (fun _ -> printfn "Here all updates to the program should be done.")
    window.add_Render (fun _ -> printfn "Here all rendering should be done.")

    //Run the window.
    window.Run ()

    // window.Run() is a BLOCKING method - this means that it will halt execution of any code in the current
    // method until the window has finished running. Therefore, this dispose method will not be called until you
    // close the window.
    window.Dispose ()
    0