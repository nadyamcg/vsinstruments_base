using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace VSInstrumentsBase.src.GUI;

internal static class GuiExtensions
{
  public static GuiComposer AddVerticalScrollbarEx(
    this GuiComposer composer,
    Action<float> onNewScrollbarValue,
    ElementBounds bounds,
    ElementBounds contentBound = null,
    string key = null)
  {
    if (!composer.Composed)
      composer.AddInteractiveElement((GuiElement) new GuiElementScrollbarEx(composer.Api, onNewScrollbarValue, bounds, contentBound), key);
    return composer;
  }

  public static GuiComposer AddFlatListEx(
    this GuiComposer composer,
    ElementBounds bounds,
    Action<int> onleftClick = null,
    Action<int> onExpandClick = null,
    List<IFlatListItem> stacks = null,
    string key = null)
  {
    if (!composer.Composed)
      composer.AddInteractiveElement((GuiElement) new GuiElementFlatListEx(composer.Api, bounds, onleftClick, onExpandClick, stacks), key);
    return composer;
  }

  public static bool IsInside(
    float ptX,
    float ptY,
    float posX,
    float posY,
    float width,
    float height)
  {
    return (double) ptX >= (double) posX && (double) ptX < (double) posX + (double) width && (double) ptY >= (double) posY && (double) ptY < (double) posY + (double) height;
  }

  public static bool IsInside(float ptX, float ptY, Vec4f bounds)
  {
    return GuiExtensions.IsInside(ptX, ptY, bounds.X, bounds.Y, bounds.Z, bounds.W);
  }

  public static Vec4f ActiveButtonTextColor
  {
    get
    {
      return new Vec4f((float) GuiStyle.ActiveButtonTextColor[0], (float) GuiStyle.ActiveButtonTextColor[1], (float) GuiStyle.ActiveButtonTextColor[2], (float) GuiStyle.ActiveButtonTextColor[3]);
    }
  }

  public static Vec4f DialogDefaultTextColor
  {
    get
    {
      return new Vec4f((float) GuiStyle.DialogDefaultTextColor[0], (float) GuiStyle.DialogDefaultTextColor[1], (float) GuiStyle.DialogDefaultTextColor[2], (float) GuiStyle.DialogDefaultTextColor[3]);
    }
  }
}
