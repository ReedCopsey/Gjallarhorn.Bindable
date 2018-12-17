namespace AvaloniaElmInspiredOne

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml


// The WPF Platform specific bits of this application need to do 2 things:
// 1) They create the view (the actual Window)
// 2) The start the WPF specific version of the framework with the view

// ----------------------------------     View      ---------------------------------- 
// Our platform specific view type

type App () =
    inherit Application ()

    override this.Initialize () = AvaloniaXamlLoader.Load this

type MainWindow () as self = 
    inherit Window ()

    do
        AvaloniaXamlLoader.Load self
