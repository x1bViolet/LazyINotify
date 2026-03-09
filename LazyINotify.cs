using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using LazyINotifyLib.Miscellaneous;

namespace LazyINotifyLib
{
    namespace Miscellaneous
    {
        /// <summary>
        /// Specifies the default value for a property to create it via <see cref="Activator.CreateInstance(Type)"/> / <see cref="Activator.CreateInstance(Type, object?[]?)"/> from <see cref="LazyINotify"/> constructor.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class NewInstanceAsDefaultValueAttribute(object?[]? ConstructorArguments = null) : Attribute
        {
            public object?[]? ConstructorArgsuments { get; } = ConstructorArguments;
        }

        /// <summary>
        /// Used by <see cref="NotifyGet"/> and <see cref="NotifySet"/> to modify the output/input value.
        /// <br/><br/>
        /// Code block from <see cref="NotifyGet"/> with actual usage when returning stored property value (ValueFormatting returns a possibly modified value):
        /// <code>return ValueFormatting != null ? ValueFormatting(PropertyName, ReturnValue) : ReturnValue</code>
        /// </summary>
        public delegate object? ValueFormattingInstructor(string PropertyName, object? OriginalValue);




        /// <summary>
        /// Notifies clients that a property value is being getted.
        /// </summary>
        public interface INotifyPropertyGetting
        {
            /// <summary>
            /// Occurs before the property value is getted.
            /// </summary>
            public event PropertyGettingEventHandler? PropertyGetting;
        }


        /// <summary>
        /// Represents the method that will handle the <see cref="INotifyPropertyGetting.PropertyGetting"/> event of an <see cref="INotifyPropertyGetting"/> interface.
        /// </summary>
        /// <param name="Sender">The source of the event.</param>
        /// <param name="Args">A <see cref="PropertyGettingEventArgs"/> that contains the event data.</param>
        public delegate void PropertyGettingEventHandler(object? Sender, PropertyGettingEventArgs Args);

        /// <summary>
        /// Provides data for the <see cref="INotifyPropertyGetting.PropertyGetting"/> event.
        /// </summary>
        /// <param name="PropertyName">The name of the property whose value is being getted</param>
        public class PropertyGettingEventArgs(string? PropertyName) : EventArgs
        {
            /// <summary>
            /// The name of the property whose value is being getted
            /// </summary>
            public virtual string? PropertyName { get; } = PropertyName;
        }
    }





    /// <summary>
    /// • Implements <see cref="INotifyPropertyChanged.PropertyChanged"/> and <see cref="INotifyPropertyChanging.PropertyChanging"/> features through alt <see langword="set"/> accessor (Plus custom <see cref="Miscellaneous.INotifyPropertyGetting"/> for <see langword="get"/>).<br/>
    /// • Usage: <c>{ get => NotifyGet(); set => NotifySet(value); }</c><br/>
    /// (<see cref="NotifySet"/> invokes <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> <see langword="events"/>, and stores property value internally)<br/>
    /// (<see cref="NotifyGet"/> invokes <see cref="PropertyGetting"/> and returns value stored by <c>NotifySet</c>, otherwise default for property type if value is not set (<see langword="null"/> / <see cref="Activator.CreateInstance(Type)"/>))
    /// <br/><br/>
    /// • <see cref="ValueFormattingInstructor"/> <see langword="delegate"/> at the <c>NotifyGet</c> or <c>NotifySet</c> methods can change out/in value on <see langword="get"></see>/<see langword="set"/>.
    /// <br/><br/>
    /// • Use <see cref="DefaultValueAttribute"/> to set default value for properties with this advanced <see langword="get"/> and <see langword="set"/>.<br/>
    /// • Use <see cref="NewInstanceAsDefaultValueAttribute"/> to set <see langword="new"/>() as default value (Available with constructor args via <see langword="params"/> <see langword="object"/>[] args for Attribute).
    /// </summary>
    public abstract class LazyINotify : INotifyPropertyChanging, INotifyPropertyChanged, INotifyPropertyGetting
    {
        /// <summary>
        /// Values stored by <see cref="NotifySet"/>.
        /// </summary>
        private readonly Dictionary<string, dynamic?> LazyINotify__StoredPropertyValues = [];

        /// <summary>
        /// <see cref="PropertyInfo"/> dictionary used by <see cref="NotifyGet"/> to determine the nullability of the return value for property when its value is not set.
        /// </summary>
        private readonly Dictionary<string, PropertyInfo> LazyINotify__ThisProperties = [];



