namespace DemoForUiTests

// Our tree types
type Location = { X: float; Y: float }
type Tree = { Position : Location ; Height : float ; Decorated : bool ; Lit : bool }

// Update types allowed on a tree
type TreeMessage = | Decorate | Light | DecorateOrLight

// Module showing allowed operations on an existing tree
module Tree =
    let private rnd = System.Random()
    let private makeHeight () = 8.0 + rnd.NextDouble() * 4.0

    let create location = 
        { Position = location ; Height = makeHeight () ; Decorated = false ; Lit = false }

    let update msg tree =
        match msg with
        | Decorate -> { tree with Decorated = true }
        | Light -> { tree with Lit = true }
        | DecorateOrLight ->
            match tree.Decorated, tree.Lit with
            | false, _ -> { tree with Decorated = true }
            | true, false -> { tree with Lit = true }
            | true, true -> { tree with Decorated = false ; Lit = false}

// Our main forest model
type Forest = Tree list

// Update types allowed on a forest
type ForestMessage =
    | Add of Location option // Add new tree at a location
    | UpdateTree of msg : TreeMessage * tree : Tree // Update an existing tree
    | Prune of maxTrees : int  // Prune the trees

// Module with allowed operations on a forest
module Forest =
    let private rnd = System.Random()

    let empty : Forest = []
    
    // Prune one tree if we're over the max size
    let private prune max (forest : Forest) : Forest = 
        let l = List.length forest
        if max < l then
            // Remove an "older" tree, from the 2nd half of the list
            let indexToRemove = rnd.Next ( l / 2, l)
            forest 
            |> List.mapi (fun i t -> (i <> indexToRemove, t))
            |> List.filter fst
            |> List.map snd
        else
            forest         

    let update msg forest =
        match msg with
            | Add(location)         -> 
                match location with
                | Some l -> Tree.create l :: forest    
                | None   -> forest
            | UpdateTree(msg, tree) -> Tree.update msg tree :: List.except [ tree ] forest
            | Prune(maxTrees)       -> prune maxTrees forest


open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

module Program =

    // "VM" types can be used in XAML designer, and allow simplified component construction
    type TreeVM =
        {
            Tree            : Tree
            Decorate        : VmCmd<TreeMessage>
            Light           : VmCmd<TreeMessage>
            DecorateOrLight : VmCmd<TreeMessage>
        }
    let treeDesign = { Tree = { Position = { X = 0.0 ; Y = 0.0 } ; Height = 1.0 ; Decorated = true ; Lit = true } ; Decorate = Vm.cmd Decorate ; Light = Vm.cmd Light ; DecorateOrLight = Vm.cmd DecorateOrLight }

    type ForestVM =
        {
            Forest     : Forest
            Add        : VmCmd<ForestMessage>            
        }
    let forestDesign = { Forest = Forest.empty ; Add = Vm.cmd (Add None) }

    // Create binding for a single tree.  This will output Decorate and Light messages
    let treeComponent =
        Component.create<Tree,unit,TreeMessage> [
            <@ treeDesign.Tree @>            |> Bind.oneWay id
            <@ treeDesign.Decorate @>        |> Bind.cmd
            <@ treeDesign.Light @>           |> Bind.cmd
            <@ treeDesign.DecorateOrLight @> |> Bind.cmd
        ]

    // Create binding for entire application.  This will output all of our messages.
    let forestComponent =
        Component.create<Forest,unit,ForestMessage> [
            <@ forestDesign.Forest @> |> Bind.collection id treeComponent UpdateTree
            <@ forestDesign.Add @>    |> Bind.cmdParam Add
        ]
    
    let pruneHandler (dispatch : Dispatch<_>) token =        
        // Handle pruning of the forest - 
        // Twice per second, send a prune message to remove a tree if there are more than max
        let rec pruneForever max =
            async {
                do! Async.Sleep 500                
                Prune max |> dispatch
                return! pruneForever max 
            }
    
        // Start prune loop in the background asynchronously
        Async.Start(pruneForever 10, token)

    let application nav =       
        // Start pruning "loop"
        let prune = new Executor<_,_>(pruneHandler)
        prune.Start()
        // Start our application
        Framework.application Forest.empty Forest.update forestComponent nav
        |> Framework.withDispatcher prune