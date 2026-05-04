using System;
using System.Collections.Generic;
using System.Text;

namespace ZurfurGui.Property;

public record class DataPropertyInfo(string Name, Type BaseType, bool IsNullable);
