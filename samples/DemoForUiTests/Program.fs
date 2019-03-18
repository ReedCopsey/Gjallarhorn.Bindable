open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml

    type MainWin= XAML<"MainWindow.xaml">

open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

module Program = 
    type Cell = { Value : int }
    type Model = { Items : Cell list; IsOrdered : bool }

    type Msg = | Move of Cell
    type CellMsg = | Click
    let update msg model =
        match msg with
        | Move value ->
            let nitems = value :: List.except [ value ] model.Items
            let sorted = nitems |> List.sort
            if nitems = sorted then { Items = nitems ; IsOrdered = true }
            else { model with Items = nitems }
    
    type ViewModel = 
        {
            Model : Cell
            Click : VmCmd<CellMsg>
        }

    let init = { Items = [ 3; 1; 2 ] |> List.map (fun i -> { Value = i }); IsOrdered = false }
    let cell = { Value = 1 }
    let d = { Model = cell ; Click = Vm.cmd CellMsg.Click }
    
    let cellComponent =
        Component.create [
            <@ d.Model @> |> Bind.oneWay id
            <@ d.Click @> |> Bind.cmd
        ]
    let bindToSource =
        Component.create [
            <@ init.Items @> |> Bind.collection (fun m -> m.Items) cellComponent (snd >> Msg.Move)
            <@ init.IsOrdered @> |> Bind.oneWay (fun m -> m.IsOrdered)
        ]         

    let applicationCore = Framework.application init update bindToSource Nav.empty

open Gjallarhorn.Wpf

[<STAThread>]
[<EntryPoint>]
let main _ =          
    Framework.RunApplication (Navigation.singleViewFromWindow Views.MainWin, Program.applicationCore)
    1