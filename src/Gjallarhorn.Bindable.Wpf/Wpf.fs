namespace Gjallarhorn.Wpf

open System.Threading
open System.Windows.Threading
open Gjallarhorn.Bindable

/// Platform installation
module Platform =

    // Gets, and potentially installs, the WPF synchronization context
    let private installAndGetSynchronizationContext () =
        match SynchronizationContext.Current with
        | null ->
            // Create our UI sync context, and install it:
            DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher)
            |> SynchronizationContext.SetSynchronizationContext
        | _ -> ()

        SynchronizationContext.Current

    let private creation (typ : System.Type) =
        let sourceType = typedefof<Gjallarhorn.Wpf.DesktopBindingSource<_>>.MakeGenericType([|typ|])
        System.Activator.CreateInstance(sourceType) 

    /// Installs WPF targets for binding into Gjallarhorn
    [<CompiledName("Install")>]
    let install installSynchronizationContext =        
        Gjallarhorn.Bindable.Bind.Implementation.installCreationFunction (fun _ -> creation typeof<obj>) creation

        match installSynchronizationContext with
        | true -> installAndGetSynchronizationContext ()
        | false -> SynchronizationContext.Current

module App =                    
    let toApplicationSpecification (navigator : Framework.INavigator<'Model, 'Nav, 'Message>) (appCore : Framework.ApplicationCore<'Model, 'Nav, 'Message>) : Framework.ApplicationSpecification<'Model,'Nav,'Message> = 
            { 
                Core = appCore
                Render = navigator.Run appCore
            }                

/// WPF Specific implementation of the Application Framework
[<AbstractClass;Sealed>]
type Framework =
    /// Run an application given an Application generator, Window generator, and other required information
    static member RunApplication<'Model,'Nav,'Message when 'Model : equality> (navigator : Framework.INavigator<'Model,'Nav,'Message>, applicationInfo : Framework.ApplicationCore<'Model,'Nav,'Message>) =        
        Framework.Framework.runApplication (App.toApplicationSpecification navigator applicationInfo) 
    
    ///// Run an application using Application.Current and a function to construct the main window
    //static member RunApplication<'Model,'Nav,'Message,'Window when 'Window :> Window> (windowCreation : System.Func<'Window>, applicationInfo : Framework.ApplicationCore<'Model,'Nav,'Message>) =
    //    let render (createCtx : SynchronizationContext -> ObservableBindingSource<'Message>) = 
    //        let dataContext = createCtx SynchronizationContext.Current

    //        // Get or create the application first, which guarantees application resources are available
    //        // If we create the application, we assume we need to run it explicitly
    //        let app, run = 
    //            match Application.Current with
    //            | null -> Application(), true
    //            | a -> a, false

    //        // Use the main Window as our entry window
    //        let win = windowCreation.Invoke ()
    //        app.MainWindow <- win
    //        win.DataContext <- dataContext               

    //        // Use standdard WPF message pump
    //        if run then
    //            app.Run win |> ignore
    //        else                
    //            win.Show()                

    //    Platform.install true |> ignore        
    //    Gjallarhorn.Bindable.Framework.Framework.runApplication (App.toApplicationSpecification render applicationInfo) 


