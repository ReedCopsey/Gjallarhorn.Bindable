﻿namespace Gjallarhorn.Bindable.Tests

open Gjallarhorn
open Gjallarhorn.Wpf
open Gjallarhorn.Bindable
open Gjallarhorn.Validation
open Gjallarhorn.Validation.Validators
open System.ComponentModel
open NUnit.Framework
open System

type TestBindingSource<'b>() =
    inherit ObservableBindingSource<'b>()

    override __.AddReadWriteProperty<'a> (name, getter : Func<'a>, setter : Action<'a>) =
        ()
    override __.AddReadOnlyProperty<'a> (name, getter : Func<'a>) =
        ()

    override __.CreateObservableBindingSource () =
        new TestBindingSource<_>() :> _

type PropertyChangedObserver(o : INotifyPropertyChanged) =
    let changes = System.Collections.Generic.Dictionary<string,int>()

    do
        o.PropertyChanged.Add(fun args ->            
            let current = 
                match changes.ContainsKey args.PropertyName with
                | true -> changes.[args.PropertyName]
                | false -> 0
            changes.[args.PropertyName] <- current + 1)

    member __.Item 
        with get(name) = 
            let success, count = changes.TryGetValue name
            match success with
            | true -> count
            | false -> 0

[<TestFixture>]
type BindingSourceTest() =

    let getProperty (source : obj) name =
        let props = TypeDescriptor.GetProperties source
        let prop = props.Find(name, false)
        unbox <| prop.GetValue(source)

    let setProperty (source : obj) name value =
        let props = TypeDescriptor.GetProperties source
        let prop = props.Find(name, false)
        prop.SetValue(source, box value)

    let fullNameValidation (value : string) = 
        match System.String.IsNullOrWhiteSpace(value) with
        | true -> Some "Value must contain at least a first and last name"
        | false ->
            let words = value.Split([|' '|], System.StringSplitOptions.RemoveEmptyEntries)
            if words.Length >= 2 then
                None
            else
                Some "Value must contain at least a first and last name"

    [<OneTimeSetUp>]
    member __.Initialize() =
        Gjallarhorn.Wpf.Platform.install(false) |> ignore

    [<Test>]
    member __.``BindingSource raises property changed`` () =
        use tbt = new TestBindingSource<obj>()

        let obs = PropertyChangedObserver(tbt)    

        let bt = tbt :> BindingSource

        bt.RaisePropertyChanged("Test")
        bt.RaisePropertyChanged("Test")
    
        Assert.AreEqual(2, obs.["Test"])

    [<Test>]
    member __.``BindingSource\TrackObservable tracks a view change`` () =
        use tbt = new TestBindingSource<obj>()

        let value = Mutable.create 0
        let obs = PropertyChangedObserver(tbt)    

        let bt = tbt :> BindingSource

        bt.TrackObservable("Test", value)
        value.Value <- 1
        value.Value <- 2
    
        Assert.AreEqual(2, obs.["Test"])

    [<Test>]
    member __.``BindingSource\TrackObservable ignores view changes with same value`` () =
        use tbt = new TestBindingSource<obj>()

        let value = Mutable.create 0
        let obs = PropertyChangedObserver(tbt)    

        let bt = tbt :> BindingSource

        bt.TrackObservable ("Test", value)
        value.Value <- 1
        value.Value <- 2
        value.Value <- 2
    
        Assert.AreEqual(2, obs.["Test"])

    [<Test>]
    member __.``BindingSource\twoWay raises property changed`` () =
        let v1 = Mutable.create 1
        let v2 = Signal.map (fun i -> i+1) v1
        use dynamicVm = new Gjallarhorn.Wpf.  DesktopBindingSource<obj>() :> BindingSource
        Bind.Explicit.twoWay dynamicVm "Test" v2 |> ignore

        let obs = PropertyChangedObserver(dynamicVm)    
    
        let cur = getProperty dynamicVm "Test" 
        Assert.AreEqual(2, cur)

        v1.Value <- 55 // Change 1
        let cur = getProperty dynamicVm "Test" 
        Assert.AreEqual(56, cur)

        v1.Value <- 66 // Change 2
        v1.Value <- 66 // No Change  - Value the same
        v1.Value <- 77 // Change 3
        Assert.AreEqual(3, obs.["Test"])

    [<Test>]
    member __.``Bind\Explicit\oneWay tracks values properly`` () =
        let first = Mutable.create ""
        let last = Mutable.create ""
        let full = Signal.map2 (fun f l -> f + " " + l) first last

        use dynamicVm = Bind.createSource ()
        
        Bind.Explicit.oneWay dynamicVm "Full" full        
    
        let fullValue() = getProperty dynamicVm "Full"

        Assert.AreEqual(" ", fullValue())

        first.Value <- "Foo"
        Assert.AreEqual("Foo ", fullValue())

        last.Value <- "Bar"
        Assert.AreEqual("Foo Bar", fullValue())

    [<Test>]
    member __.``Bind\Explicit\oneWay raises property changed appropriately`` () =
        let first = Mutable.create ""
        let last = Mutable.create ""
        let full = Signal.map2 (fun f l -> f + " " + l) first last

        use dynamicVm = Bind.createSource ()

        Bind.Explicit.oneWay dynamicVm "First" first 
        Bind.Explicit.oneWay dynamicVm "Last" last
        Bind.Explicit.oneWay dynamicVm "Full" full

        let obs = PropertyChangedObserver(dynamicVm)    

        Assert.AreEqual(0, obs.["Full"])        

        first.Value <- "Foo"
        Assert.AreEqual(1, obs.["Full"])        
        Assert.AreEqual(1, obs.["First"])        

        last.Value <- "Bar"
        Assert.AreEqual(1, obs.["Last"])        
        Assert.AreEqual(2, obs.["Full"])

    [<Test>]
    member __.``Bind\Explicit\twoWayValidated sets error state`` () =
        let first = Mutable.create ""
           
        let last = Mutable.create ""
        let full = Signal.map2 (fun f l -> f + " " + l) first last

        use dynamicVm = Bind.createSource ()

        Bind.Explicit.oneWay dynamicVm "Full" full

        let first' = 
            first
            |> Bind.Explicit.twoWayValidated dynamicVm "First" notNullOrWhitespace 
        let last' = 
            last
            |> Bind.Explicit.twoWayValidated dynamicVm "Last" notNullOrWhitespace
            

        let obs = PropertyChangedObserver(dynamicVm)    

        Assert.IsFalse(dynamicVm.IsValid)
        first.Value <- "Foo"
        Assert.IsFalse(dynamicVm.IsValid)
        Assert.AreEqual(0, obs.["IsValid"])        
        last.Value <- "Bar"
        Assert.IsTrue(dynamicVm.IsValid)
        Assert.AreEqual(1, obs.["IsValid"])        

    [<Test>]
    member __.``Bind\Explicit\toViewValidated sets error state`` () =
        let first = Mutable.create ""
        let last = Mutable.create ""

        let full = Signal.map2 (fun f l -> f + " " + l) first last            

        use dynamicVm = Bind.createSource ()

        Bind.Explicit.oneWay dynamicVm "First" first
        Bind.Explicit.oneWay dynamicVm "Last" last
        Bind.Explicit.toViewValidated dynamicVm "Full" (notNullOrWhitespace >> validateWith fullNameValidation) full

        let obs = PropertyChangedObserver(dynamicVm)    

        Assert.IsFalse(dynamicVm.IsValid)
        Assert.AreEqual(0, obs.["IsValid"])        

        first.Value <- "Foo"
        Assert.IsFalse(dynamicVm.IsValid)
        Assert.AreEqual(0, obs.["IsValid"])        
        last.Value <- "Bar"
        Assert.IsTrue(dynamicVm.IsValid)
        Assert.AreEqual(1, obs.["IsValid"])        

    [<Test>]
    member __.``Bind.Explicit\toViewValidated puts proper errors into INotifyDataErrorInfo`` () =
        let first = Mutable.create ""
        let last = Mutable.create ""
        let full = Signal.map2 (fun f l -> f + " " + l) first last            

        let sub1 = first.Subscribe(fun a -> ())
        let sub2 = last.Subscribe(fun a -> ())

        use dynamicVm = Bind.createSource ()

        Bind.Explicit.oneWay dynamicVm "First" first
        Bind.Explicit.oneWay dynamicVm "Last" last
        Bind.Explicit.toViewValidated dynamicVm "Full" (notNullOrWhitespace >> fixErrors >> validateWith fullNameValidation) full
        dynamicVm.AddDisposable sub1
        dynamicVm.AddDisposable sub2

        let obs = PropertyChangedObserver(dynamicVm)    

        let errors () =
            let inde = dynamicVm :> INotifyDataErrorInfo
            inde.GetErrors("Full")
            |> Seq.cast<string>
            |> Seq.toArray
    
        Assert.IsFalse(dynamicVm.IsValid)
        Assert.AreEqual(0, obs.["IsValid"])        
        Assert.AreEqual(1, errors().Length)        
        Assert.AreEqual("Value cannot be null or empty.", errors().[0])        

        first.Value <- "Foo"
        Assert.IsFalse(dynamicVm.IsValid)
        Assert.AreEqual(0, obs.["IsValid"])        
        Assert.AreEqual(1, errors().Length)        
        Assert.AreEqual("Value must contain at least a first and last name", errors().[0])        

        last.Value <- "Bar"
        Assert.IsTrue(dynamicVm.IsValid)
        Assert.AreEqual(1, obs.["IsValid"])        
        Assert.AreEqual(0, errors().Length)        

        let cur = getProperty dynamicVm "Full" 
        Assert.AreEqual("Foo Bar", cur)

    [<Test>]
    member __.``Bind\Explicit\twoWayMutableValidated tracks valid state properly`` () =
        let value = Mutable.create ""

        use dynamicVm = Bind.createSource ()
        let validator = (notNullOrWhitespace >> hasLengthAtLeast 3)
        Bind.Explicit.twoWayMutableValidated dynamicVm "Value" validator value
    
        Assert.IsFalse dynamicVm.IsValid
        value.Value <- "Valid"

        Assert.IsTrue dynamicVm.IsValid 

        value.Value <- "-"
        Assert.IsFalse dynamicVm.IsValid 

    [<Test>]
    member __.``Bind\Explicit\twoWayMutableValidated assigns from source`` () =
        let value = Mutable.create ""

        use dynamicVm = Bind.createSource ()
        let validator = (notNullOrWhitespace >> hasLengthAtLeast 3)
        Bind.Explicit.twoWayMutableValidated dynamicVm "Value" validator value
    
        Assert.IsFalse dynamicVm.IsValid

        setProperty dynamicVm "Value" "Valid"
        Assert.AreEqual ("Valid", value.Value)

        Assert.IsTrue dynamicVm.IsValid 

        setProperty dynamicVm "Value" "Valid 2"
        Assert.AreEqual ("Valid 2", value.Value)

        Assert.IsTrue dynamicVm.IsValid 

    [<Test>]
    member __.``Bind\Explicit\twoWayMutableValidated assigns back properly`` () =
        let value = Mutable.create ""

        use dynamicVm = Bind.createSource ()
        let validator = (notNullOrWhitespace >> hasLengthAtLeast 3)
        Bind.Explicit.twoWayMutableValidated dynamicVm "Value" validator value
    
        Assert.IsFalse dynamicVm.IsValid

        setProperty dynamicVm "Value" "Valid"
        Assert.AreEqual ("Valid", value.Value)

        Assert.IsTrue dynamicVm.IsValid 

        setProperty dynamicVm "Value" "-"
        
        // Shouldn't push back to mutable since we're invalid!
        Assert.AreEqual ("Valid", value.Value)
        Assert.IsFalse dynamicVm.IsValid 

        setProperty dynamicVm "Value" "Valid 2"
        Assert.AreEqual ("Valid 2", value.Value)

        Assert.IsTrue dynamicVm.IsValid 
