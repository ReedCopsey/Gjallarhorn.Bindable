namespace CollectionSample

open System
open Gjallarhorn
open Gjallarhorn.Bindable

module Credentials = 
    // Simple, poorly designed authentication for demo purposes
    type AuthenticationStatus =
        | Unknown
        | Querying
        | InvalidPassword
        | Approved

    type User = 
        {
            User : string            
            AuthenticationStatus : AuthenticationStatus
        }

    // These are the updates that can be performed on our requests
    type Message = 
        | TryAuthenticate of user : string * password : string
        | SetAuthenticationStatus of user : string * status : AuthenticationStatus
    
    // For illustration, we always fail the first round
    let private loginAttempts = ref 0
    // Subscription which asynchronously handles authentication attempts
    let handleAuthenticationAttempt (msg : Message) _currentModel =
        match msg with
        | TryAuthenticate(user,_pass) ->
            async {
                // Synth pushing out to some async "verification" for this...
                do! Async.Sleep 250
                
                incr loginAttempts

                let rnd = System.Random ()
                let result = 
                    if !loginAttempts > 1 && rnd.NextDouble () > 0.5 then
                        SetAuthenticationStatus(user, AuthenticationStatus.Approved)
                    else                        
                        SetAuthenticationStatus(user, AuthenticationStatus.InvalidPassword)

                return Some result
            } 
        | _ -> async { return None }

        
    // Update the model based on an UpdateRequest
    let rec update msg current =        
        match msg with
        | TryAuthenticate(user,pass) -> { current with AuthenticationStatus = Querying }
        | SetAuthenticationStatus(user,status) -> { User = user ; AuthenticationStatus = status }

    type CredentialViewModel =
        {
            User     : string
            Password : string
            Status   : string
            Submit   : VmCmd<DateTime>
        }
    let cd = { User = "Test User" ; Password = "Test pass" ; Status = "Enter credentials..." ; Submit = Vm.cmd DateTime.Now }
    
    // Create the component for the Requests as a whole.
    // Note that this uses BindingCollection to map the collection to individual request -> messages,
    // using the component defined previously, then maps this to the model-wide update message.
    let credentialBindings _nav source (model : ISignal<User>) : IObservable<Message> list =      

        let statusToString = function
            | Unknown -> "Enter user and password:"
            | InvalidPassword -> "Password incorrect. Please re-enter:"
            | Approved -> "Approved"
            | Querying -> "Checking password... Please wait."

        let status = model |> Signal.map (fun m -> statusToString m.AuthenticationStatus)

        let pw = Mutable.create ""

        Bind.Explicit.oneWay source (nameof <@ cd.Status @>) status
        let user = Bind.Explicit.twoWay source (nameof <@ cd.User @>) (model |> Signal.map (fun m -> m.User) )

        Bind.Explicit.twoWayMutable source (nameof <@ cd.Password @>) pw

        let canSubmit = Signal.map3 (fun (u : string) (p : string) (m : User) -> u.Length > 0 && p.Length > 0 && m.AuthenticationStatus <> AuthenticationStatus.Querying && m.AuthenticationStatus <> AuthenticationStatus.Approved) user pw model
        let submit = Bind.Explicit.createCommandChecked (nameof <@ cd.Submit @>) canSubmit source

        [
            // On click, submit 
            submit |> Observable.map (fun _ -> TryAuthenticate(user.Value, pw.Value))
        ]

    let credentialComponent = 
        Component.fromExplicit<User,CollectionNav,Message> credentialBindings
        |> Component.withSubscription handleAuthenticationAttempt
        |> Component.toSelfUpdating update 