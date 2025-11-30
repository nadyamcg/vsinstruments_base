// Decompiled with JetBrains decompiler
// Type: Instruments.GUI.NoteGUI
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using Vintagestory.API.Client;

#nullable disable
namespace VSInstrumentsBase.src.GUI;

public class NoteGUI : HudElement
{
  public virtual string ToggleKeyCombinationCode => (string) null;

  public NoteGUI(ICoreClientAPI capi)
    : base(capi)
  {
    this.SetupDialog();
  }

  private void SetupDialog()
  {
    ElementBounds elementBounds1 = ElementStdBounds.AutosizedMainDialog.WithAlignment((EnumDialogArea) 7);
    elementBounds1.fixedY -= 70.0;
    ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 20.0, 30.0, 20.0);
    ElementBounds elementBounds3 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
    elementBounds3.BothSizing = (ElementSizing) 2;
    elementBounds3.WithChildren(new ElementBounds[1]
    {
      elementBounds2
    });
    this.SingleComposer = GuiElementDynamicTextHelper.AddDynamicText(GuiComposerHelpers.AddShadedDialogBG(this.capi.Gui.CreateCompo("NoteDialog", elementBounds1), elementBounds3, true, 5.0, 0.75f), "No note selected!", CairoFont.WhiteDetailText(), elementBounds2, "note").Compose(true);
  }

  public void UpdateText(string newText)
  {
    GuiElementDynamicTextHelper.GetDynamicText(this.SingleComposer, "note").SetNewText(newText, false, false, false);
  }
}
