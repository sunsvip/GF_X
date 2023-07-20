// Amplify Shader Editor - Visual Shader Editing Tool
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

#if UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public class WindowsUtil
{
	public const int GWL_STYLE = -16;              //hex constant for style changing
	public const int WS_BORDER = 0x00800000;       //window with border
	public const int WS_CAPTION = 0x00C00000;      //window with a title bar with border
	public const int WS_SYSMENU = 0x00080000;      //window with no borders etc.
	public const int WS_MAXIMIZE = 0x01000000;
	public const int WS_MAXIMIZEBOX = 0x00010000;
	public const int WS_MINIMIZE = 0x20000000;
	public const int WS_MINIMIZEBOX = 0x00020000;
	public const int WS_SIZEBOX = 0x00040000;
	public const int WS_VISIBLE = 0x10000000;
	public const int WS_TABSTOP = 0x00010000;
	public const int WS_CLIPCHILDREN = 0x02000000;
	public const int WS_CLIPSIBLINGS = 0x04000000;

	[DllImport( "user32.dll", EntryPoint = "SetWindowPos" )]
	public static extern bool SetWindowPos( System.IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags );

	public delegate bool EnumWindowsProc( System.IntPtr hWnd, System.IntPtr lParam );

	[DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
	public static extern IntPtr GetDesktopWindow();

	[DllImport( "user32.dll" )]
	public static extern int SetWindowLong( IntPtr hWnd, int nIndex, int dwNewLong );

	[DllImport( "user32.dll" )]
	public static extern int GetWindowLong( IntPtr hWnd, int nIndex );

	[DllImport( "user32.dll", ExactSpelling = true, SetLastError = true )]
	internal static extern int MapWindowPoints( IntPtr hWndFrom, IntPtr hWndTo, [In, Out] ref Rect rect, [MarshalAs( UnmanagedType.U4 )] int cPoints );

	[DllImport( "user32.dll" )]
	public static extern bool EnumWindows( EnumWindowsProc enumProc, System.IntPtr lParam );

	[DllImport( "user32" )]
	[return: MarshalAs( UnmanagedType.Bool )]
	public static extern bool EnumChildWindows( IntPtr window, EnumWindowProc callback, IntPtr lParam );

	public delegate bool EnumWindowProc( IntPtr hwnd, IntPtr lParam );

	[DllImport( "user32.dll", SetLastError = true )]
	public static extern bool MoveWindow( IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint );

	[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = true )]
	public static extern int GetWindowThreadProcessId( System.IntPtr handle, out int processId );

	[DllImport( "user32.dll", SetLastError = true )]
	public static extern IntPtr FindWindowEx( string lpClassName, string lpWindowName );

	// Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.
	[DllImport( "user32.dll", EntryPoint = "FindWindow", SetLastError = true )]
	public static extern IntPtr FindWindowByCaptionEx( IntPtr ZeroOnly, string lpWindowName );

	[DllImport( "user32.dll", SetLastError = true, CharSet = CharSet.Auto )]
	public static extern int GetClassName( IntPtr hWnd, StringBuilder lpClassName, int nMaxCount );

	[DllImport( "user32.dll" )]
	public static extern int GetWindowText( System.IntPtr hWnd, StringBuilder text, int nMaxCount );

	[DllImport( "user32.dll" )]
	public static extern int GetWindowTextLength( System.IntPtr hWnd );

	[DllImport( "user32.dll" )]
	public static extern IntPtr FindWindowEx( IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle );

	[DllImport( "user32.dll" )]
	public static extern IntPtr GetActiveWindow();

	[DllImport( "user32.dll" )]
	public static extern bool GetWindowRect( System.IntPtr hwnd, ref Rect rectangle );

	static public IntPtr[] GetProcessWindows( int processId )
	{
		List<IntPtr> output = new List<IntPtr>();
		IntPtr winPtr = IntPtr.Zero;
		do
		{
			winPtr = FindWindowEx( IntPtr.Zero, winPtr, null, null );
			int id;
			GetWindowThreadProcessId( winPtr, out id );
			if( id == processId )
				output.Add( winPtr );
		} while( winPtr != IntPtr.Zero );

		return output.ToArray();
	}

	public struct Rect
	{
		public int Left { get; set; }
		public int Top { get; set; }
		public int Right { get; set; }
		public int Bottom { get; set; }
		public int Width { get { return Right - Left; } }
		public int Height { get { return Bottom - Top; } }

		public override string ToString()
		{
			return "(l: " + Left + ", r: " + Right + ", t: " + Top + ", b: " + Bottom + ")";
		}
	}

	public static bool GetProcessRect( System.Diagnostics.Process process, ref Rect rect )
	{
		IntPtr[] winPtrs = WindowsUtil.GetProcessWindows( process.Id );

		for( int i = 0; i < winPtrs.Length; i++ )
		{
			bool gotRect = WindowsUtil.GetWindowRect( winPtrs[ i ], ref rect );
			if( gotRect && ( rect.Left != 0 && rect.Top != 0 ) )
				return true;
		}
		return false;
	}

	public static void SetWindowPosition( int x, int y, int sizeX = 0, int sizeY = 0 )
	{
		System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
		process.Refresh();

		EnumWindows( delegate ( System.IntPtr wnd, System.IntPtr param )
		{
			int id;
			GetWindowThreadProcessId( wnd, out id );
			if( id == process.Id )
			{
				SetWindowPos( wnd, 0, x, y, sizeX, sizeY, sizeX * sizeY == 0 ? 1 : 0 );
				return false;
			}

			return true;
		}, System.IntPtr.Zero );
	}
}

#endif