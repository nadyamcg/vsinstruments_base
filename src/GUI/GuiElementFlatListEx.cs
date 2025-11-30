using VSInstrumentsBase.src.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace VSInstrumentsBase.src.GUI;

internal class GuiElementFlatListEx : GuiElementFlatList
{
  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private ICoreClientAPI ClientAPI { get; set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private LoadedTexture ExpandedTexture { get; set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private LoadedTexture CollapsedTexture { get; set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private LoadedTexture HoverOverlayTexture { get; set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private Action<int> OnExpandClick { get; set; }

  [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
  private bool WasMouseDownOnElement { get; set; }

  private bool RecomposeNeeded
  {
    get
    {
      return this.ExpandedTexture == null || this.CollapsedTexture == null || this.HoverOverlayTexture == null || ((GuiElement) this).Bounds.RequiresRecalculation;
    }
  }

  public GuiElementFlatListEx(
    ICoreClientAPI capi,
    ElementBounds bounds,
    Action<int> onLeftClick,
    Action<int> onExpandClick,
    List<IFlatListItem> elements = null)
    : base(capi, bounds, onLeftClick, elements)
  {
    this.ClientAPI = capi;
    this.OnExpandClick = onExpandClick;
  }

  public void Recompose(ICoreClientAPI capi)
  {
    this.ExpandedTexture?.Dispose();
    this.CollapsedTexture?.Dispose();
    this.ExpandedTexture = this.GenTextTexture('▼');
    this.CollapsedTexture = this.GenTextTexture('►');
    FieldInfo field = typeof (GuiElementFlatList).GetField("hoverOverlayTexture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
    if (field != null)
      return;
    this.HoverOverlayTexture = (LoadedTexture) field.GetValue((object) this);
  }

  private LoadedTexture GenTextTexture(char symbol)
  {
    CairoFont cairoFont = CairoFont.WhiteSmallText();
    return new TextTextureUtil(this.ClientAPI).GenTextTexture($"{symbol}", cairoFont, (TextBackground) null);
  }

  public virtual void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
  {
    if (!((GuiElement) this).Bounds.ParentBounds.PointInside(args.X, args.Y))
      return;
    base.OnMouseDownOnElement(api, args);
    this.WasMouseDownOnElement = true;
  }

  public virtual void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
  {
    if (!((GuiElement) this).Bounds.ParentBounds.PointInside(args.X, args.Y) || !this.WasMouseDownOnElement)
      return;
    this.WasMouseDownOnElement = false;
    int num1 = 0;
    int mouseX = api.Input.MouseX;
    int mouseY = api.Input.MouseY;
    double absY = this.insideBounds.absY;
    foreach (IFlatListItem element in this.Elements)
    {
      if (!element.Visible)
      {
        ++num1;
      }
      else
      {
        float posY = (float) (5.0 + ((GuiElement) this).Bounds.absY + absY);
        double num2 = GuiElement.scaled((double) this.unscalledYPad);
        if ((double) mouseX > ((GuiElement) this).Bounds.absX && (double) mouseX <= ((GuiElement) this).Bounds.absX + ((GuiElement) this).Bounds.InnerWidth && (double) mouseY >= (double) posY - num2 && (double) mouseY <= (double) posY + GuiElement.scaled((double) this.unscaledCellHeight) - num2)
        {
          if (element is FileTree.Node node)
          {
            Vec4f expandButtonBounds = this.GetExpandButtonBounds(posY, node.Depth);
            if (GuiExtensions.IsInside((float) mouseX, (float) mouseY, expandButtonBounds))
            {
              api.Gui.PlaySound("menubutton_press", false, 1f);
              Action<int> onExpandClick = this.OnExpandClick;
              if (onExpandClick != null)
                onExpandClick(num1);
              args.Handled = true;
              break;
            }
          }
          api.Gui.PlaySound("menubutton_press", false, 1f);
          Action<int> onLeftClick = this.onLeftClick;
          if (onLeftClick != null)
            onLeftClick(num1);
          args.Handled = true;
          break;
        }
        absY += GuiElement.scaled((double) (this.unscaledCellHeight + this.unscaledCellSpacing));
        ++num1;
      }
    }
  }

  private double GetConstantExpandOffset() => GuiElement.scaled(16.0);

  private Vec4f GetExpandButtonBounds(float posY, int depth)
  {
    int num1;
    int num2;
    if (this.ExpandedTexture != null)
    {
      num1 = this.ExpandedTexture.Width;
      num2 = this.ExpandedTexture.Height;
    }
    else
    {
      num1 = 16 ;
      num2 = 16 ;
    }
    return new Vec4f((float) ((GuiElement) this).Bounds.absX + (float) (depth * num1), posY + 0.25f * (float) num2, (float) num1, (float) num2);
  }

  public virtual void RenderInteractiveElements(float deltaTime)
  {
    if (this.RecomposeNeeded)
      this.Recompose(this.ClientAPI);
    int mouseX = this.ClientAPI.Input.MouseX;
    int mouseY = this.ClientAPI.Input.MouseY;
    bool flag = ((GuiElement) this).Bounds.ParentBounds.PointInside(mouseX, mouseY);
    double absY = this.insideBounds.absY;
    double num1 = GuiElement.scaled((double) this.unscalledYPad);
    double num2 = GuiElement.scaled((double) this.unscaledCellHeight);
    foreach (IFlatListItem element in this.Elements)
    {
      if (element.Visible)
      {
        float posY = (float) (5.0 + ((GuiElement) this).Bounds.absY + absY);
        if (flag && (double) mouseX > ((GuiElement) this).Bounds.absX && (double) mouseX <= ((GuiElement) this).Bounds.absX + ((GuiElement) this).Bounds.InnerWidth && (double) mouseY >= (double) posY - num1 && (double) mouseY <= (double) posY + num2 - num1 && this.HoverOverlayTexture != null)
          this.ClientAPI.Render.Render2DLoadedTexture(this.HoverOverlayTexture, (float) ((GuiElement) this).Bounds.absX, posY - (float) num1, 50f);
        if (absY > -50.0 && absY < ((GuiElement) this).Bounds.OuterHeight + 50.0)
        {
          IFlatListExpandable flatListExpandable = element as IFlatListExpandable;
          Vec4f expandButtonBounds = this.GetExpandButtonBounds(posY, flatListExpandable.Depth);
          float x = expandButtonBounds.X;
          float num3;
          if (!(flatListExpandable is FileTree.Node node) || node.ChildDirectoryCount > 0)
          {
            LoadedTexture loadedTexture = flatListExpandable.IsExpanded ? this.ExpandedTexture : this.CollapsedTexture;
            Vec4f vec4f = GuiExtensions.IsInside((float) mouseX, (float) mouseY, expandButtonBounds) ? GuiExtensions.ActiveButtonTextColor : GuiExtensions.DialogDefaultTextColor;
            num3 = x + (float) this.GetConstantExpandOffset();
            this.ClientAPI.Render.Render2DTexturePremultipliedAlpha(loadedTexture.TextureId, expandButtonBounds.X, expandButtonBounds.Y, expandButtonBounds.Z, expandButtonBounds.W, 50f, vec4f);
          }
          else
            num3 = x + (float) this.GetConstantExpandOffset();
          element.RenderListEntryTo(this.api, deltaTime, (double) num3, (double) posY, ((GuiElement) this).Bounds.InnerWidth, num2);
        }
        absY += GuiElement.scaled((double) (this.unscaledCellHeight + this.unscaledCellSpacing));
      }
    }
  }

  public virtual void Dispose()
  {
    this.CollapsedTexture?.Dispose();
    this.ExpandedTexture?.Dispose();
    base.Dispose();
  }
}
