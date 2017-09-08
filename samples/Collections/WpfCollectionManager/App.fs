
open System

open CollectionSample
open Gjallarhorn.Wpf

open Views

// The WPF Platform specific bits of this application need to do 2 things:
// 1) They create the view (the actual Window)
// 2) The start the WPF specific version of the framework with the view


// ----------------------------------  Application  ---------------------------------- 
[<STAThread>]
[<EntryPoint>]
let main _ =  
    // These are our "handlers" for accepted and rejected requests
    // We're defining them here to show that we can pass them around from 
    // anywhere in the program, and inject them into the PCL target safely 
    // (since printfn/Console isn't available in PCL)
    // In a "real program" this would likely call out to a service
    let fnAccepted (req : Request.Model) = 
        Console.ForegroundColor <- ConsoleColor.Green
        printfn "Accepted Request: %A" req.Id
    let fnRejected (req : Request.Model) = 
        Console.ForegroundColor <- ConsoleColor.Red
        printfn "Rejected Request: %A" req.Id

    let printItem (req : Request.Model) =
        match req.Status with
        | Request.Status.Accepted -> fnAccepted req
        | Request.Status.Rejected -> fnRejected req
        | _ -> ()

    let logger _ msg _ =
        match msg with 
        | CollectionApplication.Msg.Update(Requests.Message.Remove(items)) -> 
            items
            |> List.iter printItem
        | _ -> ()

    // Run using the WPF wrappers around the basic application framework    
    let app = Program.applicationCore
    app.AddLogger logger

    Framework.RunApplication (App, MainWin, app)
    1