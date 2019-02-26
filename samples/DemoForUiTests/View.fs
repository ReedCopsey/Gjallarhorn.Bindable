namespace Views

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Input

open FsXaml
open DemoForUiTests

module internal MouseConverters =
    // Create a converter from mouse clicks on a Canvas to Some(location), and clicks elsewhere to None
    let locationConverter (args : MouseEventArgs) =
        match args.OriginalSource with
        | :? Canvas ->
            let source = args.OriginalSource :?> IInputElement
            let pt = args.GetPosition(source)
            Some { X = pt.X; Y = pt.Y }
        | _ -> None

// Create our converter from MouseEventArgs -> Location
type LocationConverter() = inherit EventArgsConverter<MouseEventArgs, Location option>(MouseConverters.locationConverter, None)
// Create our Window
type MainWindow = XAML<"MainWindow.xaml"> 

module Main =    
    [<STAThread>]
    [<EntryPoint>]
    let main _ =  
        // Run using the WPF wrappers around the basic application framework    
        let nav = Gjallarhorn.Wpf.Navigation.singleView System.Windows.Application MainWindow
        let app = Program.application nav.Navigate
        Gjallarhorn.Wpf.Framework.RunApplication<Forest,unit,ForestMessage> (nav, app)
        0