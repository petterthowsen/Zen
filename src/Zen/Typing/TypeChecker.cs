namespace Zen.Typing;

public class TypeChecker {
    public static bool IsCompatible(ZenType source, ZenType target) {
        // Base cases
        if (target == ZenType.Any) return true;
        if (source == target) return true;
        
        // Handle numeric promotions
        if (source.IsNumeric && target.IsNumeric) {
            return IsValidNumericPromotion(source, target);
        }

        // Handle parametric types
        if (source.IsParametric && target.IsParametric) {
            return CheckParametricTypeCompatibility(source, target);
        }

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

    private static bool CheckParametricTypeCompatibility(ZenType source, ZenType target) {
        if (source.Name != target.Name) return false;
        if (source.Parameters.Length != target.Parameters.Length) return false;
        
        for (int i = 0; i < source.Parameters.Length; i++) {
            if (!IsCompatible(source.Parameters[i], target.Parameters[i])) {
                return false;
            }
        }
        
        return true;
    }
}