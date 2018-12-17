namespace Gjallarhorn.Avalonia.Internal

open System
open System.ComponentModel
open System.Threading
open Avalonia
open Avalonia.Data
open Avalonia.Data.Core.Plugins
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Internal
open Avalonia.Utilities

type IAvaloniaBindingTarget =
    abstract member GetProperty : string -> (IValueHolder * Type) option    

type DynamicPropertyAccessor (weakRef : WeakReference, name : string) =    
    inherit PropertyAccessorBase ()

    let mutable wr = weakRef
    let mutable eventRaised = false

    let getVH () = 
        let v = wr.Target 
        match v with
        | :? IAvaloniaBindingTarget as v' ->
            let vh = v'.GetProperty(name)
            match vh with
            | Some vh -> vh
            | None -> failwith "Invalid binding sent to DynamicPropertyAccessor"
        | _ -> failwith "Invalid binding sent to DynamicPropertyAccessor"
    
    member this.SendCurrentValue () = this.PublishValue this.Value

    override __.PropertyType =
        let vh = getVH ()
        snd vh
    override __.Value =
        let vh = getVH ()
        let v = fst vh
        v.GetValue ()
    override __.SetValue (obj, _) =
        let vh = getVH ()
        let v = fst vh
        v.SetValue obj
        true
    override this.SubscribeCore () =
        this.SendCurrentValue ()
        match wr.Target with
        | :? INotifyPropertyChanged as inpc ->
            WeakSubscriptionManager.Subscribe(inpc, "PropertyChanged", this :> IWeakSubscriber<PropertyChangedEventArgs>)
        | _ -> ()
    override this.UnsubscribeCore() =
        match wr.Target with
        | :? INotifyPropertyChanged as inpc ->
            WeakSubscriptionManager.Unsubscribe(inpc, "PropertyChanged", this :> IWeakSubscriber<PropertyChangedEventArgs>)
        | _ -> ()
    
    interface IWeakSubscriber<PropertyChangedEventArgs> with
        member this.OnEvent(sender: obj, e:PropertyChangedEventArgs) =
            if e.PropertyName = name then  
                eventRaised <- true
                this.SendCurrentValue ()

                      

type DynamicPropertyAccessorPlugin () =
    interface IPropertyAccessorPlugin with
        member __.Match (obj, name) =
            match obj with
            | :? IAvaloniaBindingTarget as target -> target.GetProperty name |> Option.isSome
            | _ -> false

        member __.Start (wr, name) = new DynamicPropertyAccessor(wr, name) :> _

type internal AvaloniaBindingTarget<'b>() =
    inherit ObservableBindingSource<'b>()
    
    let properties = System.Collections.Generic.Dictionary<string, IValueHolder*Type>()

    let getProperty name =
        match properties.TryGetValue name with
        | false, _ -> None
        | true, v -> Some v

    override __.AddReadWriteProperty<'a> (name, getter : Func<'a>, setter : Action<'a>) =
        let vh = ValueHolder.readWrite getter setter        

        if properties.ContainsKey name then
            failwith <| sprintf "Property [%s] already exists on this binding source" name
        properties.Add(name, (vh,typeof<'a>))

    override __.AddReadOnlyProperty<'a> (name, getter : Func<'a>) =
        let vh = ValueHolder.readOnly getter 

        if properties.ContainsKey name then
            failwith <| sprintf "Property [%s] already exists on this binding source" name
        properties.Add(name, (vh,typeof<'a>))
    
    override __.CreateObservableBindingSource () =
        new AvaloniaBindingTarget<_>() :> _    
        
    interface IAvaloniaBindingTarget with  
        member __.GetProperty name = getProperty name    

