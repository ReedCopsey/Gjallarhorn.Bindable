namespace Gjallarhorn.Bindable.Framework

open Gjallarhorn
open Gjallarhorn.Bindable
open System.Threading
open System

/// Dispatch a single message
type Dispatch<'a> = 'a -> unit

/// The core information required for an application 
type ApplicationCore<'Model,'Message> (initialModel, update, binding) =             

    let model = Mutable.createAsync initialModel
    let logging = Event<_>()

    let mutable dispatched = None
    let dispatch msg = dispatched <- Some msg    

    let rec updateLog msg model =
        let orig = model
        let updated = update msg model
        logging.Trigger (orig, msg, updated)

        if Option.isSome dispatched then
            let msg = Option.get dispatched
            dispatched <- None
            updateLog msg updated
        else
            updated

    let upd msg = model.Update (updateLog msg) |> ignore
    let updAsync msg = model.UpdateAsync (updateLog msg) |> Async.Ignore

    /// The current model as a signal
    member __.Model : ISignal<'Model> = model :> _
    
    /// Push an update message to the model
    member __.Update (message : 'Message) : unit =  
        upd message

    /// Push an update message asynchronously to the model
    member __.UpdateAsync (message : 'Message) : Async<unit> =  
        updAsync message

    /// The function which binds the model to the view
    member __.Binding : Component<'Model,'Message> = binding

    /// An stream that reports all updates as original model, message, new model
    member __.UpdateLog with get () = logging.Publish :> System.IObservable<_>

    /// Used to dispatch operations back to  
    member __.Dispatch : Dispatch<'Message> = dispatch

    /// Add a logger to this application
    member this.AddLogger logger =
        this.UpdateLog.Add (fun (o,msg,n) -> logger o msg n)        

/// Alias for a function to create a data context
type CreateDataContext<'Message> = System.Threading.SynchronizationContext -> ObservableBindingSource<'Message>

/// Full specification required to run an application
type ApplicationSpecification<'Model,'Message> = 
    { 
        /// The application core
        Core : ApplicationCore<'Model,'Message>
        /// The platform specific render function
        Render : CreateDataContext<'Message> -> unit
    }
    with 
        /// The model generator function from the core application
        member this.Model = this.Core.Model
        /// The update function from the core application
        member this.Update = this.Core.Update
        /// The binding function from the core application
        member this.Binding = this.Core.Binding   

type Dispatcher<'a> () =
    let evt = Event<'a>()
    let trigger: Dispatch<_> = fun msg -> evt.Trigger msg

    // Trigger our message
    member __.Dispatch msg = trigger msg

    interface System.IObservable<'a> with
        member __.Subscribe (o : System.IObserver<'a>) = evt.Publish.Subscribe o

/// Type to execute messages externally
type Executor<'a> (startExecuting : Dispatch<'a> -> CancellationToken -> unit) = 
    let executing = Mutable.create false
    
    let dispatch = Dispatcher<'a>()

    let mutable cts = null
    let changeState run =
        if run then
            cts <- new CancellationTokenSource()
            startExecuting dispatch.Dispatch cts.Token
        else
            cts.Cancel()
            cts <- null

    let subscription = executing |> Signal.Subscription.create changeState

    // Use to start or stop this operation
    member __.Executing with get () = executing :> IMutatable<_>

    member this.Start() = this.Executing.Value <- true
    member this.Stop()  = this.Executing.Value <- false

    interface System.IObservable<'a> with
        member __.Subscribe (o : System.IObserver<'a>) = (dispatch :> System.IObservable<'a>).Subscribe o
    interface System.IDisposable with
        member __.Dispose() = 
            subscription.Dispose()
            if not(isNull cts) then
                cts.Dispose()


    

/// A platform neutral application framework
module Framework =
        
    /// Build an application given an initial model, update function, and binding function
    let application model update binding = ApplicationCore(model, update, binding)

    /// Add a dispatch operation from an arbitrary observable to pump messages into the application
    let withDispatcher (dispatcher : System.IObservable<'a>) (mapper : 'a -> 'Msg) (application : ApplicationCore<_,'Msg>) =
        let execute a =
            let msg = mapper a
            application.UpdateAsync msg |> Async.Start
        dispatcher
        |> Observable.add execute
        application

    /// Adds a logger function 
    let withLogger (logger : 'Model -> 'Message -> 'Model -> unit) (application : ApplicationCore<'Model,'Message>) =
        application.AddLogger logger
        application        
    
    /// Run an application given the full ApplicationSpecification            
    let runApplication<'Model,'Message> (applicationInfo : ApplicationSpecification<'Model,'Message>) =        
        // Map our state directly into the view context - this gives us something that can be data bound
        let viewContext (ctx : System.Threading.SynchronizationContext) = 
            let source = Bind.createObservableSource<'Message>()                    
            let model = 
                applicationInfo.Model 
                |> Signal.observeOn ctx

            applicationInfo.Binding.Install (source :> BindingSource) model
            |> source.OutputObservables

            // Permanently subscribe to the observables, and call our update function
            // Note we're not allowing this to be a disposable subscription - we need to force it to
            // stay alive, even in Xamarin Forms where the "render" method doesn't do the final rendering
            source.Add applicationInfo.Update
            source
        
        // Render the "application"
        applicationInfo.Render viewContext
           