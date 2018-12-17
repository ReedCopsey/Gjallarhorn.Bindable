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
    let app () = AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug().SetupWithoutStarting().Instance                            
    Framework.RunApplication (Navigation.singleView app MainWindow, Program.applicationCore)
    1