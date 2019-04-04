namespace Gjallarhorn.Avalonia

open System.Threading
open Avalonia
open Avalonia.Controls

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework
open System

type private SingleView<'Model, 'Nav, 'Message, 'App, 'Win when 'Model : equality and 'App :> Application and 'Win :> Window> private (appCtor : unit -> 'App, windowCtor : unit -> 'Win, show : bool) =

    new(appCtor : unit -> 'App, windowCtor : unit -> 'Win) = SingleView(appCtor, windowCtor, false)
    static member Create(windowCtor : unit -> 'Win) =
        Platform.install true |> ignore

        // Get or create the application first, which guarantees application resources are available
        // If we create the application, we assume we need to run it explicitly
        let app, run = 
            match Application.Current with
            | null -> Application, true
            | a -> (fun () -> a), false
        SingleView<'Model,'Nav,'Message,Application,'Win>(app, windowCtor, run)        

    member __.Run (createCtx : SynchronizationContext -> ObservableBindingSource<'Message>) =
        let ctx = Platform.install true
        let dataContext = createCtx ctx
        // Construct application first, which guarantees application resources are available
        let app = appCtor()
        // Construct main window and set data context
        let win = windowCtor()
        win.DataContext <- dataContext             
        app.Run win |> ignore

    interface INavigator<'Model, 'Nav, 'Message> with
        member this.Run _app createCtx = this.Run createCtx

        // Our navigation does nothing
        member __.Navigate (_app : ApplicationCore<'Model,'Nav,'Message>) (_nav : 'Nav) = ()

type UIType =
    | Ignore
    | Content of Control
    | ModalDialog of Window
//    | Message of title : string * message : string

/// Generates the UI, with proper binding contexts installed
[<AbstractClass>]
type UIFactory<'Model,'Nav,'Message when 'Model : equality> () =    
    let mutable beforeNav = None
    let mutable afterNav = None

    /// Create the specific UI element given the application
    abstract member Create : ApplicationCore<'Model,'Nav,'Message> -> UIType

    member __.BeforeNav = defaultArg beforeNav (fun () -> ())
    member __.AfterNav =  defaultArg afterNav (fun () -> ())
    member __.SetBeforeNav (handler : unit -> unit) = beforeNav <- Some handler
    member __.SetAfterNav  (handler : unit -> unit) = afterNav <- Some handler

type private IgnoreUIFactory<'Model,'Nav,'Message when 'Model : equality> () =
    inherit UIFactory<'Model,'Nav,'Message>()
    override __.Create _ = Ignore

//type private MessageUIFactory<'Model,'Nav,'Message when 'Model : equality> (title,message) =
//    inherit UIFactory<'Model,'Nav,'Message>()
//    override __.Create app = Message(title,message)

type private ComponentUIFactory<'Model,'Nav,'Message,'Submodel,'Submsg,'FE when 'Model : equality and 'FE :> Control>
        (
            makeElement : unit -> 'FE,
            modelMapper : ISignal<'Model> -> ISignal<'Submodel>,
            comp        : IComponent<'Submodel,'Nav,'Submsg>,
            msgMapper   : 'Submsg -> 'Message,
            makeWindows : bool
        ) =
            inherit UIFactory<'Model,'Nav,'Message>()

            override __.Create app = 
                let ui = makeElement () :> Control
                let signal = modelMapper app.Model
                let source = Bind.createObservableSource<'Submsg> ()
                let msgs = comp.Install app.Navigation source signal 

                msgs
                |> List.iter (fun msg -> msg |> Observable.subscribe (fun m -> msgMapper m |> app.UpdateAsync |> Async.Start) |> source.AddDisposable)

                ui.DataContext <- source

                if makeWindows then
                    ModalDialog (ui :?> Window)
                else
                    Content ui


type private SinglePageApplicationNavigator<'Model,'Nav,'Message, 'App, 'Win when 'Model : equality and 'App :> Application and 'Win :> Window> 
                (
                    initialNavigationState : 'Nav, 
                    appCtor : unit -> 'App, 
                    windowCtor : unit -> 'Win, 
                    update : ApplicationCore<'Model,'Nav,'Message> -> 'Nav -> UIFactory<'Model,'Nav,'Message>
                ) =
    let ctx = Platform.install true

    // Construct application first, which guarantees application resources are available
    let application = appCtor()
    let mainWindow = windowCtor ()    
    
    member this.Run (app : ApplicationCore<'Model,'Nav,'Message>) (createCtx : SynchronizationContext -> ObservableBindingSource<'Message>) =
        let dataContext = createCtx ctx
        // Construct main window and set data context        
        mainWindow.DataContext <- dataContext     
        this.Update app initialNavigationState
        
        application.Run mainWindow |> ignore

    member __.Update (application: ApplicationCore<'Model,'Nav,'Message>) (nav : 'Nav) : unit =
        let factory = update application nav
        let newUI = factory.Create application

        factory.BeforeNav ()
        match newUI with
        | Ignore -> ()
        | Content newContent ->
            let oldContent = mainWindow.Content
            mainWindow.Content <- newContent
        
            // Dispose of any old binding source if needed
            match oldContent with
            | :? Control as fe -> 
                let dataCtx = fe.DataContext
                match dataCtx with
                | :? IDisposable as disp -> disp.Dispose()
                | _ -> ()
                fe.DataContext <- null            
            | _ -> ()
        //| Message (title,message) ->
        //    MessageBox.Show(mainWindow,message,title) |> ignore
        | ModalDialog window ->
            window.Owner <- mainWindow
            let dataCtx = window.DataContext
            let t = window.ShowDialog()
            match dataCtx with
            | :? IDisposable as disp -> t.ContinueWith(fun _ -> disp.Dispose()) |> ignore
            | _ -> ()

        factory.AfterNav ()
    interface INavigator<'Model,'Nav,'Message> with
        member this.Run app createCtx = this.Run app createCtx

        member this.Navigate (app : ApplicationCore<'Model,'Nav,'Message>) (nav : 'Nav) = this.Update app nav

module Navigation =
    
    let singleView appCtor windowCtor = SingleView<_,_,_,_,_>(appCtor, windowCtor) :> INavigator<_,_,_>
    let singleViewFromWindow windowCtor = SingleView<_,_,_,_,_>.Create(windowCtor) :> INavigator<_,_,_>

    let singlePage<'Model,'Nav,'Message, 'App, 'Win when 'Model : equality and 'App :> Application and 'Win :> Window>  appCtor windowCtor initialNav update = SinglePageApplicationNavigator<'Model,'Nav,'Message, 'App, 'Win> (initialNav,appCtor,windowCtor,update) :> INavigator<_,_,_>

    module Page =
        let create (makeElement : unit -> 'UIElement) (comp : IComponent<'Model,'Nav,'Message>) =
            ComponentUIFactory (makeElement, id, comp, id,false) :> UIFactory<_,_,_>
        let ignore<'Model,'Nav,'Message when 'Model : equality> () = IgnoreUIFactory<_,_,_> () :> UIFactory<'Model,'Nav,'Message>
        // let message<'Model,'Nav,'Message when 'Model : equality> title message = MessageUIFactory<_,_,_> (title, message) :> UIFactory<'Model,'Nav,'Message>

        let fromComponentS
                (makeElement : unit -> 'UIElement)
                (modelMapper : ISignal<'Model> -> ISignal<'Submodel>)
                (comp        : IComponent<'Submodel,'Nav,'Submsg>)
                (msgMapper   : 'Submsg -> 'Message) =
                    ComponentUIFactory (makeElement,modelMapper,comp,msgMapper,false) :> UIFactory<_,_,_>
        let fromComponent 
                (makeElement : unit -> 'UIElement)
                (modelMapper : 'Model -> 'Submodel)
                (comp        : IComponent<'Submodel,'Nav,'Submsg>)
                (msgMapper   : 'Submsg -> 'Message) =
                    ComponentUIFactory (makeElement,(fun m -> m |> Signal.map modelMapper),comp,msgMapper,false) :> UIFactory<_,_,_>

        let dialogS<'Model,'Nav,'Message,'Submodel,'Submsg,'Win when 'Model : equality and 'Win :> Window>
                (makeElement : unit -> 'Win)
                (modelMapper : ISignal<'Model> -> ISignal<'Submodel>)
                (comp        : IComponent<'Submodel,'Nav,'Submsg>)
                (msgMapper   : 'Submsg -> 'Message) =
                    ComponentUIFactory (makeElement,modelMapper,comp,msgMapper,true) :> UIFactory<_,_,_>

        let dialog<'Model,'Nav,'Message,'Submodel,'Submsg,'Win when 'Model : equality and 'Win :> Window>
                (makeElement : unit -> 'Win)
                (modelMapper : 'Model -> 'Submodel)
                (comp        : IComponent<'Submodel,'Nav,'Submsg>)
                (msgMapper   : 'Submsg -> 'Message) =
                    ComponentUIFactory (makeElement,(fun m -> m |> Signal.map modelMapper),comp,msgMapper,true) :> UIFactory<_,_,_>

        let withCallbacks before after (factory : UIFactory<_,_,_>) =
            factory.SetBeforeNav before
            factory.SetAfterNav after
            factory


namespace Gjallarhorn.Avalonia.CSharp

open Gjallarhorn.Bindable.Framework
open Gjallarhorn.Avalonia

open Avalonia
open Avalonia.Controls

type Navigation =
    static member SingleView (app : System.Func<#Application>, win : System.Func<#Window>) = Navigation.singleView app.Invoke win.Invoke
    static member SingleView<'Model,'Nav,'Message,'Win when 'Model : equality and 'Win :> Window> (win : System.Func<'Win>) = Navigation.singleViewFromWindow win.Invoke :> INavigator<'Model,'Nav,'Message>
    
