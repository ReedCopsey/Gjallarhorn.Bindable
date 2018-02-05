namespace SimpleForm

open System

open Gjallarhorn
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework
open Gjallarhorn.Validation
open Gjallarhorn.Validation.Validators

// Note that this program is defined in a PCL, and is completely platform neutral.
// It will work unchanged on WPF, Xamarin Forms, etc

module Program =
    // ----------------------------------     Model     ---------------------------------- 
    // Model contains our first and last name
    type Model = 
        { 
            FirstName : string 
            LastName : string 
        }
    with 
        static member Default = { FirstName = "" ; LastName = "" }

    // ----------------------------------    Update     ---------------------------------- 
    // We define a union type for each possible message
    type Msg = 
        | FirstName of string
        | LastName of string        
        | FullName of first : string * last : string
        | Clear

    // Union type of navigation operations
    type Navigation =
        | Confirm of Model

    // Create a function that updates the model given a message
    let update msg (model : Model) =
        match msg with
        | FirstName f -> { model with FirstName = f }
        | LastName l -> { model with LastName = l }        
        | FullName (f,l) -> { FirstName = f ; LastName = l }
        | Clear -> Model.Default

    // Our "ViewModel". This is optional, but allows you to avoid using "magic strings", as well as enables design time XAML in C# projects
    [<CLIMutable>] // CLIMutable is required by XAML tooling if we have 2-way bindings
    type ViewModel = 
        {
            FirstName   : string
            LastName    : string
            FullName    : string
        }    

    // This is our design/compile time ViewModel used for XAML and binding for naming
    let d = { FirstName = "Reed" ; LastName = "Copsey" ; FullName = "Reed Copsey" }

    // ----------------------------------    Binding    ---------------------------------- 
    // Create a function that binds a model to a source, and outputs messages
    let bindToSource _nav source (model : ISignal<Model>) : IObservable<Msg> list = 
        // Composable validation - Can be written inline as well
        let validLast = notNullOrWhitespace >> notEqual "Copsey" 
        let validFull = notNullOrWhitespace >> fixErrorsWithMessage "Please enter a valid name"

        // Bind the first and last name to the view 2-way with validation
        let first = 
            model
            |> Signal.map (fun m -> m.FirstName)
            |> Bind.Explicit.twoWayValidated source (nameof <@ d.FirstName @>) notNullOrWhitespace 
        let last = 
            model
            |> Signal.map (fun m -> m.LastName)
            |> Bind.Explicit.twoWayValidated source (nameof <@ d.LastName @>) validLast         
        // Display the full name with validation
        model
        |> Signal.map (fun m -> m.FirstName + " " + m.LastName)
        |> Bind.Explicit.toViewValidated source (nameof <@ d.FullName @>) validFull 

        
        // Create our output observables
        [
            // Map the two validation results (string option) together into a signal that's only Some when both are Some (valid)
            // then pipe into an observable message that filters out None (invalid states)
            Signal.mapOption2 (fun f l -> f,l) first last
            |> Observable.choose (Option.map FullName)
        ]           

    // ----------------------------------   Framework  -----------------------------------     
    let applicationCore = Framework.application Model.Default update (Component.fromExplicit bindToSource) Nav.empty
