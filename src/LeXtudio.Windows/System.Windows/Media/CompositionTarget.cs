namespace System.Windows.Media
{
    public sealed class CompositionTarget
    {
        public Matrix TransformFromDevice { get; set; } = Matrix.Identity;
        public Matrix TransformToDevice { get; set; } = Matrix.Identity;
    }
}
