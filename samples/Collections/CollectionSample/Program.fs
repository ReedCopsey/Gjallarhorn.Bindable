namespace CollectionSample

open System
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

// Note that this program is defined in a PCL, and is completely platform neutral.
// It will work unchanged on WPF, Xamarin Forms, etc

module Program =
    open Requests
    
    let [<Literal>] minAddTime = 1500
    let [<Literal>] addRandomness = 1500
    let [<Literal>] processThreshold = 3.0

    // Generate a message to add a random new element to the list on a regular basis
    // In a "real" application, this would likely be doing something like
    // asynchronously calling out to a service and adding in new items
    // Accepts a cancelation token used to stop processing
    let startUpdating (dispatch : Dispatch<CollectionApplication.Msg>) token =        
        let rnd = Random()
        let rec wf () = 
            async {
                // minAddTime to (minAddTime+addRandomness) seconds sleep between additions
                do! Async.Sleep <| minAddTime + rnd.Next(addRandomness)                                
                
                Requests.AddNew(Guid.NewGuid(), rnd.NextDouble() * 500.0)
                |> CollectionApplication.Msg.Update
                |> dispatch

                return! wf()
            }

        Async.Start(wf(), cancellationToken = token)       

    // Purge processed elements from the list as time goes by at random intervals
    let startProcessing (dispatch : Dispatch<CollectionApplication.Msg>) token =        
        let rec wf () = async {
            // On half second intervals, purge anything processed more than processThreshold seconds ago
            do! Async.Sleep 250

            TimeSpan.FromSeconds(processThreshold)
            |> CollectionApplication.Process
            |> dispatch

            return! wf()
        }

        Async.Start(wf(), cancellationToken = token)        
    

    // Build our core application
    let applicationCore nav =
        // These are external "executors" which allows us to start and control a process which pumps messages
        let adding = new Executor<_>(startUpdating)
        let processing = new Executor<_>(startProcessing)

        // This is a dispatcher that lets us pump messages back into the application as needed
        let updates = Dispatcher<CollectionApplication.Msg>()

        // Start these processes - if we don't do this, the toggles will be off by default
        adding.Start()
        processing.Start()        

        Framework.application (CollectionApplication.buildInitialModel adding processing) nav (CollectionApplication.update updates) CollectionApplication.appComponent
        |> Framework.withDispatcher updates
        |> Framework.withDispatcher adding 
        |> Framework.withDispatcher processing
