namespace Gjallarhorn.Wpf

open System.Threading
open System.Windows

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework
open System

type private SingleView<'Model, 'Nav, 'Message, 'App, 'Win when 'App :> Application and 'Win :> Window> private (appCtor : unit -> 'App, windowCtor : unit -> 'Win, show : bool) =

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

    interface INavigator<'Model,'Nav,'Message> with
        member this.Run _app createCtx = this.Run createCtx

        // Our navigation does nothing
        member __.Navigate (_app : ApplicationCore<'Model,'Nav,'Message>) (_nav : 'Nav) = ()

/// Generates the UI, with proper binding contexts installed
[<AbstractClass>]
type UIFactory<'Model,'Nav,'Message> () =    
    /// Create the specific UI element given the application
    abstract member Create : ApplicationCore<'Model,'Nav,'Message> -> FrameworkElement

type private ComponentUIFactory<'Model,'Nav,'Message,'Submodel,'Submsg,'FE when 'FE :> FrameworkElement>
        (
            makeElement : unit -> 'FE,
            modelMapper : 'Model -> 'Submodel,
            comp        : IComponent<'Submodel,'Nav,'Submsg>,
            msgMapper   : 'Submsg -> 'Message
        ) =
            inherit UIFactory<'Model,'Nav,'Message>()

            override __.Create app = 
                let ui = makeElement () :> FrameworkElement
                let signal = app.Model |> Signal.map modelMapper                
                let source = Bind.createObservableSource<'Submsg> ()
                let msgs = comp.Install app.Navigation source signal 

                msgs
                |> List.iter (fun msg -> msg |> Observable.subscribe (fun m -> msgMapper m |> app.UpdateAsync |> Async.Start) |> source.AddDisposable)

                ui.DataContext <- source
                ui


type private SinglePageApplicationNavigator<'Model,'Nav,'Message, 'App, 'Win when 'App :> Application and 'Win :> Window> 
                (
                    initialNavigationState : 'Nav, 
                    appCtor : unit -> 'App, 
                    windowCtor : unit -> 'Win, 
                    update : ApplicationCore<'Model,'Nav,'Message> -> 'Nav -> UIFactory<'Model,'Nav,'Message> option
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
        let generator = update application nav
        match generator with
        | None -> ()
        | Some factory ->
            let newContent = factory.Create application

            let oldContent = mainWindow.Content
            mainWindow.Content <- newContent
        
            // Dispose of any old binding source if needed
            match oldContent with
            | :? FrameworkElement as fe -> 
                let dataCtx = fe.DataContext
                match dataCtx with
                | :? IDisposable as disp -> disp.Dispose()
                | _ -> ()
                fe.DataContext <- null            
            | _ -> ()

    interface INavigator<'Model,'Nav,'Message> with
        member this.Run app createCtx = this.Run app createCtx

        // Our navigation does nothing
        member this.Navigate (app : ApplicationCore<'Model,'Nav,'Message>) (nav : 'Nav) = this.Update app nav

module Navigation =
    
    let singleView appCtor windowCtor = SingleView<_,_,_,_,_>(appCtor, windowCtor) :> INavigator<_,_,_>
    let singleViewFromWindow windowCtor = SingleView<_,_,_,_,_>.Create(windowCtor) :> INavigator<_,_,_>

    let singlePage appCtor windowCtor initialNav update = SinglePageApplicationNavigator<_,_,_,_,_>(initialNav,appCtor,windowCtor,update) :> INavigator<_,_,_>

    module Generator =
        let create (makeElement : unit -> 'UIElement) (comp : IComponent<'Model,'Nav,'Message>) =
            ComponentUIFactory (makeElement, id, comp, id) :> UIFactory<_,_,_>
        let fromComponent 
                (makeElement : unit -> 'UIElement)
                (modelMapper : 'Model -> 'Submodel)
                (comp        : IComponent<'Submodel,'Nav,'Submsg>)
                (msgMapper   : 'Submsg -> 'Message) =
                    ComponentUIFactory (makeElement,modelMapper,comp,msgMapper) :> UIFactory<_,_,_>

namespace Gjallarhorn.Wpf.CSharp

open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework
open Gjallarhorn.Wpf

open System.Windows

type Navigation =
    static member SingleView (app : System.Func<#Application>, win : System.Func<#Window>) = Navigation.singleView app.Invoke win.Invoke
    static member SingleView<'Model,'Nav,'Message,'Win when 'Win :> Window> (win : System.Func<'Win>) = Navigation.singleViewFromWindow win.Invoke :> INavigator<'Model,'Nav,'Message>
    
