namespace AvaloniaElmInspiredOne

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

// ----------------------------------     View      ---------------------------------- 
// Our platform specific view type

type App () =
    inherit Application ()

    override this.Initialize () = AvaloniaXamlLoader.Load this

type MainWindow () as self = 
    inherit Window ()

    do
        AvaloniaXamlLoader.Load self
    #if DEBUG
    do self.AttachDevTools()
    #endif