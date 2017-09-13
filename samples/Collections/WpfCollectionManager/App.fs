
open System

open CollectionSample
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Wpf

open Views

// The WPF Platform specific bits of this application need to do 2 things:
// 1) They create the view (the actual Window)
// 2) The start the WPF specific version of the framework with the view


module WpfNav =
    let displayDialog<'Model,'Nav,'Message,'Window when 'Window :> System.Windows.Window> (nav : Dispatch<'Nav>) (viewFn : unit -> 'Window) (initial : 'Model) (comp : IComponent<'Model,'Nav,'Message>) (dispatch : Dispatch<'Message>) =
        let d = viewFn()
        let bindingSource = Bind.createObservableSource<'Message>()
        let model = Signal.constant initial
        let messages = comp.Install nav bindingSource model

        messages
        |> List.iter (Observable.subscribe dispatch >> bindingSource.AddDisposable)

        d.DataContext <- bindingSource
        d.Owner <- System.Windows.Application.Current.MainWindow
        d.ShowDialog() |> ignore

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

    let rec nav (request : Nav) (dispatch : Dispatch<CollectionApplication.Msg>) =
        let navDispatch r = nav r dispatch
        match request with
        | DisplayRequest r -> 
            let comp = Request.requestComponent |> Component.withMappedNavigation Nav.suppress<_,Nav>
            let dispatchRequest (req : Request) =                
                let msg = 
                    Requests.Message.Update (req, r)
                    |> CollectionApplication.Msg.Update
                dispatch msg
            WpfNav.displayDialog navDispatch RequestDialog r comp dispatchRequest 
        

    // Run using the WPF wrappers around the basic application framework    
    let app = Program.applicationCore nav
    app.AddLogger logger

    Framework.RunApplication (App, MainWin, app)
    1