namespace CollectionSample

open System
open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

module CollectionApplication =

    type Msg =
        | AddRequests of bool
        | ProcessRequests of bool
        | Update of Requests.Message
        | Process of TimeSpan        
        with 
            // Helpers we use to convert from "child messages" to this message type
            static member FromRequests (msg : Requests.Message) = msg |> Update
            static member FromRequest (request : Request) = request |> Requests.Message.Update |> Msg.FromRequests

    // Create an application wide model+ msg + update which composes 
    // multiple models
    type Model = { Requests : Requests.Model ; AddingRequests : bool ; Processing : bool }

    let buildInitialModel : Model = 
        { 
            Requests = [] 
            AddingRequests = false
            Processing = false
        }
    
    let update (adding : Executor<_>) (processing : Executor<_>) (dispatch : Dispatcher<Msg>) (msg : Msg) (current : Model) = 
        match msg with
        | AddRequests b -> 
            adding.Executing <- b
            { current with AddingRequests = adding.Executing }
        | ProcessRequests b -> 
            processing.Executing <- b
            { current with Processing = b }            
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
    type ProcessVM =
        {
            AddingRequests : bool
            Processing : bool
        }
    let procd = { AddingRequests = false ; Processing = false }

    let externalComponent = 
        Component.create<Model,_,_> [
            <@ procd.AddingRequests @> |> Bind.twoWay (fun m -> m.AddingRequests) AddRequests
            <@ procd.Processing @>     |> Bind.twoWay (fun m -> m.Processing)     ProcessRequests
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
        Component.create<Model,_,_> [
            <@ appd.Requests @> |> Bind.comp (fun m -> m.Requests) Requests.requestsComponent (fst >> Update)
            <@ appd.Updates @>  |> Bind.comp id externalComponent fst
        ] 

