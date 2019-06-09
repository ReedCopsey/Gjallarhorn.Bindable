namespace CollectionSample


open Credentials

open System
open Gjallarhorn.Bindable

module CollectionApplication =

    type Msg =
        | AddRequests of bool
        | ProcessRequests of bool
        | Update of Requests.Message
        | UpdateCredentials of User
        | Process of TimeSpan        
        with 
            // Helpers we use to convert from "child messages" to this message type
            static member FromRequests (msg : Requests.Message) = msg |> Update
            static member FromRequest (request : Request) = request |> Requests.Message.Update |> Msg.FromRequests

    // Create an application wide model+ msg + update which composes 
    // multiple models
    type Model = { Requests : Requests.Model ; AddingRequests : bool ; Processing : bool ; Credentials : User }

    let buildInitialModel : Model = 
        { 
            Requests = [] 
            AddingRequests = false
            Processing = false
            Credentials = { User = "" ; AuthenticationStatus = AuthenticationStatus.Unknown }
        }
    
    let update (nav : Dispatcher<CollectionNav>) (dispatch : Dispatcher<Msg>) (msg : Msg) (current : Model) = 
        match msg with
        | AddRequests b ->       { current with AddingRequests = b }
        | ProcessRequests b ->   { current with Processing = b }            
        | Update u ->            { current with Requests = Requests.update u current.Requests }
        | UpdateCredentials c -> 
            match current.Credentials.AuthenticationStatus, c.AuthenticationStatus with
            // If we weren't approved before, and are now, navigate us to start processing requests
            | old, AuthenticationStatus.Approved when old <> AuthenticationStatus.Approved -> 
                // Add slight delay so you see UI update
                async {
                    do! Async.Sleep 1500
                    return CollectionNav.StartProcessing(true, true)
                }
                |> nav.DispatchAsync 
            // Otherwise, do nothing here
            | _ -> ()

            { current with Credentials = c }
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
            User : User
        }
    let appd = { Requests = [] ; Updates = Unchecked.defaultof<_> ; User = { User = "" ; AuthenticationStatus = Unknown } }

    /// Compose our components above into one application level component
    let appComponent =
        Component.create<Model,_,_> [
            <@ appd.Requests @> |> Bind.comp (fun m -> m.Requests) Requests.requestsComponent (fst >> Update)
            <@ appd.Updates @>  |> Bind.comp id externalComponent fst
            <@ appd.User @>     |> Bind.comp (fun m -> m.Credentials) Credentials.credentialComponent (fst >> UpdateCredentials)
        ] 
