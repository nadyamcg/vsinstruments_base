using System;
using System.Diagnostics;
using Vintagestory.API.Client;


namespace VSInstrumentsBase.src.GUI;

internal class GuiElementScrollbarEx : GuiElementScrollbar
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  protected ElementBounds ContentBounds { get; private set; }

  public GuiElementScrollbarEx(
    ICoreClientAPI capi,
    Action<float> onNewScrollbarValue,
    ElementBounds bounds,
    ElementBounds contentBounds)
    : base(capi, onNewScrollbarValue, bounds)
  {
    this.ContentBounds = contentBounds;
  }

  public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
  {
    double mouseX = (double) api.Input.MouseX;
    double mouseY = (double) api.Input.MouseY;
    if (!((GuiElement) this).Bounds.PointInside(mouseX, mouseY))
    {
      if (this.ContentBounds == null)
        return;
      this.ContentBounds.CalcWorldBounds();
      if (!this.ContentBounds.PointInside(mouseX, mouseY))
        return;
    }
    base.OnMouseWheel(api, args);
  }
}
