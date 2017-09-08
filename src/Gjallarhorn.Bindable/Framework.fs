namespace Gjallarhorn.Bindable.Framework

open Gjallarhorn
open Gjallarhorn.Bindable
open System.Threading
open System

/// The core information required for an application 
type ApplicationCore<'Model,'Message> (initialModel, update, binding) =             

    let model = Mutable.createAsync initialModel
    let logging = Event<_>()

    let rec updateLog msg model =
        let orig = model
        let updated = update msg model
        logging.Trigger (orig, msg, updated)
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

/// Represents a Dispatch of a single message
type Dispatch<'Msg> = 'Msg -> unit

/// Allows dispatching of a message via an Observable
type Dispatcher<'Msg> () =
    let evt = Event<'Msg>()
    let trigger: Dispatch<_> = fun msg -> evt.Trigger msg

    // Trigger our message
    member __.Dispatch msg = trigger msg

    interface System.IObservable<'Msg> with
        member __.Subscribe (o : System.IObserver<'Msg>) = evt.Publish.Subscribe o

/// Manages the execution of an operation that produces messages to be dispatched
type Executor<'Msg> (startExecuting : Dispatch<'Msg> -> CancellationToken -> unit) = 
    let executing = Mutable.create false
    
    let dispatch = Dispatcher<'Msg>()

    let mutable cts = null
    let changeState run =
        if run then
            cts <- new CancellationTokenSource()
            startExecuting dispatch.Dispatch cts.Token
        else
            cts.Cancel()
            cts <- null

    let subscription = executing |> Signal.Subscription.create changeState

    /// Used to watch our execution status
    member __.Executing with get () = executing :> ISignal<_>

    /// Attempt to start the operation
    member __.Start() = executing.Value <- true
    
    /// Attempt to stop the operation
    member __.Stop()  = executing.Value <- false

    interface System.IObservable<'Msg> with
        member __.Subscribe (o : System.IObserver<'Msg>) = (dispatch :> System.IObservable<'Msg>).Subscribe o
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
    let withDispatcher (dispatcher : System.IObservable<'Msg>) (application : ApplicationCore<_,'Msg>) =
        let execute msg = application.UpdateAsync msg |> Async.Start
        dispatcher |> Observable.add execute
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
           