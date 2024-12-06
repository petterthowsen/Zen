namespace Zen.Typing;

public interface IZenClass
{
    public string Name { get; set; }
    public ZenType Type { get; set; }
    public List<Parameter> Parameters { get; set; }

    public struct Parameter {
        public string Name;
        public ZenType Type;
        public ZenValue DefaultValue;

        public bool IsTypeParameter => Type == ZenType.Type;
        public bool IsValueParameter => !IsTypeParameter;

        public Parameter(string name, ZenType type, ZenValue? defaultValue = null) {
            Name = name;
            Type = type;  // Keep the original type (ZenType.Type for type parameters)
            
            if (defaultValue != null) {
                DefaultValue = (ZenValue) defaultValue;
            }
            else if (IsTypeParameter) {
                // For type parameters, default to ZenType.Any
                DefaultValue = new ZenValue(ZenType.Type, ZenType.Any);
            }
            else {
                DefaultValue = ZenValue.Null;
            }
        }

        public bool ValidateValue(ZenValue value) {
            if (IsTypeParameter) {
                // For type parameters, value must be a ZenType.Type
                // and its underlying value must be a ZenType
                return value.Type == ZenType.Type;
            }
            return Type.IsAssignableFrom(value.Type);
        }
    }

    public bool IsAssignableFrom(IZenClass other);
}