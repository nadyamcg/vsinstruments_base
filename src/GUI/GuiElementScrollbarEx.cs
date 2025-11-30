// Decompiled with JetBrains decompiler
// Type: Instruments.GUI.GuiElementScrollbarEx
// Assembly: vsinstruments_base, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7554D117-662F-4F07-A243-1ECE784371FD
// Assembly location: C:\users\nadya\Desktop\vsinstruments_base(1).dll

using System;
using System.Diagnostics;
using Vintagestory.API.Client;

#nullable disable
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
