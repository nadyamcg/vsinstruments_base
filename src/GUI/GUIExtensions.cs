using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Instruments.Files;

namespace Instruments.GUI
{
	//
	// Summary:
	//     This class only exists as a workaround to resolve a native limitation of the GuiElementScrollbar,
	//     which only checks the vertical area of the bounds, which prevents multiple scrollbars placed on
	//     the screen simultaneously from interacting properly.
	internal class GuiElementScrollbarEx : GuiElementScrollbar
	{
		//
		// Summary:
		//     The bounds of the corresponding element that should also intercept scrolling
		//     events as part of this scrollbar. Typically an associated list of items.
		protected ElementBounds ContentBounds { get; private set; }
		//
		// Summary:
		//     Create new scrollbar gui element that only intercepts scrolling events within its bounds or
		//      bounds of its associated content element, if provided.
		public GuiElementScrollbarEx(ICoreClientAPI capi, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBounds)
				: base(capi, onNewScrollbarValue, bounds)
		{
			ContentBounds = contentBounds;
		}
		//
		// Summary:
		//     Resolve the mouse scrolling event, making sure that the event is only consumed if the 
		//     pointer is within the bounds of this or the content element, resolving the native limitation.
		public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
		{
			double mouseX = api.Input.MouseX;
			double mouseY = api.Input.MouseY;

			// Check the direct bounds of the scrollbar only
			if (!Bounds.PointInside(mouseX, mouseY))
			{
				// With defined content bounds, fallback to also checking the bounds
				// of the content. If not inside, terminate the event, preventing any
				// unwated scrolling from external elements.
				if (ContentBounds != null)
				{
					ContentBounds.CalcWorldBounds();
					if (!ContentBounds.PointInside(mouseX, mouseY))
						return;
				}
				else
				{
					return;
				}
			}

			// Fallback to base implementation
			base.OnMouseWheel(api, args);
		}
	}
	//
	// Summary:
	//     This class only exists as a workaround to allow convenient way of handling file tree node view.
	//     It attempts to draw and handle the expand button as part of the element, raising the onExpandClick
	//     action if an item in the collection has the expand clicked.
	internal class GuiElementFlatListEx : GuiElementFlatList
	{
		private ICoreClientAPI ClientAPI { get; set; }
		//
		// Summary:
		//     Texture representing an expanded item in the tree, typically an arrow pointing down.
		private LoadedTexture ExpandedTexture { get; set; }
		//
		// Summary:
		//     Texture representing an expanded item in the tree, typically an arrow pointing right.
		private LoadedTexture CollapsedTexture { get; set; }
		//
		// Summary:
		//     Texture used to draw hover over overlay; not managed by this class, do not dispose manually!
		private LoadedTexture HoverOverlayTexture { get; set; }
		//
		// Summary:
		//     Callback raised when the expand/collapse button is clicked.
		private Action<int> OnExpandClick { get; set; }
		//
		// Summary:
		//     Whether a button was pressed over an element during last event.
		private bool WasMouseDownOnElement { get; set; }
		//
		// Summary:
		//     Whether the composition needs recomposing.
		private bool RecomposeNeeded
		{
			get
			{
				return ExpandedTexture == null
					|| CollapsedTexture == null
					|| HoverOverlayTexture == null
					|| Bounds.RequiresRecalculation;
			}
		}
		//
		// Summary:
		//     Creates new flat extended flat list, that is actually a tree list. Oops!
		public GuiElementFlatListEx(ICoreClientAPI capi, ElementBounds bounds, Action<int> onLeftClick, Action<int> onExpandClick, List<IFlatListItem> elements = null)
			: base(capi, bounds, onLeftClick, elements)
		{
			ClientAPI = api;
			OnExpandClick = onExpandClick;
		}
		//
		// Summary:
		//     Recomposes the GUI for this node.
		public void Recompose(ICoreClientAPI capi)
		{
			ExpandedTexture?.Dispose();
			CollapsedTexture?.Dispose();

			ExpandedTexture = GenTextTexture('▼');
			CollapsedTexture = GenTextTexture('►');

			// This is very dirty, but so is the entirety of this class, sorry. Trying to keep it as close
			// to "vanilla" flat list, but ugh. Worst case scenario it simply won't have a texture on hover.
			FieldInfo field = typeof(GuiElementFlatList).GetField(
					"hoverOverlayTexture",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy
				);
			if (field != null)
			{
				HoverOverlayTexture = (LoadedTexture)field.GetValue(this);
			}
		}
		//
		// Summary:
		//     Generates text texture for provided symbol.
		private LoadedTexture GenTextTexture(char symbol)
		{
			CairoFont font = CairoFont.WhiteSmallText();
			return new TextTextureUtil(ClientAPI).GenTextTexture($"{symbol}", font);
		}
		//
		// Summary:
		//     Handles mouse downs on elements.
		public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
		{
			if (Bounds.ParentBounds.PointInside(args.X, args.Y))
			{
				base.OnMouseDownOnElement(api, args);
				WasMouseDownOnElement = true;
			}
		}
		//
		// Summary:
		//     Handles mouse ups on elements and determines whether the expand or the actual item were pressed.
		public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
		{
			if (!Bounds.ParentBounds.PointInside(args.X, args.Y) || !WasMouseDownOnElement)
			{
				return;
			}

			WasMouseDownOnElement = false;
			int index = 0;
			int mouseX = api.Input.MouseX;
			int mouseY = api.Input.MouseY;
			double absY = insideBounds.absY;
			foreach (IFlatListItem element in Elements)
			{
				if (!element.Visible)
				{
					index++;
					continue;
				}

				float posY = (float)(5.0 + Bounds.absY + absY);
				double padY = GuiElement.scaled(unscalledYPad);

				// Item clicked
				if ((double)mouseX > Bounds.absX && (double)mouseX <= Bounds.absX + Bounds.InnerWidth && (double)mouseY >= (double)posY - padY && (double)mouseY <= (double)posY + GuiElement.scaled(unscaledCellHeight) - padY)
				{
					// Now that we know we are inside of the entire item bounds,
					// we can find its depth and interecpt this event if the expand
					// button is pressed, raising the onExpandClick instead.
					if (element is FileTree.Node node)
					{
						Vec4f b = GetExpandButtonBounds(posY, node.Depth);
						if (GuiExtensions.IsInside(mouseX, mouseY, b))
						{
							api.Gui.PlaySound("menubutton_press");
							OnExpandClick?.Invoke(index);
							args.Handled = true;
							break;
						}
					}

					api.Gui.PlaySound("menubutton_press");
					onLeftClick?.Invoke(index);
					args.Handled = true;
					break;
				}

				absY += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
				index++;
			}
		}
		//
		// Summary:
		//     Returns the base (constant) offset for the expand element.
		double GetConstantExpandOffset()
		{
			return GuiElement.scaled(16);
		}
		//
		// Summary:
		//     Returns bounds for the "expand" area.
		private Vec4f GetExpandButtonBounds(float posY, int depth)
		{
			int width, height;
			if (ExpandedTexture != null)
			{
				width = ExpandedTexture.Width;
				height = ExpandedTexture.Height;
			}
			else
			{
				width = 16;
				height = 16;
			}

			int offset = depth * width;

			return new Vec4f(
				(float)(Bounds.absX + offset),
				posY + (0.25f * height),
				width,
				height);
		}

