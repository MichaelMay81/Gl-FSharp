module Tutorial1_1_Hello_Window

open Silk.NET.Input
open Silk.NET.Maths
open Silk.NET.Windowing

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onLoad (window:IWindow) =
    printfn "Load!"

    let input = window.CreateInput ()
    input.Keyboards
    |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (keyDown window))
    
[<EntryPoint>]
let main _ =
    use window:IWindow = Window.Create WindowOptions.Default
    window.Size <- Vector2D<int> (800, 600)
    window.Title <- "My first Silk.NET program!"

    window.add_Load (fun _ -> onLoad window)
    window.add_Update (fun _ -> printfn "Update!")
    window.add_Render (fun _ -> printfn "Render!")

    window.Run ()
    0