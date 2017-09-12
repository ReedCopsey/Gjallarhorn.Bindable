namespace CollectionSample

open System
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

module CollectionApplication =

    type Msg =
        | AddRequests of bool
        | ProcessRequests of bool
        | Update of Requests.Message
        | Process of TimeSpan        

    // Create an application wide model+ msg + update which composes 
    // multiple models
    type Model = { Requests : Requests.Model ; AddingRequests : Executor<Msg> ; Processing : Executor<Msg> }

    let buildInitialModel adding processing : Model = 
        { 
            Requests = [] 
            AddingRequests = adding
            Processing = processing
        }
    
    let update (dispatch : Dispatcher<Msg>) (msg : Msg) (current : Model) = 
        match msg with
        | AddRequests b -> 
            if b then current.AddingRequests.Start() else current.AddingRequests.Stop()
            current
        | ProcessRequests b -> 
            if b then current.Processing.Start() else current.Processing.Stop()            
            current
        | Update u -> { current with Requests = Requests.update u current.Requests }
        | Process timeSpan ->
            let threshold = DateTime.UtcNow - timeSpan

            let shouldRemove (req : Request) =
                match req.Status, req.StatusUpdated with
                | RequestStatus.Unknown, _ -> false
                | _ , Some u when u <= threshold -> true
                | _ -> false
                
            let toRemove = current.Requests |> List.filter shouldRemove
                
            // Dispatch an operation to remove these requests
            toRemove
            |> Requests.Remove
            |> Update
            |> dispatch.Dispatch

            current

    // We split our update operations into a separate, nested "child" object
    // This is completely optional, but allows us, on the binding side, to use separate xaml bound to "Updates" 
    // (see below) to access these properties for toggling on the operations
    type extVM = { AddingRequests : bool ; Processing : bool }
    let  extD = { AddingRequests = false ; Processing = false }
    let externalComponent =                
        Component.fromBindings<Model,_,_> [
            <@ extD.AddingRequests @> |> Bind.twoWay (fun m -> m.AddingRequests.Executing.Value) AddRequests
            <@ extD.Processing     @> |> Bind.twoWay (fun m -> m.Processing.Executing.Value)     ProcessRequests
        ]         

    // Our main "ViewModel" for creating the bindings
    type AppViewModel =
        {
            Requests : Requests.Model
            Updates : Model
        }
    let appd = { Requests = [] ; Updates = Unchecked.defaultof<_> }

    /// Compose our components above into one application level component
    let appComponent =
        Component.fromBindings<Model,_,_> [
            <@ appd.Requests @> |> Bind.comp (fun m -> m.Requests) Requests.requestsComponent (fst >> Update)
            <@ appd.Updates @>  |> Bind.comp id externalComponent fst
        ] 

