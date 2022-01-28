using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;
using WindowsInput;
using SharpDX.XInput;
using System.Runtime.InteropServices;

namespace XBoxAsMouse
{
	public class XBoxControllerAsMouse
	{
		[DllImport("User32")]
		private static extern int keybd_event(Byte bVk, Byte bScan, long dwFlags, long dwExtraInfo);
		private const byte UP = 2;
		private const byte CTRL = 17;
		private const byte ESC = 27;

		private int MovementDivider = 20000;
		private int ScrollDivider = 20000;
		private int RefreshRate = 60;

		private System.Threading.Timer _timer;
		private Controller _controller;
		private IMouseSimulator _mouseSimulator;
		private IKeyboardSimulator _sim;

		private bool _wasRSDown;
		private bool _wasLSDown;
		private bool _wasStartDown;
		private bool _wasBackDown;
		private bool _wasXDown;

		private int interval = 0;
		//dpad key
		/*
		private bool _wasUpDown;
		private bool _wasDownDown;
		private bool _wasLeftDown;
		private bool _wasRightDown;
		*/

		public XBoxControllerAsMouse()
		{
			_controller = new Controller(UserIndex.One);
			_mouseSimulator = new InputSimulator().Mouse;
			_sim = new InputSimulator().Keyboard;
			_timer = new System.Threading.Timer(obj => Update());
		}

		public void Start(int clickInterval, int mouseSpeed, int scrollSpeed, int rRate)
		{
			interval = rRate - clickInterval;
			MovementDivider = mouseSpeed;
			ScrollDivider = scrollSpeed;
			RefreshRate = rRate;
			_timer.Change(0, 1000 / RefreshRate);
		}

		private void Update()
		{
			if(IsForegroundFullScreen())
            {
				_timer.Change(0, 1);
			}
            else
            {
				_timer.Change(0, 1000 / RefreshRate);
				_controller.GetState(out var state);
				IntervalUpdate();
				//update
				Movement(state);
				if (interval > 12)
				{
					Scroll(state);
					LeftButton(state);
					RightButton(state);
					WinKey(state);
					EscKey(state);
					TabKey(state);
					interval = 0;
				}
			}

			//arrow keys
			/*
			Up(state);
			Down(state);
			Left(state);
			Right(state);
			*/
		}

		private void IntervalUpdate()
        {
			interval++;
			if (interval < 0) interval = 20;
        }
		//hard code every button
		private void RightButton(State state)
		{
			var isLSDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder);
			if (isLSDown && !_wasLSDown) _mouseSimulator.RightButtonDown();
			else if (!isLSDown && _wasLSDown) _mouseSimulator.RightButtonUp();
			_wasLSDown = isLSDown;
		}

		private void LeftButton(State state)
		{
			var isRSDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.RightShoulder);
			if (isRSDown && !_wasRSDown) _mouseSimulator.LeftButtonDown();
			else if (!isRSDown && _wasRSDown) _mouseSimulator.LeftButtonUp();
			_wasRSDown = isRSDown;
		}

		private void Scroll(State state)
		{
			var x = state.Gamepad.RightThumbX / ScrollDivider;
			var y = state.Gamepad.RightThumbY / ScrollDivider;
			_mouseSimulator.HorizontalScroll(x);
			_mouseSimulator.VerticalScroll(y);
		}

		private void Movement(State state)
		{
			var x = state.Gamepad.LeftThumbX / MovementDivider;
			var y = state.Gamepad.LeftThumbY / MovementDivider;
			_mouseSimulator.MoveMouseBy(x, -y);
		}

		private void WinKey(State state)
		{
			var isStartDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Start);
			if (isStartDown && !_wasStartDown) _sim.KeyDown(VirtualKeyCode.LWIN);
			else if (!isStartDown && _wasStartDown) _sim.KeyUp(VirtualKeyCode.LWIN);
			_wasStartDown = isStartDown;
		}
		private void EscKey(State state)
		{
			var isBackDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.Back);
			if (isBackDown && !_wasBackDown) _sim.KeyDown(VirtualKeyCode.ESCAPE);
			else if (!isBackDown && _wasBackDown) _sim.KeyUp(VirtualKeyCode.ESCAPE);
			_wasBackDown = isBackDown;
		}
		private void TabKey(State state)
		{
			var isXDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.X);
			if (isXDown && !_wasXDown) _sim.KeyDown(VirtualKeyCode.TAB);
			else if (!isXDown && _wasXDown) _sim.KeyUp(VirtualKeyCode.TAB);
			_wasXDown = isXDown;
		}
		//arrow (dpad) keys
		/*
		private void Up(State state)
		{
			var isUpDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadUp);
			if (isUpDown && !_wasUpDown) _sim.KeyDown(VirtualKeyCode.UP);
			else if (!isUpDown && _wasUpDown) _sim.KeyUp(VirtualKeyCode.UP);
			_wasUpDown = isUpDown;
		}
		private void Down(State state)
		{
			var isDownDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadDown);
			if (isDownDown && !_wasDownDown) _sim.KeyDown(VirtualKeyCode.DOWN);
			else if (!isDownDown && _wasDownDown) _sim.KeyUp(VirtualKeyCode.DOWN);
			_wasDownDown = isDownDown;
		}
		private void Left(State state)
		{
			var isLeftDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadLeft);
			if (isLeftDown && !_wasLeftDown) _sim.KeyDown(VirtualKeyCode.LEFT);
			else if (!isLeftDown && _wasLeftDown) _sim.KeyUp(VirtualKeyCode.LEFT);
			_wasLeftDown = isLeftDown;
		}
		private void Right(State state)
		{
			var isRightDown = state.Gamepad.Buttons.HasFlag(GamepadButtonFlags.DPadRight);
			if (isRightDown && !_wasRightDown) _sim.KeyDown(VirtualKeyCode.RIGHT);
			else if (!isRightDown && _wasRightDown) _sim.KeyUp(VirtualKeyCode.RIGHT);
			_wasRightDown = isRightDown;
		}
		*/
		//detect full screen app
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		public static bool IsForegroundFullScreen()
		{
			return IsForegroundFullScreen(null);
		}


		public static bool IsForegroundFullScreen(System.Windows.Forms.Screen screen)
		{

			if (screen == null)
			{
				screen = System.Windows.Forms.Screen.PrimaryScreen;
			}
			RECT rect = new RECT();
			IntPtr hWnd = (IntPtr)GetForegroundWindow();


			GetWindowRect(new HandleRef(null, hWnd), ref rect);

			/* in case you want the process name:
			uint procId = 0;
			GetWindowThreadProcessId(hWnd, out procId);
			var proc = System.Diagnostics.Process.GetProcessById((int)procId);
			Console.WriteLine(proc.ProcessName);
			*/


			if (screen.Bounds.Width == (rect.right - rect.left) && screen.Bounds.Height == (rect.bottom - rect.top))
			{
			return true;
			}
			else
			{
				return false;
			}


		}
	}
}