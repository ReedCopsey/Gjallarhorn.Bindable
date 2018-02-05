namespace Gjallarhorn.Bindable

open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Gjallarhorn.Bindable.Tests")>]
do ()

[<AutoOpen>]
module internal Utilities =
//module Utilities =
    let internal castAs<'T when 'T : null> (o:obj) = 
    //let castAs<'T when 'T : null> (o:obj) = 
        match o with
        | :? 'T as res -> Some res
        | _ -> None

    let internal downcastAndCreateOption<'T> (o: obj) =
    //let downcastAndCreateOption<'T> (o: obj) =
        match o with
        | :? 'T as res -> Some res
        | _ -> None

    let getPropertyNameFromExpression(expr : Expr) = 
        match expr with
        | PropertyGet(a, pi, list) -> pi.Name
        | _ -> ""

    
[<AutoOpen>]
/// Provides helpers to get the name of a property from a quotation
module NameFromQuotations =
    /// Get the name of a quotation
    let nameof (expr : Expr) =
        getPropertyNameFromExpression expr