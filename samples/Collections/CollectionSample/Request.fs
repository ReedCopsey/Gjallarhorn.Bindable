namespace CollectionSample

// Note that this program is defined in a PCL, and is completely platform neutral.
// It will work unchanged on WPF, Xamarin Forms, etc

// This is definitely on the "complicated" side, but shows how to use multiple 
// mailbox processors, defined outside of the main UI, to manage a model
// defined itself as a collection

open System
open Gjallarhorn.Bindable

type RequestStatus =
    | Unknown
    | Accepted
    | Rejected

type Request = 
        { 
            Id : Guid // What is our unique identifier
            Created : DateTime 
            ExpectedHours : float 
            Status : RequestStatus 
            StatusUpdated : DateTime option 
        }    

type RequestMsg =
    | Accept
    | Reject

module Request =

    let create guid hours = { Id = guid ; Created = DateTime.UtcNow ; ExpectedHours = hours ; Status = Unknown ; StatusUpdated = None }
    let update msg model =
        match msg with
        | Accept -> { model with Status = Accepted ; StatusUpdated = Some(DateTime.UtcNow) }
        | Reject -> { model with Status = Rejected ; StatusUpdated = Some(DateTime.UtcNow) }

    // We start with a "ViewModel" for cleaner bindings and XAML support
    type RequestViewModel =
        {
            Id : Guid
            Hours : float
            Status : RequestStatus
            Accept : VmCmd<RequestMsg>
            Reject : VmCmd<RequestMsg>
        }
    let reqd = { Id = Guid.NewGuid() ; Hours = 45.32 ; Status = Accepted ; Accept = Vm.cmd Accept ; Reject = Vm.cmd Reject }
    
    // Create a component for a single request
    let requestComponent  =
        Component.fromBindings<Request,unit,_> [
            <@ reqd.Id @>       |> Bind.oneWay (fun r -> r.Id)
            <@ reqd.Hours @>    |> Bind.oneWay (fun r -> r.ExpectedHours)
            <@ reqd.Status @>   |> Bind.oneWay (fun r -> r.Status)
            <@ reqd.Accept @>   |> Bind.cmd 
            <@ reqd.Reject @>   |> Bind.cmd 
        ] 
