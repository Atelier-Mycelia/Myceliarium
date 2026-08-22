namespace AtMycelia.Myceliarium
{
    /// <summary>
    /// Represents a subwindow in the Myceliarium ControlPanel that displays a specific 
    /// category of items or controls.
    /// </summary>
    public abstract class CategorySubwindow :  ICategorySubwindow
    {
        public abstract void Init(bool forceReinit = false);
    }

    public interface ICategorySubwindow
    {
        void Init(bool forceReinit = false);
    }
}