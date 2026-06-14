namespace System.Windows.Controls;

public static class Validation
{
    public static bool GetHasError(DependencyObject element)
        => element switch
        {
            DataGridCell { HasValidationError: true } => true,
            DataGridRow { HasRowValidationError: true } => true,
            _ => false
        };
}
