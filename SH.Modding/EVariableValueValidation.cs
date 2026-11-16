namespace SH.Modding;

public enum EVariableValueValidation
{
    None = 0, // no validation
    NotNullOrEmpty, // (String and Char) not null or ""
    AllowedChars, // (String and Char) a list of allowed chars for a string (also NotNullOrEmpty)
    GT, // greater than validation value (also NotNullOrEmpty)
    GTE, // greater than or equal validation value (also NotNullOrEmpty)
    LT, // less than validation value (also NotNullOrEmpty)
    LTE, // less than or equal validation value (also NotNullOrEmpty)
    InRange, // must be within a min/max validation values (as comma separated, also NotNullOrEmpty)
    InList, // a value from a comma-separated list of validation values (also NotNullOrEmpty)
}
