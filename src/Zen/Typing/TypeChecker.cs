using Zen.Common;

namespace Zen.Typing;

public class TypeChecker {
    public static bool IsCompatible(ZenType source, ZenType target) {
        Logger.Instance.Debug($"Checking type compatibility: source={source}, target={target}");
        Logger.Instance.Debug($"Source type: {source.GetType()}, Target type: {target.GetType()}");

        // Base cases
        if (target == ZenType.Any) {
            Logger.Instance.Debug("Target is Any, returning true");
            return true;
        }
        if (source == target) {
            Logger.Instance.Debug("Source equals target, returning true");
            return true;
        }

        // Handle generic type parameters
        if (target.IsGeneric) {
            Logger.Instance.Debug($"Target is generic parameter {target.Name}");
            // When checking against a generic type parameter,
            // we need to ensure the source type satisfies any constraints
            // For now, we just allow any type to be assigned to a generic parameter
            return true;
        }

        // Handle numeric promotions
        if (source.IsNumeric && target.IsNumeric) {
            Logger.Instance.Debug("Checking numeric promotion");
            return IsValidNumericPromotion(source, target);
        }

        // Handle parametric types (like Container<string>)
        if (source.IsParametric && target.IsParametric) {
            Logger.Instance.Debug($"Both types are parametric: source={source}, target={target}");
            // First check if base types match
            if (source.Name != target.Name) {
                Logger.Instance.Debug("Base types don't match, returning false");
                return false;
            }
            
            // Then check if parameters match
            if (source.Parameters.Length != target.Parameters.Length) {
                Logger.Instance.Debug("Parameter count mismatch, returning false");
                return false;
            }

            // Check each parameter
            for (int i = 0; i < source.Parameters.Length; i++) {
                if (!IsCompatible(source.Parameters[i], target.Parameters[i])) {
                    Logger.Instance.Debug($"Parameter {i} is not compatible");
                    return false;
                }
            }
            
            Logger.Instance.Debug("All parameters are compatible");
            return true;
        }

        // Handle class types
        if (source.IsClass && target.IsClass) {
            return source.Clazz!.IsAssignableFrom(target.Clazz!);
        }

        // Handle nullable types
        // if (target.IsNullable) {
        //     // A non-nullable type can be assigned to its nullable version
        //     if (!source.IsNullable && source.Name == target.Name) {
        //         Logger.Instance.Debug("Non-nullable to nullable assignment allowed");
        //         return true;
        //     }
        //     // Null can be assigned to any nullable type
        //     if (source == ZenType.Null) {
        //         Logger.Instance.Debug("Null to nullable assignment allowed");
        //         return true;
        //     }
        // }

        Logger.Instance.Debug("No compatibility rules matched, returning false");
        return false;
    }

    private static bool IsValidNumericPromotion(ZenType source, ZenType target) {
        // Allow widening conversions only
        if (source == ZenType.Integer) {
            return target == ZenType.Integer64 || target == ZenType.Float || target == ZenType.Float64;
        }
        if (source == ZenType.Float) {
            return target == ZenType.Float64;
        }
        if (source == ZenType.Integer64) {
            return target == ZenType.Float64;
        }
        return false;
    }
}