		public override void RenderInteractiveElements(float deltaTime)
		{
			// Recompose elements needed for drawing
			if (RecomposeNeeded)
			{
				Recompose(ClientAPI);
			}

			int mouseX = api.Input.MouseX;
			int mouseY = api.Input.MouseY;
			bool inside = Bounds.ParentBounds.PointInside(mouseX, mouseY);
			double absY = insideBounds.absY;
			double padY = GuiElement.scaled(unscalledYPad);
			double cellHeight = GuiElement.scaled(unscaledCellHeight);
			foreach (IFlatListItem element in Elements)
			{
				if (element.Visible)
				{
					// Definitely should be added
					float posY = (float)(5.0 + Bounds.absY + absY);
					if (inside && (double)mouseX > Bounds.absX && (double)mouseX <= Bounds.absX + Bounds.InnerWidth && (double)mouseY >= (double)posY - padY && (double)mouseY <= (double)posY + cellHeight - padY)
					{
						if (HoverOverlayTexture != null)
							api.Render.Render2DLoadedTexture(HoverOverlayTexture, (float)Bounds.absX, posY - (float)padY);
					}

					if (absY > -50.0 && absY < Bounds.OuterHeight + 50.0)
					{
						IFlatListExpandable expandable = element as IFlatListExpandable;

						Vec4f expandBounds = GetExpandButtonBounds(posY, expandable.Depth);
						float xOffset = expandBounds.X;
						if (expandable is not FileTree.Node node || node.ChildDirectoryCount > 0)//Currently only folders, but ugh
						{
							LoadedTexture tex = expandable.IsExpanded ? ExpandedTexture : CollapsedTexture;


							Vec4f color = GuiExtensions.IsInside(mouseX, mouseY, expandBounds) ? GuiExtensions.ActiveButtonTextColor : GuiExtensions.DialogDefaultTextColor;
							xOffset += (float)GetConstantExpandOffset();

							api.Render.Render2DTexturePremultipliedAlpha(
								tex.TextureId,
								expandBounds.X,
								expandBounds.Y,
								expandBounds.Z,
								expandBounds.W,
								50.0f,
								color
								);


						}
						else
						{
							xOffset += (float)GetConstantExpandOffset();
						}


						element.RenderListEntryTo(api, deltaTime, xOffset, posY, Bounds.InnerWidth, cellHeight);
					}

					absY += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
				}
			}
		}

