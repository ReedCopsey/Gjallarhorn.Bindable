[<RequireQualifiedAccess>]
module Views

open FsXaml

type MainWin = XAML<"MainWindow.xaml">

type MenuView = XAML<"MenuView.xaml">

open System.Windows
open System.Windows.Controls

let create (control : FrameworkElement) =
    let dock = DockPanel(LastChildFill=true)
    DockPanel.SetDock(control, Dock.Left)

    MenuView () |> dock.Children.Add |> ignore
    dock.Children.Add control |> ignore

    dock :> FrameworkElement

type StartPage = XAML<"StartPage.xaml">
let createStartPage = StartPage >> create

type Issue21 = XAML<"Issue21.xaml">
let createIssue21 = Issue21 >> create

type Issue24 = XAML<"Issue24.xaml">
let createIssue24 = Issue24 >> create




