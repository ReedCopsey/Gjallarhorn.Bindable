namespace Gjallarhorn.Avalonia

open System.Threading
open Avalonia.Data.Core.Plugins
open Avalonia.Threading
open Gjallarhorn.Bindable

/// Platform installation
module Platform =
    open Avalonia.Data.Core

    let private creation (typ : System.Type) =
        let sourceType = typedefof<Gjallarhorn.Avalonia.Internal.AvaloniaBindingTarget<_>>.MakeGenericType([|typ|])
        System.Activator.CreateInstance(sourceType) 
    
    // Gets, and potentially installs, the Avalonia synchronization context
    let private installAndGetSynchronizationContext () =
        AvaloniaSynchronizationContext.InstallIfNeeded ()
        SynchronizationContext.Current

    /// Installs Avalonia targets for binding into Gjallarhorn
    [<CompiledName("Install")>]
    let install installSynchronizationContext =        
        ExpressionObserver.PropertyAccessors.Insert(0, Gjallarhorn.Avalonia.Internal.DynamicPropertyAccessorPlugin() :> IPropertyAccessorPlugin)

        Gjallarhorn.Bindable.Bind.Implementation.installCreationFunction (fun _ -> creation typeof<obj>) creation

        match installSynchronizationContext with
        | true -> installAndGetSynchronizationContext ()
        | false -> SynchronizationContext.Current

module App =                    
    let toApplicationSpecification (navigator : Framework.INavigator<'Model, 'Nav, 'Message>) (appCore : Framework.ApplicationCore<'Model, 'Nav, 'Message>) : Framework.ApplicationSpecification<'Model,'Nav,'Message> = 
            { 
                Core = appCore
                Render = navigator.Run appCore
            }                

/// Avalonia Specific implementation of the Application Framework
[<AbstractClass;Sealed>]
type Framework =
    /// Run an application given an Application generator, Window generator, and other required information
    static member RunApplication<'Model,'Nav,'Message when 'Model : equality> (navigator : Framework.INavigator<'Model,'Nav,'Message>, applicationInfo : Framework.ApplicationCore<'Model,'Nav,'Message>) =        
        Framework.Framework.runApplication (App.toApplicationSpecification navigator applicationInfo) 
    