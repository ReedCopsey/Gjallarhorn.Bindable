module Startup

open System

open ElmInspiredOne
open AvaloniaElmInspiredOne

open Gjallarhorn.Avalonia
open Avalonia
open Avalonia.Logging.Serilog

// ----------------------------------  Application  ---------------------------------- 
[<STAThread>]
[<EntryPoint>]
let main _ =         
    let app () =
        AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug().SetupWithoutStarting().Instance                

    let a = app ()
    let w = MainWindow()
    w.Show ()
    a.Run w
    // let v = Navigation.singleView app MainWindow, Program.applicationCore
    // Framework.RunApplication v
    1