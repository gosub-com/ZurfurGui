using System;
using System.Collections.Generic;
using System.Text;



namespace ZurfurGui.Property;

/// <summary>
/// Specifies how a data property is bound to a view property or style.
/// </summary>
public enum ZuiPropertyType : byte
{
    Data,
    StyledData,
    Styled,
    Property,
    AttachedProperty,
    AttachedStyle
}

/// <summary>
/// Provides helper operations for <see cref="ZuiPropertyType"/> values.
/// </summary>
public static class ZuiPropertyTypeExtensions
{
    extension (ZuiPropertyType bindType)
    {
        public bool IsAttached =>
            bindType is ZuiPropertyType.AttachedProperty or ZuiPropertyType.AttachedStyle;
        public bool IsProperty =>
            bindType is ZuiPropertyType.Property or ZuiPropertyType.AttachedProperty;
    }
}

/// <summary>
/// Describes a property declared in a ZUI data definition.
/// </summary>
public record class ZuiPropertyInfo(
    string Name,
    Type BaseType,
    bool IsNullable,
    ZuiPropertyType PropertyType,
    PropertyKeyId? PropertyKeyId);