		public override void Dispose()
		{
			CollapsedTexture?.Dispose();
			ExpandedTexture?.Dispose();

			base.Dispose();
		}
	}
	//
	// Summary:
	//     Interface for objects that can be drawn as expandable in the flat list ex.
	public interface IFlatListExpandable
	{
		//
		// Summary:
		//     Whether this item is expanded or not.
		public bool IsExpanded { get; }
		//
		// Summary:
		//     The depth of this item in the parent tree.
		public int Depth { get; }
	}
	//
	// Summary:
	//     This class implements GUI extensions methods similarly to follow the game convention.
	internal static class GuiExtensions
	{
		//
		// Summary:
		//     Creates new scroll bar element with optional element bounds that ensures scroll events are only intercepted
		//     while the pointer is within the scrollbar bounds or the content bounds, if provided.
		public static GuiComposer AddVerticalScrollbarEx(this GuiComposer composer, Action<float> onNewScrollbarValue, ElementBounds bounds, ElementBounds contentBound = null, string key = null)
		{
			if (!composer.Composed)
			{
				composer.AddInteractiveElement(new GuiElementScrollbarEx(composer.Api, onNewScrollbarValue, bounds, contentBound), key);
			}

			return composer;
		}
		//
		// Summary:
		//     Creates new flat list element that has covers additional functionality regarding expanding and collapsing file tree nodes.
		public static GuiComposer AddFlatListEx(this GuiComposer composer, ElementBounds bounds, Action<int> onleftClick = null, Action<int> onExpandClick = null, List<IFlatListItem> stacks = null, string key = null)
		{
			if (!composer.Composed)
			{
				composer.AddInteractiveElement(new GuiElementFlatListEx(composer.Api, bounds, onleftClick, onExpandClick, stacks), key);
			}

			return composer;
		}
		//
		// Summary:
		//     Returns whether point (x,y) is specified within bounds.
		public static bool IsInside(float ptX, float ptY, float posX, float posY, float width, float height)
		{
			bool isInside = ptX >= posX && ptX < posX + width &&
							ptY >= posY && ptY < posY + height;
			return isInside;
		}
		//
		// Summary:
		//     Returns whether point (x,y) is specified within bounds.
		public static bool IsInside(float ptX, float ptY, Vec4f bounds)
		{
			return IsInside(ptX, ptY, bounds.X, bounds.Y, bounds.Z, bounds.W);
		}
		//
		// Summary:
		//     Returns the GuiStyle ActiveButtonTextColor as a vector.
		public static Vec4f ActiveButtonTextColor
		{
			get
			{
				return new Vec4f(
					(float)GuiStyle.ActiveButtonTextColor[0],
					(float)GuiStyle.ActiveButtonTextColor[1],
					(float)GuiStyle.ActiveButtonTextColor[2],
					(float)GuiStyle.ActiveButtonTextColor[3]
					);
			}
		}
		//
		// Summary:
		//     Returns the GuiStyle ActiveButtonTextColor as a vector.
		public static Vec4f DialogDefaultTextColor
		{
			get
			{
				return new Vec4f(
					(float)GuiStyle.DialogDefaultTextColor[0],
					(float)GuiStyle.DialogDefaultTextColor[1],
					(float)GuiStyle.DialogDefaultTextColor[2],
					(float)GuiStyle.DialogDefaultTextColor[3]
					);
			}
		}
	}
}
