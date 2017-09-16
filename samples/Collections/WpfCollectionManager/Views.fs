namespace Views

open FsXaml

// ----------------------------------     View      ---------------------------------- 
// Our platform specific view type
type App = XAML<"App.xaml">
type MainWin = XAML<"MainWindow.xaml">
type RequestView = XAML<"RequestView.xaml">
type ProcessControl = XAML<"ProcessControl.xaml">
type LoginControl = XAML<"LoginControl.xaml">

type RequestDialogBase = XAML<"RequestDialog.xaml">
type RequestDialog() =
    inherit RequestDialogBase()

    override this.CloseClick (_sender, _e) = this.Close()

