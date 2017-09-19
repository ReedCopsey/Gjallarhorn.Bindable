
open System

open CollectionSample
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework
open Gjallarhorn.Wpf

open Views
open Gjallarhorn.Bindable.Nav
open System.Threading
open System.Windows
open CollectionSample.Requests

// The WPF Platform specific bits of this application need to do 2 things:
// 1) They create the view (the actual Window)
// 2) The start the WPF specific version of the framework with the view




module WpfNav =
    open CollectionSample.CollectionApplication

    let displayDialog<'Model,'Submodel,'Nav,'Message,'Window when 'Window :> Window> (viewFn : unit -> 'Window) (model : ISignal<'Submodel>) (comp : IComponent<'Submodel,'Nav,'Message>) (application : ApplicationCore<'Model,'Nav,'Message>) =
        let navigate, dispatch = application.Navigation, application.Update
        let d = viewFn()
        let bindingSource = Bind.createObservableSource<'Message>()        
        let messages = comp.Install navigate bindingSource model

        messages
        |> List.iter ((Observable.subscribe dispatch) >> bindingSource.AddDisposable)

        d.DataContext <- bindingSource
        d.Owner <- Application.Current.MainWindow
        d.ShowDialog() |> ignore

// ----------------------------------  Application  ---------------------------------- 
[<STAThread>]
[<EntryPoint>]
let main _ =  
    // These are our "logger" for accepted and rejected requests
    // We're defining them here to show that we can pass them around from 
    // anywhere in the program, and inject them into the PCL target safely 
    // (since printfn/Console isn't available in PCL)    
    let printItem (req : Request) =
        match req.Status with
        | RequestStatus.Accepted -> 
            Console.ForegroundColor <- ConsoleColor.Green
            printfn "Accepted Request: %A" req.Id
        | RequestStatus.Rejected -> 
            Console.ForegroundColor <- ConsoleColor.Red
            printfn "Rejected Request: %A" req.Id
        | _ -> ()

    let logger _ msg _ =
        match msg with 
        | CollectionApplication.Msg.Update(Requests.Message.Remove(items)) -> 
            items
            |> List.iter printItem
        | _ -> ()

    //let routeNavigation application request =
    //    // Map our request "child" component to our app navigation model 
    //    // (in this case, by just suppressing child navigation requests),
    //    // ss well as to our update model        
    //    let requestComponentWrapped = 
    //        Request.requestComponent 
    //        |> Component.withMappedNavigation Nav.suppress
    //        |> Component.withMappedMessages CollectionApplication.Msg.FromRequest

    //    match request with
    //    | DisplayRequest r -> 
    //        WpfNav.displayDialog RequestDialog r requestComponentWrapped application        

    let updateNavigation (application : ApplicationCore<_,_,_>) request : UIElement =         
        match request with
        | Login -> 
            LoginControl() :> _
        | DisplayRequest r -> 
            ProcessControl() :> _
        | StartProcessing (addNew,processElements) -> 
            application.Update (CollectionApplication.Msg.AddRequests addNew)
            application.Update (CollectionApplication.Msg.ProcessRequests processElements)
            ProcessControl() :> _

    let makeWindow () = MainWin()        
    let navigator = Navigation.singlePage App makeWindow (Login) updateNavigation

    // Run using the WPF wrappers around the basic application framework    
    let app = Program.applicationCore navigator.Navigate
    app.AddLogger logger

    Framework.RunApplication (navigator, app)
    1