# LazyINotify
An abstract class implementing the [`INotifyPropertyChanging`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanging) and [`INotifyPropertyChanged`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged) interfaces from [`System.ComponentModel`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel) via specialized get/set accessors.

## Namespace tree
```jsonc
LazyINotifyLib [root namespace]
│
├─ Miscellaneous [namespace] <-- Contains auxiliary parts and also an addition in the form of INotifyPropertyGetting interface
│  │
│  ├─ NewInstanceAsDefaultValueAttribute [class derived from Attribute]
│  │
│  ├─ ValueFormattingInstructor [delegate object?]
│  │
│  │
│  ├─ INotifyPropertyGetting [interface]
│  │
│  ├─ PropertyGettingEventHandler [delegate void]
│  │
│  └─ PropertyGettingEventArgs [class derived from EventArgs]
│
└─ LazyINotify [abstract class] <-- Main class
```

## Example of basic usage
```cs
using LazyINotifyLib;
using LazyINotifyLib.Miscellaneous; // For NewInstanceAsDefaultValueAttribute
using System.ComponentModel; // For DefaultValueAttribute

// ...

public class ApplicationViewModelClass : LazyINotify
{
    // Default value is not set
    public string? FirstProperty { get => NotifyGet(); set => NotifySet(value); }


    // Default value for regular types
    [DefaultValue("Default value for the second property")] // Attribute from System.ComponentModel namespace
    public string SecondProperty { get => NotifyGet()!; set => NotifySet(value); }


    // Default value via Activator.CreateInstance()
    [NewInstanceAsDefaultValue(["First argument of the constructor", 2.0])] // Attribute from LazyINotifyLib.Miscellaneous namespace
    public DummyClass ThirdProperty { get => NotifyGet()!; set => NotifySet(value); }

    public class DummyClass(string FirstConstructorArgument, double SecondConstructorArgument)
    {
        public string FirstDummyProperty { get; } = FirstConstructorArgument;
        public double SecondDummyProperty { get; } = SecondConstructorArgument;
    }
}
```

`NewInstanceAsDefaultValueAttribute` is `Attribute` class located in the `LazyINotifyLib.Miscellaneous` namespace.

The target value type of `NotifyGet()` and `NotifySet()` methods is [`dynamic`](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/using-type-dynamic) to avoid needless type casts.
Both methods use [`[CallerMemberName]`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.callermembernameattribute) attribute for the `string PropertyName = ""` argument to identify target property name without constantly specifying it.

- `NotifySet()` invokes `PropertyChanging` and `PropertyChanged` events.
- `NotifyGet()` invokes additional `PropertyGetting` event added from `LazyINotifyLib.Miscellaneous` namespace. 
  > The `PropertyGetting` event is a small addition to justify the name of the `NotifyGet()` method (And just why not).

## `ValueFormattingInstructor` usage
`NotifyGet()` and `NotifySet()` methods also accepts `ValueFormattingInstructor` delegate as an argument. This delegate is located in the `LazyINotifyLib.Miscellaneous` namespace. It allows to change the value through a specified method that must return an `object?`-type value. Alternatively, it can be used as a custom action for additional manipulations before getting/setting a value based on the property name and value to get/set.

Very basic example of said usages:
```cs
using LazyINotifyLib;
using System.ComponentModel; // For DefaultValueAttribute

// ...

public class ApplicationViewModelClass : LazyINotify
{
    [DefaultValue(10.0)] // Testing property
    public double NumberProperty { get => NotifyGet(ValueFormatting: NumberAcquiringNotify); set => NotifySet(value, ValueFormatting: NumberRangeAssertionOnSet); }



    private static object? NumberRangeAssertionOnSet(string PropertyName, object? Value)
    {
        if (Value is double Number)
        {
            if (Number > 50)
            {
                Console.WriteLine($"[{DateTime.Now}] Excessive value '{Number}' received for \"{PropertyName}\".");
                Number = 50;
            }
            else if (Number < 0)
            {
                Console.WriteLine($"[{DateTime.Now}] Insufficient value '{Number}' received for \"{PropertyName}\".");
                Number = 0;
            }

            Value = Number;
        }

        return Value; // -> actually store by LazyINotify.NotifySet()
    }

    private static object? NumberAcquiringNotify(string PropertyName, object? Value)
    {
        Console.WriteLine($"[{DateTime.Now}] The value '{Value}' is returned from the \"{PropertyName}\" property.");
        
        return Value; // -> return from LazyINotify.NotifyGet()
    }
}
```

## `PropertyAcknowledgementLevel` usage
`LazyINotify` class also contains a `protected virtual` property `PropertyAcknowledgementLevel` of type [`System.Reflection.BindingFlags`](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.bindingflags). This property determines which access level properties should be accessible in NotifyGet / NotifySet. Base value is `BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance` (`public` / `private` / `protected`  access levels).

Example of overriding with permission for only `public` properties (`BindingFlags.Public | BindingFlags.Instance`):
```cs
using LazyINotifyLib;
using System.Reflection; // For BindingFlags
using System.ComponentModel; // For DefaultValueAttribute

// ...

public class ApplicationViewModelClass : LazyINotify
{
    // Overriding with permission for only `public` properties
    protected override BindingFlags PropertyAcknowledgementLevel => BindingFlags.Public | BindingFlags.Instance;


    [DefaultValue("Default string 1 (public)")] // public property
    public string PublicStringProperty { get => NotifyGet(); set => NotifySet(value); }

    [DefaultValue("Default string 2 (private)")] // private property
    private string PrivateStringProperty { get => NotifyGet(); set => NotifySet(value); }


    public void ShowAllData()
    {
        // [✓] This will work fine
        Console.WriteLine($"Public string is: \"{PublicStringProperty}\"");

        // [🞩] This will throw an ArgumentException
        Console.WriteLine($"Private string is: \"{PrivateStringProperty}\"");
    }
}
```