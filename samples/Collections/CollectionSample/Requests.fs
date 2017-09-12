namespace CollectionSample

open System
open Gjallarhorn.Bindable

module Requests = 
    type Model = Request list

    // These are the updates that can be performed on our requests
    type Message = 
        | Accept of Request
        | Reject of Request
        | AddNew of Guid * float
        | Remove of Request list

    // Maps from an accept/rejection of a single request to an
    // update message for the model as a whole
    let requestUpdateToUpdate (ru : RequestMsg, req : Request) =
        match ru with
        | RequestMsg.Accept -> Accept req
        | RequestMsg.Reject -> Reject req        

    // Update the model based on an UpdateRequest
    let rec update msg current =
        let excluded r = current |> List.except [| r |]
        match msg with
        | Accept(r)-> Request.update RequestMsg.Accept r :: excluded r
        | Reject(r) -> Request.update RequestMsg.Reject r :: excluded r            
        | AddNew(guid, hours) -> Request.create guid hours :: current 
        | Remove(toRemove) -> current |> List.except toRemove

    type RequestsViewModel =
        {
            Requests : Model
        }
    let reqsd = { Requests = [] }
    
    // Create the component for the Requests as a whole.
    // Note that this uses BindingCollection to map the collection to individual request -> messages,
    // using the component defined previously, then maps this to the model-wide update message.
    let requestsComponent = //source (model : ISignal<Requests>) =
        let sorted (requests : Model) = requests |> List.sortBy (fun r -> r.Created)
        Component.fromBindings [
            <@ reqsd.Requests @> |> Bind.collection sorted Request.requestComponent requestUpdateToUpdate
        ]         
