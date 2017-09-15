namespace ElmInspiredOne

open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

// Note that this program is defined in a PCL, and is completely platform neutral.
// It will work unchanged on WPF, Xamarin Forms, etc
module Program =    
    // ----------------------------------     Model     ---------------------------------- 
    // Model is a simple integer for counter
    type Model = { Value : int }
    
    // Create an initialization function
    let initModel i = { Value = i }

    // ----------------------------------    Update     ---------------------------------- 
    // We define a union type for each possible message
    type Msg = 
        | Increment 
        | Decrement

    // Create a function that updates the model given a message
    let update msg model =
        match msg with
        | Increment -> { Value = min 10 (model.Value + 1) }
        | Decrement -> { Value = max 0 (model.Value - 1) }

    // Our "ViewModel". This is optional, but allows you to avoid using "magic strings", as well as enables design time XAML in C# projects
    type ViewModel = 
        {
            Current : int 
            Increment : VmCmd<Msg>
            Decrement : VmCmd<Msg>
        }    

    // This is our design/compile time ViewModel used for XAML and binding for naming
    let d = { Current = 5 ; Increment = Vm.cmd Increment; Decrement = Vm.cmd Decrement }
         
    // ----------------------------------    Binding    ---------------------------------- 
    // Create a function that binds a model to a source, and outputs messages
    // This essentially acts as our "view" in Elm terminology, though it doesn't actually display 
    // the view as much as map from our type to bindings
    let bindToSource =           
        // Create our bindings - the VM type defines the name, the Bind call determines the type of data binding
        Component.create [
            <@ d.Current    @> |> Bind.oneWay (fun m -> m.Value)
            <@ d.Increment  @> |> Bind.cmdIf (fun m -> m.Value < 10)
            <@ d.Decrement  @> |> Bind.cmdIf (fun m -> m.Value > 0)
        ]         

    // ----------------------------------   Framework  -----------------------------------     
    let applicationCore = Framework.application (initModel 5) update bindToSource Nav.empty
