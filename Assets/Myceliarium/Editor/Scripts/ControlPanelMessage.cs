namespace AtMycelia.Myceliarium
{
    public enum ControlPanelMessageType
    {
        Info,
        Warning,
        Error
    }

    public class ControlPanelMessage
    {
        public ControlPanelMessageType Type { get; }
        public string Text { get; }

        public ControlPanelMessage(ControlPanelMessageType type, string text)
        {
            Type = type;
            Text = text;
        }
    }

}