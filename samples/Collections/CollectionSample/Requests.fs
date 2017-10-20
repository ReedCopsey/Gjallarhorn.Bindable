namespace CollectionSample

open System
open Gjallarhorn
open Gjallarhorn.Bindable

module Requests = 
    type Model = Request list

    // These are the updates that can be performed on our requests
    type Message = 
        | Update of Request
        | AddNew of Guid * float
        | Remove of Request list

    // Update the model based on an UpdateRequest
    let rec update msg current =
        let excluded guid = current |> List.filter (fun req -> req.Id <> guid)
        match msg with
        | Update r -> r :: excluded r.Id
        | AddNew(guid, hours) -> Request.create guid hours :: current 
        | Remove(toRemove) -> current |> List.except toRemove

    type RequestsViewModel =
        {
            Requests : Model
            Edit : VmCmd<CollectionNav>
            Info : VmCmd<CollectionNav>
        }
    let reqsd = { Requests = [] ; Edit = Vm.cmd (CollectionNav.DisplayRequest Request.designSignal) ; Info = Vm.cmd (CollectionNav.ShowRequestDetails Request.designSignal.Value)}
    
    // Map our child component to our navigation model (in this case, by just suppressing child navigation requests)
    let requestComponentWithNav = Request.requestComponent |> Component.suppressNavigation

    // Create the component for the Requests as a whole.
    // Note that this uses BindingCollection to map the collection to individual request -> messages,
    // using the component defined previously, then maps this to the model-wide update message.
    let requestsComponent = //source (model : ISignal<Requests>) =
        let sorted (requests : Model) = requests |> List.sortBy (fun r -> r.Created)        
        let hasRequests = List.isEmpty >> not           
        Component.create<Model,CollectionNav,Message> [
            <@ reqsd.Requests @> |> Bind.collection sorted requestComponentWithNav (fst >> Update)
            <@ reqsd.Edit @> |> Bind.cmdParamIf hasRequests CollectionNav.DisplayRequest |> Bind.toNav
            <@ reqsd.Info @> |> Bind.cmdParamIf hasRequests CollectionNav.ShowRequestDetails |> Bind.toNav
        ]         
