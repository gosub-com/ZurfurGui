using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Property;

public enum ZuiPropertyType : byte
{
    Data,
    StyledData,
    Styled,
    Property,
    AttachedProperty,
    AttachedStyle
}

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

public record class ZuiPropertyInfo(
    string Name,
    Type BaseType,
    bool IsNullable,
    ZuiPropertyType PropertyType,
    PropertyKeyId? PropertyKeyId);
