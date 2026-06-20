namespace SH.Framework.IO;

public enum EVersionOperator
{
    any,
    eq,
    lt,
    lte,
    gt,
    gte,
}

public static class VersionOperatorParser
{
    public static EVersionOperator ToOperator(string str) => (str?.ToLowerInvariant()) switch
    {
        "==" or "=" or "eq" or "equal" or "equals" =>
            EVersionOperator.eq,

        "<" or "lt" or "lessthan" =>
            EVersionOperator.lt,

        "<=" or "lte" or "lessthanorequal" or "lessthanorequals" or "lessthanorequalto" =>
            EVersionOperator.lte,

        ">" or "gt" or "greaterthan" =>
            EVersionOperator.gt,

        ">=" or "gte" or "greaterthanorequal" or "greaterthanorequals" or "greaterthanorequalto" =>
            EVersionOperator.gte,

        _ => EVersionOperator.any,
    };

    public static string ToDisplayString(this EVersionOperator op) => op switch
    {
        EVersionOperator.eq => "=",
        EVersionOperator.lt => "<",
        EVersionOperator.lte => "<=",
        EVersionOperator.gt => ">",
        EVersionOperator.gte => ">=",
        _ => "all versions",
    };

}