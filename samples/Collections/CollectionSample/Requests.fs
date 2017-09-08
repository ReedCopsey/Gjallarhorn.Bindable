namespace CollectionSample

open System
open Gjallarhorn.Bindable

module Requests = 
    type Model = Request.Model list

    // These are the updates that can be performed on our requests
    type Message = 
        | Accept of Request.Model
        | Reject of Request.Model
        | AddNew of Guid * float
        | Remove of Request.Model list

    // Maps from an accept/rejection of a single request to an
    // update message for the model as a whole
    let requestUpdateToUpdate (ru : Request.Message, req : Request.Model) =
        match ru with
        | Request.Accept -> Accept req
        | Request.Reject -> Reject req        

    // Update the model based on an UpdateRequest
    let rec update msg current =
        let excluded r = current |> List.except [| r |]
        match msg with
        | Accept(r)-> Request.update Request.Accept r :: excluded r
        | Reject(r) -> Request.update Request.Reject r :: excluded r            
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
