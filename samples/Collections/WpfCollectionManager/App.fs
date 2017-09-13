
open System

open CollectionSample
open Gjallarhorn
open Gjallarhorn.Bindable
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
    let fnAccepted (req : Request) = 
        Console.ForegroundColor <- ConsoleColor.Green
        printfn "Accepted Request: %A" req.Id
    let fnRejected (req : Request) = 
        Console.ForegroundColor <- ConsoleColor.Red
        printfn "Rejected Request: %A" req.Id

    let printItem (req : Request) =
        match req.Status with
        | RequestStatus.Accepted -> fnAccepted req
        | RequestStatus.Rejected -> fnRejected req
        | _ -> ()

    let logger _ msg _ =
        match msg with 
        | CollectionApplication.Msg.Update(Requests.Message.Remove(items)) -> 
            items
            |> List.iter printItem
        | _ -> ()

    let nav (request : Nav) dispatch =
        match request with
        | DisplayRequest r ->
            let d = RequestDialog()
            let comp = Request.requestComponent |> Component.withMappedNavigation Nav.suppress
            let bindingSource = Bind.createObservableSource<Request>()
            let model = Signal.constant r
            let messages = comp.Install dispatch bindingSource model

            let sendMessage (req : Request)=                
                let msg = 
                    Requests.Message.Update (req, r)
                    |> CollectionApplication.Msg.Update
                dispatch msg

            messages
            |> List.iter (Observable.subscribe sendMessage >> bindingSource.AddDisposable)

            d.DataContext <- bindingSource
            d.Owner <- System.Windows.Application.Current.MainWindow
            d.ShowDialog() |> ignore
        

    // Run using the WPF wrappers around the basic application framework    
    let app = Program.applicationCore nav
    app.AddLogger logger

    Framework.RunApplication (App, MainWin, app)
    1