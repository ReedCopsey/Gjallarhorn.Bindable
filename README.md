# Gjallarhorn.Bindable

Unidirectional binding library built on top of Gjallarhorn for WPF, Xamarin Forms and Avalonia. 

To install library into the project use following NuGet-packages:

[Gjallarhorn.Bindable.Wpf](https://www.nuget.org/packages/Gjallarhorn.Bindable.Wpf/1.0.0-beta7)
[Gjallarhorn.Bindable.Avalonia](https://www.nuget.org/packages/Gjallarhorn.Bindable.Avalonia/1.0.0-beta7)
[Gjallarhorn.Bindable.XamarinForms](https://www.nuget.org/packages/Gjallarhorn.Bindable.XamarinForms/1.0.0-beta7)

### Known issues:

1. Using PropertyPath in bindings:

Gjallarhorn.Bindable provides helper properties in form of:

`propertyName + "-Errors"`

and

`propertyName + "-IsValid"`

that can be serve, for example, to show error message separately.

Sample that illustrates applying of those properties in the WPF project is `FrameworkSimpleForm`:

```xml
Text="{Binding FirstName-Errors[0]}"
```

Avalonia doesn't allow using dashes in PropertyName, therefore attempt to use these properties in bindings raises exception inside of Avalonia.
It might be changed in the next versions but for now it's impossible.
See original issue for additional information [#2621 - Chars restriction for PropertyPath](https://github.com/AvaloniaUI/Avalonia/issues/2621)