using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using GuiLabs.Canvas.Events;
using GuiLabs.Canvas.Renderer;
using GuiLabs.Canvas.Shapes;
using GuiLabs.Canvas.Utils;

namespace GuiLabs.Canvas
{
	public class DrawWindow : System.Windows.Forms.UserControl, IDrawWindow
	{
		#region Component (not relevant)

		/// <summary>
		/// Constructor. Does practically nothing and doesn't need anything.
		/// </summary>
		public DrawWindow()
		{
			InitializeComponent();
		}

		private System.ComponentModel.Container components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// To tell the control to receive all possible keys it can.
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns>true to receive all possible key events.</returns>
		protected override bool IsInputKey(System.Windows.Forms.Keys keyData)
		{
			return true;
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// DrawWindow
			// 
			this.Name = "DrawWindow";
			this.Size = new System.Drawing.Size(378, 274);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.DrawWindow_Paint);

		}

		#endregion

		public event RepaintHandler Repaint;
		public event RegionRepaintHandler RegionRepaint;

		#region IDrawWindow

		/// <summary>
		/// DrawWindow hat einen Renderer.
		/// Alle exemplare teilen denselben Renderer.
		/// </summary>
		public IRenderer Renderer
		{
			get
			{
				return RendererSingleton.Instance;
			}
		}

		/// <summary>
		/// Größe des Fensters.
		/// </summary>
		private Rect mBounds = new Rect();
		public new Rect Bounds
		{
			get
			{
				return mBounds;
			}
		}

		#endregion

		#region Painting and redrawing

		#region Redraw

		#region Redraw(Rect)

		/// <summary>
		/// Whole redraw cycle - raises main Repaint event 
		/// for the clients to redraw themselves.
		/// </summary>
		/// <remarks>Uses a back buffer, which is being copied to the screen.</remarks>
		/// <param name="ToRedraw">A rectangular area which should be updated (redrawn)</param>
		public void Redraw(Rect ToRedraw)
		{
			// size of the backbuffer to use
			Renderer.ClientSize = this.ClientSize;
			
			// first, clear the buffer with the background color
			Renderer.Clear();
			
			// Commented out caret functionality from here.
			// Let the TextBox draw the caret instead.
			//								Kirill
			
			// hide the caret and see if anyone needs it
			//Caret textCursor = RendererSingleton.MyCaret;
			//textCursor.Visible = false;

			// raise main Repaint event
			// clients (those who draw on this DrawWindow)
			// handle the event and draw everything there
			// (they use the Renderer that they become over a parameter)
			if (Repaint != null)
				Repaint(Renderer);
			
			// if someone needed the cursor, he/she turned it on
			// so we draw the cursor only if someone needs it
			// (e.g. an active TextBox control)
			//Renderer.DrawOperations.DrawCaret(textCursor);

			// finally, copy the buffer to the screen
			Renderer.RenderBuffer(this, ToRedraw);
		}

		#endregion

		#region Redraw(Rectangle)

		private Rect conversionRect = new Rect();
		public void Redraw(Rectangle ToRedraw)
		{
			conversionRect.Set(ToRedraw);
			Redraw(conversionRect);
		}

		#endregion

		#region Redraw()

		public void Redraw()
		{
			conversionRect.Set(this.ClientRectangle);
			Redraw(conversionRect);
		}

		#endregion

		#region Redraw(IDrawableRect)

		public void Redraw(IDrawableRect ShapeToRedraw)
		{
			if (RegionRepaint != null)
			{
				RegionRepaint(Renderer, ShapeToRedraw);
			}

			// finally, copy the buffer to the screen
			Renderer.RenderBuffer(this, ShapeToRedraw.Bounds);
		}

		#endregion

		#endregion

		private void DrawWindow_Paint(object sender, PaintEventArgs e)
		{
			Redraw(e.ClipRectangle);
		}

		protected override void OnResize(System.EventArgs e)
		{
			mBounds.Size.Set(this.ClientSize.Width, this.ClientSize.Height);
			base.OnResize(e);
		}

		#endregion

		#region Raise mouse events

		#region MouseEventArgs

		private MouseEventArgsWithKeys WrapMouseEventArgs(MouseEventArgs e)
		{
			MouseEventArgsWithKeys m = new MouseEventArgsWithKeys(e);
			return m;
		}

		private System.Drawing.Point GetCursorPosition()
		{
			System.Drawing.Point CursorPos = System.Windows.Forms.Control.MousePosition;
			return this.PointToClient(CursorPos);
		}

		private MouseEventArgsWithKeys PrepareMouseArgs()
		{
			System.Drawing.Point CursorPos = GetCursorPosition();
			MouseEventArgsWithKeys Args = new MouseEventArgsWithKeys(
				Control.MouseButtons, 0, CursorPos.X, CursorPos.Y, 0);
			return Args;
		}

