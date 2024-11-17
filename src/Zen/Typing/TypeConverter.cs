using Zen.Execution;

namespace Zen.Typing;

public static class TypeConverter {
    public static ZenValue Convert(ZenValue value, ZenType targetType, bool TypeCheck = false) {
        if (value.Type == targetType) return value;
        if (TypeCheck && ! TypeChecker.IsCompatible(value.Type, targetType)) {
            throw new RuntimeError($"Cannot convert from {value.Type} to {targetType}", Common.ErrorType.TypeError, null);
        }

        return targetType switch {
            var t when t == ZenType.Integer64 => ConvertToInt64(value),
            var t when t == ZenType.Float => ConvertToFloat(value),
            var t when t == ZenType.Float64 => ConvertToFloat64(value),
            var t when t == ZenType.String => ConvertToString(value),
            _ => throw new RuntimeError($"Unsupported conversion from {value.Type} to {targetType}", Common.ErrorType.TypeError, null)
        };
    }

    private static ZenValue ConvertToInt64(ZenValue value) {
        if (value.Type == ZenType.Integer) {
            return new ZenValue(ZenType.Integer64, (long)(int)value.Underlying);
        }else if (value.Type == ZenType.Integer64) {
            return value;
        }else if (value.Type == ZenType.Float) {
            return new ZenValue(ZenType.Integer64, (long)value.Underlying);
        }else if (value.Type == ZenType.Float64) {
            return new ZenValue(ZenType.Integer64, (long)value.Underlying);
        }

        throw new RuntimeError($"Cannot convert from {value.Type} to int64", Common.ErrorType.TypeError, null);
    }

    private static ZenValue ConvertToFloat(ZenValue value) {
        if (value.Type == ZenType.Integer) {
            return new ZenValue(ZenType.Float, (float)(int)value.Underlying);
        }else if (value.Type == ZenType.Integer64) {
            return new ZenValue(ZenType.Float, (float)(long)value.Underlying);
        } else if (value.Type == ZenType.Float) {
            return value;
        } else if (value.Type == ZenType.Float64) {
            return new ZenValue(ZenType.Float, (float)value.Underlying);
        }

        throw new RuntimeError($"Cannot convert from {value.Type} to float", Common.ErrorType.TypeError, null);
    }

    private static ZenValue ConvertToFloat64(ZenValue value) {
        if (value.Type == ZenType.Integer) {
            return new ZenValue(ZenType.Float64, (double)(int)value.Underlying);
        }else if (value.Type == ZenType.Integer64) {
            return new ZenValue(ZenType.Float64, (double)(long)value.Underlying);
        }else if (value.Type == ZenType.Float) {
            return new ZenValue(ZenType.Float64, (double)value.Underlying);
        }else if (value.Type == ZenType.Float64) {
            return value;
        }

        throw new RuntimeError($"Cannot convert from {value.Type} to float64", Common.ErrorType.TypeError, null);
    }

    private static ZenValue ConvertToString(ZenValue value) {
        if (value.Type == ZenType.Integer) {
            return new ZenValue(ZenType.String, value.Underlying!.ToString());
        }else if (value.Type == ZenType.Integer64) {
            return new ZenValue(ZenType.String, value.Underlying!.ToString());
        }else if (value.Type == ZenType.Float) {
            return new ZenValue(ZenType.String, value.Underlying!.ToString());
        }else if (value.Type == ZenType.Float64) {
            return new ZenValue(ZenType.String, value.Underlying!.ToString());
        }else if (value.Type == ZenType.String) {
            return value;
        }

        throw new RuntimeError($"Cannot convert from {value.Type} to string", Common.ErrorType.TypeError, null);
    }


}