        public event PropertyGettingEventHandler? PropertyGetting;
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyGetting([CallerMemberName] string PropertyName = "") => PropertyGetting?.Invoke(this, new PropertyGettingEventArgs(PropertyName));
        public void OnPropertyChanging([CallerMemberName] string PropertyName = "") => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(PropertyName));
        public void OnPropertyChanged([CallerMemberName] string PropertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));



        private void ThrowUnknownPropertyName(string PropertyName) =>
            throw new ArgumentException
            (
                $"\n\n" +
                $"The property named \"{PropertyName}\" is not defined in the internal LazyINotify's property dictionary. " +
                $"Means you used `NotifyGet()` / `NotifySet(value)` from the method (And then method name was passed through `[CallerMemberName]` as a property name) " +
                $"or changed `protected virtual PropertiesAcknowledgementLevel` (And then access level of this property was not taken into account when creating the dictionary (Current acknowledgement level: [{this.PropertyAcknowledgementLevel}]))." +
                $"\n"
            );

        /// <summary>
        /// Uses <see cref="CallerMemberNameAttribute"/> for <paramref name="PropertyName"/>.
        /// <br/><br/>
        /// Invoking <see cref="PropertyChanging"/> before storing the value, <see cref="PropertyChanged"/> after.
        /// </summary>
        protected void NotifySet(dynamic? ValueToSet, ValueFormattingInstructor? ValueFormatting = null, [CallerMemberName] string PropertyName = "")
        {
            if (LazyINotify__ThisProperties.ContainsKey(PropertyName) == false) ThrowUnknownPropertyName(PropertyName);

            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(PropertyName));

            if (ValueFormatting != null) ValueToSet = ValueFormatting(PropertyName, ValueToSet);
            LazyINotify__StoredPropertyValues[PropertyName] = ValueToSet; // technically `field = value`

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        /// <summary>
        /// Uses <see cref="CallerMemberNameAttribute"/> for <paramref name="PropertyName"/>
        /// <br/><br/>
        /// Invoking <see cref="PropertyGetting"/> before <see langword="return"/>.
        /// </summary>
        protected dynamic? NotifyGet(ValueFormattingInstructor? ValueFormatting = null, [CallerMemberName] string PropertyName = "")
        {
            if (LazyINotify__ThisProperties.ContainsKey(PropertyName) == false) ThrowUnknownPropertyName(PropertyName);

            PropertyGetting?.Invoke(this, new PropertyGettingEventArgs(PropertyName));

            dynamic? ValueToReturn = LazyINotify__StoredPropertyValues.TryGetValue(PropertyName, out dynamic? StoredPropertyValue)
                ? ValueFormatting != null
                    ? ValueFormatting(PropertyName, StoredPropertyValue)
                    : StoredPropertyValue
                : Nullable.GetUnderlyingType(LazyINotify__ThisProperties[PropertyName].PropertyType) != null | !LazyINotify__ThisProperties[PropertyName].PropertyType.IsValueType
                    ? null
                    : Activator.CreateInstance(LazyINotify__ThisProperties[PropertyName].PropertyType);

            return ValueToReturn;
        }



        /// <summary>
        /// Determines which access level properties should be considered when creating the internal dictionary of properties accessible by <see cref="NotifyGet"/> / <see cref="NotifySet"/>.
        /// <br/><br/>
        /// Default value is <see cref="BindingFlags.Public"/> | <see cref="BindingFlags.NonPublic"/> | <see cref="BindingFlags.Instance"/> | <see cref="BindingFlags.Static"/> (Means <see langword="public"/> / <see langword="private"/> / <see langword="protected"/>).
        /// </summary>
        protected virtual BindingFlags PropertyAcknowledgementLevel => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;



        /// <summary>
        /// Constructor sets default property values via <see cref="DefaultValueAttribute"/> or <see cref="NewInstanceAsDefaultValueAttribute"/> attributes
        /// </summary>
        public LazyINotify()
        {
            static bool PropertyHasAttribute<AttributeType>(PropertyInfo TargetProperty, out AttributeType AcquiredAttribute) where AttributeType : Attribute
            {
                AcquiredAttribute = TargetProperty.GetCustomAttribute<AttributeType>()!;
                return AcquiredAttribute != null;
            }

            foreach (PropertyInfo SelfProperty in this.GetType().GetProperties(this.PropertyAcknowledgementLevel))
            {
                LazyINotify__ThisProperties[SelfProperty.Name] = SelfProperty;

                if (PropertyHasAttribute(SelfProperty, out System.ComponentModel.DefaultValueAttribute DefaultValueAttribute))
                {
                    SelfProperty.SetValue(this, DefaultValueAttribute.Value);
                }
                else if (PropertyHasAttribute(SelfProperty, out LazyINotifyLib.Miscellaneous.NewInstanceAsDefaultValueAttribute NewInstanceDefValue))
                {
                    if (NewInstanceDefValue.ConstructorArgsuments != null)
                    {
                        SelfProperty.SetValue(this, Activator.CreateInstance(SelfProperty.PropertyType, NewInstanceDefValue.ConstructorArgsuments));
                    }
                    else
                    {
                        SelfProperty.SetValue(this, Activator.CreateInstance(SelfProperty.PropertyType));
                    }
                }
            }
        }
    }
}