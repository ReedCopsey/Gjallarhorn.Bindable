module Startup

open System

open ElmInspiredOne
open AvaloniaElmInspiredOne

open Gjallarhorn.Avalonia
open Avalonia
open Avalonia.Logging.Serilog

// ----------------------------------  Application  ---------------------------------- 

// This is required to use VS Extension Previewer currently
[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp () = AppBuilder.Configure<App>().UsePlatformDetect().LogToDebug()

let app () = buildAvaloniaApp().SetupWithoutStarting().Instance

[<STAThread>]
[<EntryPoint>]
let main _ = 
    Framework.RunApplication (Navigation.singleView app MainWindow, Program.applicationCore)
    1