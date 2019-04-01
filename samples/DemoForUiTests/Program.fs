open System

open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

module MenuComponent = 
    [<RequireQualifiedAccess>]
    type MenuItemMsg = 
        | ToIssue21Request
        | ToIssue24Request

    type MenuItem = { 
        IsEnabled : bool
        Text : string
        Command : MenuItemMsg
    } 

    type Menu = { Items : MenuItem list }

    type MenuVM = {
        Menu : Menu
        Request : VmCmd<MenuItemMsg>
    }

    let defMenuItems = 
        [ MenuItemMsg.ToIssue21Request, "Issue21"
          MenuItemMsg.ToIssue24Request, "Issue24" ]
        |> List.map (fun (msg, text) -> {
            Command = msg
            Text = text
            IsEnabled = true
        })

    let defMenu = { Items = defMenuItems  }

    let d = { 
        Menu = defMenu
        Request = Vm.cmd MenuItemMsg.ToIssue21Request
    }

    let update model msg = 
        let items = 
            model.Items
            |> List.map (fun mi -> { mi with IsEnabled = mi.Command <> msg})
        { Items = items }
    
    let menuComponent = 
        Component.create<Menu, unit, MenuItemMsg> [
            <@ d.Menu.Items @> |> Bind.oneWay (fun vm -> vm.Items)
            <@ d.Request @> |> Bind.cmdParam id
        ]

module Issue21Component = 
    type Cell = { Value : int }
    type Cells = { Items : Cell list; IsOrdered : bool }
    [<RequireQualifiedAccess>]
    type CellsMsg = | Move of Cell | New of int
    [<RequireQualifiedAccess>]
    type CellMsg = | Click

    let rnd = Random()
    let create count = 
        let items = 
            [ 1..count ]
            |> List.sortBy (fun _ -> rnd.Next count)
        let isOrdered = 
            items 
            |> Seq.pairwise
            |> Seq.forall (fun (f,s) -> s >= f)
        {
            Items = items |> List.map (fun v -> { Value = v })
            IsOrdered = isOrdered
        }

    let update msg model =
        match msg with
        | CellsMsg.Move value ->
            let nitems = value :: List.except [ value ] model.Items
            let sorted = nitems |> List.sort
            if nitems = sorted then { Items = nitems ; IsOrdered = true }
            else { model with Items = nitems }
        | CellsMsg.New count -> create count

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

    let cellsComponent : IComponent<_,unit,_> =
        Component.create [
            <@ init.Items @> |> Bind.collection (fun m -> m.Items) cellComponent (snd >> CellsMsg.Move)
            <@ init.IsOrdered @> |> Bind.oneWay (fun m -> m.IsOrdered)
        ]

module Issue24Component = 
    type Values = { Items : int list; SelectedIndex : int }
    [<RequireQualifiedAccess>]
    type ValuesMsg = 
        | Click of int
        | Add of int
    [<RequireQualifiedAccess>]
    type ValuesNavMsg = 
        | AddRequest
    let update msg model =
        match msg with
        | ValuesMsg.Click value ->
            { model with SelectedIndex = value }
        | ValuesMsg.Add value ->
            let ind = model.Items.Length
            { SelectedIndex = ind; Items = model.Items @ [ value ] }
        
    type ViewModel = 
        {
            Model : Values
            AddRequest : VmCmd<ValuesNavMsg>
            Add : VmCmd<ValuesMsg>
        }

    let values = [ 0..4 ] 
    let init = { Items = values; SelectedIndex = values |> List.last }
    let d = {
        Model = init
        AddRequest  = Vm.cmd ValuesNavMsg.AddRequest
        Add = Vm.cmd (ValuesMsg.Add 0)
    }
 
    let valuesComponent =
        Component.create [
            <@ init.Items @> |> Bind.oneWay (fun m -> m.Items)
            <@ init.SelectedIndex @> |> Bind.twoWay (fun m -> m.SelectedIndex) ValuesMsg.Click
            <@ d.AddRequest @> |> Bind.cmd |> Bind.toNav
            <@ d.Add @> |> Bind.cmd
        ]

open MenuComponent
open Issue21Component
open Issue24Component

module Program = 

    type Model = {
        Menu : Menu
        Cells : Cells
        Values : Values
    }

    [<RequireQualifiedAccess>]
    type NavMessages = 
        | Issue21
        | Issue24
        | Issue24Dialog
        | StartPage

    [<RequireQualifiedAccess>]
    type Msg = 
        | MenuMsg of MenuItemMsg
        | Issue21Msg of CellsMsg
        | Issue24Msg of ValuesMsg

    let defModel = {
        Menu = MenuComponent.defMenu
        Cells = Issue21Component.init
        Values = Issue24Component.init
    }

    let emptyComponent : IComponent<Model, NavMessages, Msg> = 
        Component.create []

    let menuComponent' = 
        menuComponent
        |> Component.suppressNavigation
        |> Component.withMappedMessages Msg.MenuMsg

    let menuToNavigation = 
        function
        | MenuItemMsg.ToIssue21Request -> NavMessages.Issue21
        | MenuItemMsg.ToIssue24Request -> NavMessages.Issue24

    let cellsComponent' = 
        cellsComponent
        |> Component.suppressNavigation
        |> Component.withMappedMessages Msg.Issue21Msg

    let valuesComponent' = 
        valuesComponent
        |> Component.withMappedNavigation
            (function| ValuesNavMsg.AddRequest -> Some NavMessages.Issue24Dialog)
        |> Component.withMappedMessages Msg.Issue24Msg

    let appComp =
        Component.create<Model, NavMessages, Msg> [
            <@ defModel.Menu @> |> Bind.comp (fun m -> m.Menu) menuComponent' fst
        ]

    let update (nav:Dispatcher<NavMessages>) message model = 
        match message with
        | Msg.Issue21Msg msg ->
            let cells = Issue21Component.update msg model.Cells
            { model with Cells = cells }
        | Msg.MenuMsg msg ->
            menuToNavigation msg |> nav.Dispatch
            let menu = MenuComponent.update model.Menu msg
            { model with Menu = menu }
        | Msg.Issue24Msg msg ->
            let values = Issue24Component.update msg model.Values
            { model with Values = values }

    let applicationCore nav = 
        let n = Dispatcher<NavMessages>()
        Framework.application defModel (update n) appComp nav
        |> Framework.withNavigation n

open Gjallarhorn.Wpf
open Program

[<STAThread>]
[<EntryPoint>]
let main _ =
    let rnd = Random()
    let updateNavigation (app : ApplicationCore<Model,_,_>) request =
        match request with
        | NavMessages.StartPage ->
            Navigation.Page.fromComponent
                Views.createStartPage
                id
                emptyComponent
                id
        | NavMessages.Issue21 ->
            Navigation.Page.fromComponent
                Views.createIssue21
                (fun m -> m.Cells)
                cellsComponent'
                id
        | NavMessages.Issue24 -> 
            Navigation.Page.fromComponent
                Views.createIssue24
                (fun m -> m.Values)
                valuesComponent'
                id
        | NavMessages.Issue24Dialog -> 
            Navigation.Page.dialog
                Views.Issue24Dialog
                (fun m -> m.Values)
                valuesComponent'
                (fun _ -> Msg.Issue24Msg(ValuesMsg.Add (rnd.Next 100)))

    let navigator = 
        Navigation.singlePage
            Windows.Application
            Views.MainWin
            NavMessages.StartPage
            updateNavigation

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0