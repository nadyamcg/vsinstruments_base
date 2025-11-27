using Vintagestory.API.Client;

namespace Instruments.GUI
{
    public class NoteGUI : HudElement
    {
        public override string ToggleKeyCombinationCode => null;

        public NoteGUI(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
        }

        private void SetupDialog()
        {
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterBottom);
            dialogBounds.fixedY -= 70;
            ElementBounds textBounds = ElementBounds.Fixed(0, 20, 30, 20);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);
            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("NoteDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDynamicText("No note selected!", CairoFont.WhiteDetailText(), textBounds, "note")
                .Compose();
        }

        public void UpdateText(string newText)
        {
            // Called when the text needs to change. Update the SingleComposer's Dynamic text field.
            SingleComposer.GetDynamicText("note").SetNewText(newText);
        }
    }
}
