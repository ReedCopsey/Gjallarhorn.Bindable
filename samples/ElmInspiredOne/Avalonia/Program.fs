module Startup

open System

open ElmInspiredOne
open AvaloniaElmInspiredOne

open Gjallarhorn.Avalonia
open Avalonia
open Avalonia.Logging

// ----------------------------------  Application  ---------------------------------- 

// This is required to use VS Extension Previewer currently
[<CompiledName "BuildAvaloniaApp">] 
let buildAvaloniaApp () = AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace<AppBuilder>(LogEventLevel.Warning)

let app () = buildAvaloniaApp().SetupWithoutStarting().Instance

[<STAThread>]
[<EntryPoint>]
let main _ = 
    Framework.RunApplication (Navigation.singleView app MainWindow, Program.applicationCore)
    1