		#endregion

		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			//if (Click != null)
			//    Click(PrepareMouseArgs());
			OnClick(PrepareMouseArgs());
		}

		protected override void OnDoubleClick(EventArgs e)
		{
			base.OnDoubleClick(e);
			//if (DoubleClick != null)
			//    DoubleClick(PrepareMouseArgs());
			OnDoubleClick(PrepareMouseArgs());
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			//if (MouseDown != null)
			//    MouseDown(WrapMouseEventArgs(e));
			OnMouseDown(WrapMouseEventArgs(e));
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			//if (MouseMove != null)
			//    MouseMove(WrapMouseEventArgs(e));
			OnMouseMove(WrapMouseEventArgs(e));
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			//if (MouseUp != null)
			//    MouseUp(WrapMouseEventArgs(e));
			OnMouseUp(WrapMouseEventArgs(e));
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			//if (MouseWheel != null)
			//    MouseWheel(WrapMouseEventArgs(e));
			OnMouseWheel(WrapMouseEventArgs(e));
		}

		protected override void OnMouseHover(EventArgs e)
		{
			base.OnMouseHover(e);
			//if (MouseHover != null)
			//    MouseHover(PrepareMouseArgs());
			OnMouseHover(PrepareMouseArgs());
		}

		#region MouseHandler

		protected IMouseHandler mDefaultMouseHandler;
		/// <summary>
		/// If not null, all mouse events are being redirected to this object
		/// </summary>
		public virtual IMouseHandler DefaultMouseHandler
		{
			get
			{
				return mDefaultMouseHandler;
			}
			set
			{
				if (NextHandlerValid(value))
				{
					mDefaultMouseHandler = value;
				}
				else
				{
					mDefaultMouseHandler = null;
				}
			}
		}

		/// <summary>
		/// Can we set such a DefaultMouseHandler?
		/// Prevents endless recursive loops.
		/// </summary>
		/// <param name="nextHandler">Canditate to test</param>
		/// <returns>true, if setting DefaultMouseHandler to nextHandler causes no recursion.</returns>
		public bool NextHandlerValid(IMouseHandler nextHandler)
		{
			// setting to null is perfectly fine
			// (turning off the redirection)
			if (nextHandler == null)
			{
				return true;
			}

			// setting to itself would cause
			// an infinite recursion
			if (nextHandler == this)
			{
				return false;
			}

			IMouseHandler current = nextHandler;
			while (current != null)
			{
				current = current.DefaultMouseHandler;
				if (current == this)
				{
					return false;
				}
			}

			return true;
		}

		public virtual void OnClick(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnClick(e);
			}
		}

		public virtual void OnDoubleClick(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnDoubleClick(e);
			}
		}

		public virtual void OnMouseDown(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnMouseDown(e);
			}
		}

		/// <summary>
		/// Occures when the user hovers the mouse over the block.
		/// </summary>
		/// <param name="e"></param>
		public virtual void OnMouseHover(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnMouseHover(e);
			}
		}

		public virtual void OnMouseMove(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnMouseMove(e);
			}
		}

		public virtual void OnMouseUp(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnMouseUp(e);
			}
		}

		public virtual void OnMouseWheel(MouseEventArgsWithKeys e)
		{
			if (DefaultMouseHandler != null)
			{
				DefaultMouseHandler.OnMouseWheel(e);
			}
		}

		#endregion

		#endregion

		#region KeyHandler

		private IKeyHandler mDefaultKeyHandler;
		/// <summary>
		/// If not null, all keyboard events 
		/// are being redirected to this object
		/// </summary>
		public IKeyHandler DefaultKeyHandler
		{
			get
			{
				return mDefaultKeyHandler;
			}
			set
			{
				if (NextHandlerValid(value))
				{
					mDefaultKeyHandler = value;
				}
				else
				{
					mDefaultKeyHandler = null;
				}
			}
		}

		/// <summary>
		/// Can we set such a DefaultKeyHandler?
		/// Prevents endless recursive loops.
		/// </summary>
		/// <param name="nextHandler">Canditate to test</param>
		/// <returns>true, if setting DefaultKeyHandler to nextHandler causes no recursion.</returns>
		public bool NextHandlerValid(IKeyHandler nextHandler)
		{
			// setting to null is perfectly fine
			// (turning off the redirection)
			if (nextHandler == null)
			{
				return true;
			}

			// setting to itself would cause
			// an infinite recursion
			if (nextHandler == this)
			{
				return false;
			}

			IKeyHandler current = nextHandler;
			while (current != null)
			{
				current = current.DefaultKeyHandler;
				if (current == this)
				{
					return false;
				}
			}
			return true;
		}

		protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (DefaultKeyHandler != null)
			{
				DefaultKeyHandler.OnKeyDown(e);
			}
		}

		protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
			if (DefaultKeyHandler != null)
			{
				DefaultKeyHandler.OnKeyPress(e);
			}
		}

		protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (DefaultKeyHandler != null)
			{
				DefaultKeyHandler.OnKeyUp(e);
			}
		}

		#endregion
	}
}
