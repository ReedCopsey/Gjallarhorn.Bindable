namespace Gjallarhorn.Bindable.Framework

open Gjallarhorn
open Gjallarhorn.Bindable

/// The core information required for an application 
type ApplicationCore<'Model,'Nav,'Message> (initialModel, navUpdate : (ApplicationCore<'Model,'Nav,'Message> -> 'Nav -> unit), update, binding) =             

    let model = Mutable.createAsync initialModel
    let logging = Event<_>()

    let updateLog msg model =
        let orig = model
        let updated = update msg model
        
        logging.Trigger (orig, msg, updated)
        updated    
   
    /// The current model as a signal
    member __.Model : ISignal<'Model> = model :> _

    /// The navigation dispatcher for pumping messages
    member this.Navigation (msg : 'Nav) = navUpdate this msg

    /// Used to dispatch new navigation requests asynchronously  
    member private this.TrampolineNavigationDispatch (msg : 'Nav) = 
        async { 
            this.Navigation msg
            return ()
        } |> Async.Start
    
    /// Push an update message to the model
    member __.Update (message : 'Message) : unit = updateLog message |> model.Update |> ignore

    /// Push an update message asynchronously to the model
    member __.UpdateAsync (message : 'Message) : Async<unit> = 
        updateLog message
        |> model.UpdateAsync
        |> Async.Ignore                

    /// The function which binds the model to the view
    member __.Binding : IComponent<'Model,'Nav,'Message> = binding

    /// An stream that reports all updates as original model, message, new model
    member __.UpdateLog with get () = logging.Publish :> System.IObservable<_>    

    /// Add a logger to this application
    member this.AddLogger logger =
        this.UpdateLog.Add (fun (o,msg,n) -> logger o msg n)        

type INavigator<'Model, 'Nav, 'Message> =
    abstract member Run : ApplicationCore<'Model,'Nav,'Message> -> (System.Threading.SynchronizationContext -> ObservableBindingSource<'Message>) -> unit
    abstract member Navigate : ApplicationCore<'Model,'Nav,'Message> -> 'Nav -> unit

/// Alias for a function to create a data context
type CreateDataContext<'Message> = System.Threading.SynchronizationContext -> ObservableBindingSource<'Message>

/// Full specification required to run an application
type ApplicationSpecification<'Model,'Nav,'Message> = 
    { 
        /// The application core
        Core : ApplicationCore<'Model,'Nav,'Message>
        /// The platform specific render function
        Render : CreateDataContext<'Message> -> unit
    }
    with 
        /// The model generator function from the core application
        member this.Model = this.Core.Model
        /// The update function from the core application
        member this.Update m = this.Core.UpdateAsync m |> Async.Start
        /// The binding function from the core application
        member this.Binding = this.Core.Binding   

    

/// A platform neutral application framework
module Framework =
        
    /// Build an application given an initial model, update function, and binding function
    let application model update binding nav = ApplicationCore(model, nav, update, binding)

    /// Add a dispatch operation from an arbitrary observable to pump messages into the application
    let withDispatcher (dispatcher : System.IObservable<'Msg>) (application : ApplicationCore<_,_,'Msg>) =
        let execute msg = application.UpdateAsync msg |> Async.Start
        dispatcher |> Observable.add execute
        application

    /// Adds a logger function 
    let withLogger (logger : 'Model -> 'Message -> 'Model -> unit) (application : ApplicationCore<'Model,'Nav,'Message>) =
        application.AddLogger logger
        application        
    
    /// Run an application given the full ApplicationSpecification            
    let runApplication<'Model,'Nav,'Message> (applicationInfo : ApplicationSpecification<'Model,'Nav,'Message>) =        
        // Map our state directly into the view context - this gives us something that can be data bound
        let viewContext (ctx : System.Threading.SynchronizationContext) = 
            let source = Bind.createObservableSource<'Message>()                    
            let model = 
                applicationInfo.Model 
                |> Signal.observeOn ctx

            applicationInfo.Binding.Install applicationInfo.Core.Navigation (source :> BindingSource) model
            |> source.OutputObservables

            // Permanently subscribe to the observables, and call our update function
            // Note we're not allowing this to be a disposable subscription - we need to force it to
            // stay alive, even in Xamarin Forms where the "render" method doesn't do the final rendering
            source.Add applicationInfo.Update
            source
        
        // Render the "application"
        applicationInfo.Render viewContext

namespace Gjallarhorn.Bindable

/// Routines for working with Navigation
module Nav =
    type PromptResponse = | Yes | No
    
    type SimpleNavigation<'Message> =
        | ShowMessage of message : string * title : string
        | Prompt of question : string * title : string * response : (PromptResponse -> 'Message)

    /// A predefined, typed navigation dispatch which does nothing
    let empty<'Model,'Message> (_ : Framework.ApplicationCore<'Model,SimpleNavigation<'Message>,'Message>) (_ : SimpleNavigation<'Message>) = ()   

    /// Create a mapper to bubble from a child navigation to a parent navigation
    let bubble<'ChildNav,'ParentNav> (mapper : 'ChildNav -> 'ParentNav option) (parent : Dispatch<'ParentNav>) : Dispatch<'ChildNav> =
        mapper >> (Option.iter parent)

    /// Suppress all navigation messages from a child component to the parent
    let suppress<'ChildNav,'ParentNav> : 'ChildNav -> 'ParentNav option = fun _ -> None
