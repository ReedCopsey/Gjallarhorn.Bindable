namespace Gjallarhorn.Bindable

open Gjallarhorn

open System
open System.Threading

/// Represents a Dispatch of a single message
type Dispatch<'Msg> = 'Msg -> unit

/// Allows dispatching of a message via an Observable
type Dispatcher<'Msg> () =
    let evt = Event<'Msg>()
    let trigger: Dispatch<_> = fun msg -> evt.Trigger msg

    /// Trigger our message to be dispatched
    member __.Dispatch msg = trigger msg

    /// Trigger our message to be dispatched
    member __.DispatchAsync (msg : Async<'Msg>) =
        async {
            let! m = msg
            trigger m
        } |> Async.Start

    interface IObservable<'Msg> with
        member __.Subscribe (o : IObserver<'Msg>) = evt.Publish.Subscribe o

/// Manages the execution of an operation that produces messages to be dispatched.
/// Constructed from a startup function and (optionally) a subscription which returns a bool for whether we should execute on model changes
type Executor<'Model,'Msg> (startExecuting : Dispatch<'Msg> -> CancellationToken -> unit, ?subscription : ('Model -> bool)) = 
    let executing = Mutable.create false

    // Default subscription just keeps us in our current executing state
    let subscription : 'Model -> bool = defaultArg subscription (fun _ -> executing.Value)
    
    let dispatch = Dispatcher<'Msg>()

    let mutable cts = null
    let changeState run =
        if run then
            cts <- new CancellationTokenSource()
            startExecuting dispatch.Dispatch cts.Token
        else
            cts.Cancel()
            cts <- null

    let trackExecution = executing |> Signal.Subscription.create changeState

    /// Used to watch our execution status
    member __.Executing with get () = executing.Value and set(b) = executing.Value <- b

    /// The subscription used for 
    member internal __.Subscription m = 
        executing.Value <- subscription m

    /// Attempt to start the operation
    member __.Start() = executing.Value <- true
    
    /// Attempt to stop the operation
    member __.Stop()  = executing.Value <- false

    interface IObservable<'Msg> with
        member __.Subscribe (o : IObserver<'Msg>) = (dispatch :> IObservable<'Msg>).Subscribe o
    interface IDisposable with
        member __.Dispose() = 
            trackExecution.Dispose()
            if not(isNull cts) then
                cts.Dispose()


